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

namespace ScanMate
{

    partial class ImageToCutouts
    {
        static Stopwatch sw = new Stopwatch();

        static List<Contour> outerContours = new List<Contour>();
        static sbyte[,] labelImage;
        List<Contour> innerContours = new List<Contour>();
        private static byte[,] workingImage;
        //-//private static byte[,] stampie;     // copy of workingImage to validate


        public static Tuple<List<Color[,]>, List<Point>> process(Bitmap image)
        {
            Color[,] colorImage;
            //if (image == null) return new Color[;
            //Bitmap OutputImage1 = new Bitmap(image.Size.Width, image.Size.Height); // create new output image
            colorImage = new Color[image.Size.Width, image.Size.Height]; // create array to speed-up operations (Bitmap functions are very slow)

            // copy input Bitmap to array            
            for (int x = 0; x < image.Size.Width; x++)
                for (int y = 0; y < image.Size.Height; y++)   
                    colorImage[x, y] = image.GetPixel(x, y);

            sw.Restart();

            // preprocessing

            Preprocessing pp = new Preprocessing();
            var processed = pp.preProcess(colorImage);
            workingImage = processed;//.Item1;
            //byte shade = processed.Item2;

            Console.WriteLine("Preprocessing: {0} seconds elapsed",((double)sw.ElapsedMilliseconds/1000).ToString());

            //-//Color[,] colorResult = new Color[workingImage.GetLength(0), workingImage.GetLength(1)];

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
            List<Color[,]> leveledStamps = groupSpecifics.Item1;
            List<Point> topLeftCoords = groupSpecifics.Item2;

            Console.WriteLine("Deskewing: {0} seconds elapsed", ((double)sw.ElapsedMilliseconds / 1000).ToString());

            return Tuple.Create(leveledStamps, topLeftCoords);
        }
    }
}
