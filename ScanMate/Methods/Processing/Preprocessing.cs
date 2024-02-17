using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace ScanMate.Methods.Processing
{
    public class Preprocessing
    {

        public /*Tuple<*/byte[,]/*,byte>*/ preProcess(Color[,] inputImage)
        {
            byte[,] processImage = convertToGrayscale(inputImage);
            processImage = adjustContrast(processImage);
            var medianImageAndShade = medianFilter(processImage);
            processImage = medianImageAndShade.Item1;
            byte shade = medianImageAndShade.Item2;
            processImage = thresholdImage(processImage);
            return /*Tuple.Create(*/processImage/*, shade)*/;
        }
        private byte[,] convertToGrayscale(Color[,] inputImage)
        {
            // create temporary grayscale image of the same size as input, with a single channel
            byte[,] tempImage = new byte[inputImage.GetLength(0), inputImage.GetLength(1)];

            // setup progress bar
            //progressBar.Visible = true;
            //progressBar.Minimum = 1;
            //progressBar.Maximum = InputImage.Size.Width * InputImage.Size.Height;
            //progressBar.Value = 1;
            //progressBar.Step = 1;

            // process all pixels in the image
            for (int x = 0; x < inputImage.GetLength(0); x++)                 // loop over columns
                for (int y = 0; y < inputImage.GetLength(1); y++)            // loop over rows
                {
                    Color pixelColor = inputImage[x, y];                    // get pixel color
                    byte average = (byte)((pixelColor.R + pixelColor.B + pixelColor.G) / 3); // calculate average over the three channels
                    tempImage[x, y] = average;                              // set the new pixel color at coordinate (x,y)
                    //progressBar.PerformStep();                              // increment progress bar
                }

            //progressBar.Visible = false;                                    // hide progress bar

            return tempImage;
        }

        private byte[,] adjustContrast(byte[,] inputImage)
        {
            byte[,] tempImage = new byte[inputImage.GetLength(0), inputImage.GetLength(1)];
            byte high = 0;
            byte low = 255;

            for (int x = 0; x < inputImage.GetLength(0); x++)
                for (int y = 0; y < inputImage.GetLength(1); y++)
                {
                    if (inputImage[x, y] > high) high = inputImage[x, y];
                    if (inputImage[x, y] < low) low = inputImage[x, y];
                }

            for (int x = 0; x < inputImage.GetLength(0); x++)
                for (int y = 0; y < inputImage.GetLength(1); y++)
                {
                    tempImage[x, y] = (byte)((inputImage[x, y] - low) * (255 / Math.Max((high - low), 1)));
                }

            return tempImage;
        }

        private Tuple<byte[,],byte> medianFilter(byte[,] inputImage)
        {
            int breadth = inputImage.GetLength(0);
            int height = inputImage.GetLength(1);
            byte[,] tempImage = new byte[breadth + 2, height + 2];

            string category;
            int size = (inputImage.GetLength(0) + inputImage.GetLength(1)) / 2;

            if (size > 1300) category = "big";
            else category = "normal";

            //standard
            byte[] pixelVector = new byte[9];
            int correction = 1;

            switch (category)
            {
                case "normal":
                    pixelVector = new byte[9];
                    break;
                case "big":
                    pixelVector = new byte[25];
                    tempImage = new byte[breadth + 4, height + 4];
                    correction = 2;
                    break;
            }

            
            byte[,] resultImage = new byte[breadth, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < breadth; x++)
                {
                    tempImage[x + correction, y + correction] = inputImage[x, y];
                }
            }

            uint averageShade = 0;

            for (int y = correction; y < height + correction; y++)
            {
                for (int x = correction; x < breadth + correction; x++)
                {
                    int i = 0;
                    for (int h = -correction; h <= correction; h++)
                    {
                        for (int b = -correction; b <= correction; b++)
                        {
                            pixelVector[i] = tempImage[x + b, y + h];
                            i++;
                        }
                    }

                    Array.Sort(pixelVector);
                    byte middle = pixelVector[pixelVector.Length / 2];
                    resultImage[x - correction, y - correction] = middle;
                    averageShade += middle;

                }
            }

            averageShade = (uint)(averageShade / (breadth * height));
            return Tuple.Create(resultImage, (byte)averageShade);
        }

        private byte[,] thresholdImage(byte[,] inputImage)
        {
            byte low = 0;           //+// parameterize
            byte high = 255;        //+//

            for (int y = 0; y < inputImage.GetLength(1); y++)
            {
                for (int x = 0; x < inputImage.GetLength(0); x++)
                {
                    if (inputImage[x, y] <= 90)
                        inputImage[x, y] = low;
                    else inputImage[x, y] = high;
                }
            }

            return inputImage;
        }

    }
}
