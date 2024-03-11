using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using ScanMate.Methods.Processing.Processing;
using ScanMate.Domain;
using System.Diagnostics;
using ScanMate.Methods.Processing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ScanMate
{

    partial class ImageToCutouts
    {
        static Stopwatch sw = new Stopwatch();

        static List<Contour> outerContours = new List<Contour>();
        static sbyte[,] labelImage;
        List<Contour> innerContours = new List<Contour>();
        private static byte[,] workingImage;

        public static List<Tuple<Color[,], Point>> process(Bitmap image)
        {
            sw.Start();

            // create array to speed-up operations (Bitmap functions are very slow)
            //Color[,] colorImage;
            //colorImage = new Color[image.Size.Width, image.Size.Height]; 

            //// copy input Bitmap to array            
            //for (int x = 0; x < image.Size.Width; x++)
            //    for (int y = 0; y < image.Size.Height; y++)   
            //        colorImage[x, y] = image.GetPixel(x, y);

            Color[,] colorImage;
            colorImage = new Color[image.Width, image.Height];

            BitmapData bmpData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            unsafe
            {
                int* ptr = (int*)bmpData.Scan0;

                for (int y = 0; y < image.Height; y++)
                {
                    for (int x = 0; x < image.Width; x++)
                    {
                        int pixelValue = ptr[y * bmpData.Width + x];
                        colorImage[x, y] = Color.FromArgb(
                            (pixelValue >> 24) & 0xFF, // Alpha
                            (pixelValue >> 16) & 0xFF, // Red
                            (pixelValue >> 8) & 0xFF,  // Green
                            pixelValue & 0xFF          // Blue
                        );
                    }
                }
            }

            image.UnlockBits(bmpData);



            Console.WriteLine("Copying bm to array {0}", sw.ElapsedMilliseconds / 1000);
            sw.Restart();

            // preprocessing
            Preprocessing pp = new Preprocessing();
            var processed = pp.preProcess(colorImage);
            workingImage = processed;

            Console.WriteLine("Preprocessing: {0} seconds elapsed",((double)sw.ElapsedMilliseconds/1000).ToString());
            sw.Restart();

            // analyze image and return found stamps as Contour objects
            ImageSpecific imSp = new ImageSpecific();
            Tuple<List<Contour>,sbyte[,]> contsAndLabels = imSp.findObjects(workingImage);
            outerContours = contsAndLabels.Item1;
            labelImage = contsAndLabels.Item2;

            Console.WriteLine("Contour finding: {0} seconds elapsed", ((double)sw.ElapsedMilliseconds / 1000).ToString());
            sw.Restart();

            // in one step deskew found stamps and cluster closeby stamps
            Pipeline orderStamps = new Pipeline();
            var groupSpecifics = orderStamps.divAndConqRegions(outerContours, colorImage, labelImage);//, shade);
            //List<Color[,]> leveledStamps = groupSpecifics.Item1;
            //List<Point> topLeftCoords = groupSpecifics.Item2;

            Console.WriteLine("Deskewing: {0} seconds elapsed", ((double)sw.ElapsedMilliseconds / 1000).ToString());
            sw.Stop();

            return groupSpecifics;// Tuple.Create(leveledStamps, topLeftCoords);
        }
    }
}
