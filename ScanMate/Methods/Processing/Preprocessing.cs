using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

using System.Diagnostics;
namespace ScanMate.Methods.Processing
{
    public class Preprocessing
    {
        static Stopwatch sw3 = new Stopwatch();


        public /*Tuple<*/byte[,]/*,byte>*/ preProcess(Color[,] inputImage)
        {
            sw3.Start();
            byte[,] processImage = convertToGrayscale(inputImage);
            Console.WriteLine("{0} Grayscale", ((double)sw3.ElapsedMilliseconds / 1000).ToString());
            sw3.Restart();
            processImage = adjustContrast(processImage);
            Console.WriteLine("{0} Contrast", ((double)sw3.ElapsedMilliseconds / 1000).ToString());
            sw3.Restart();
            var medianImageAndShade = MedianFilter(processImage);
            Console.WriteLine("{0} Median", ((double)sw3.ElapsedMilliseconds / 1000).ToString());
            sw3.Restart();
            processImage = medianImageAndShade.Item1;
            byte shade = medianImageAndShade.Item2;
            processImage = thresholdImage(processImage);
            Console.WriteLine("{0} Threshold", ((double)sw3.ElapsedMilliseconds / 1000).ToString());
            sw3.Stop();
            return /*Tuple.Create(*/processImage/*, shade)*/;
        }

        static Tuple<byte[,], byte> MedianFilter(byte[,] inputImage)
        {
            int breadth = inputImage.GetLength(0);
            int height = inputImage.GetLength(1);

            // Median kernel radius
            int r = 2;

            byte[,] tempImage = new byte[breadth + 2 * r, height + 2 * r];
            byte[,] resultImage = new byte[breadth, height];

            // Initialize kernel histogram H and column histograms h1...n
            int[] kernelHistogram = new int[256];
            int[,] columnHistograms = new int[height, 256];

            uint averageShade = 0;

            // Copy input image to tempImage
            for (int y = r; y < height + r; y++)
            {
                for (int x = r; x < breadth + r; x++)
                {
                    tempImage[x, y] = inputImage[x - r, y - r];
                }
            }

            // Initialize histograms for the first kernel
            for (int i = 0; i < 2 * r + 1; i++)
            {
                for (int j = 0; j < 2 * r + 1; j++)
                {
                    byte pixel = tempImage[i, j];
                    columnHistograms[j, pixel]++;
                    kernelHistogram[pixel]++;
                }
            }

            // Process each pixel
            for (int y = r; y < height + r; y++)
            {
                for (int x = r; x < breadth + r; x++)
                {
                    // Compute median
                    resultImage[x - r, y - r] = (byte)GetMedian(kernelHistogram);

                    // Update average shade
                    averageShade += resultImage[x - r, y - r];

                    // Update column histograms
                    if (x - r - 1 >= r)
                    {
                        byte oldPixel = tempImage[x - r - 1, y];
                        columnHistograms[y - r, oldPixel]--;
                    }

                    if (x + r < breadth + r)
                    {
                        byte newPixel = tempImage[x + r, y];
                        columnHistograms[y - r, newPixel]++;
                    }

                    // Update kernel histogram
                    if (y - r - 1 >= r)
                    {
                        for (int k = 0; k < 256; k++)
                        {
                            kernelHistogram[k] -= columnHistograms[y - r - 1, k];
                        }
                    }

                    if (y + r < height + r)
                    {
                        for (int k = 0; k < 256; k++)
                        {
                            kernelHistogram[k] += columnHistograms[y + r, k];
                        }
                    }
                }
            }

            // Compute average shade
            averageShade /= (uint)(breadth * height);

            return Tuple.Create(resultImage, (byte)averageShade);
        }

        static int GetMedian(int[] histogram)
        {
            int sum = 0;
            for (int i = 0; i < 256; i++)
            {
                sum += histogram[i];
                if (sum > 0.5)
                    return i;
            }
            return 0; // Should never reach here
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
            int width = inputImage.GetLength(0);
            int height = inputImage.GetLength(1);
            byte[,] tempImage = new byte[width, height];
            byte high = 0;
            byte low = 255;

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    byte pixelValue = inputImage[x, y];
                    if (pixelValue > high) high = pixelValue;
                    if (pixelValue < low) low = pixelValue;
                }

            float factor = 255 / Math.Max((high - low), 1);

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    tempImage[x, y] = (byte)((inputImage[x, y] - low) * factor);
                }

            return tempImage;
        }

        private Tuple<byte[,],byte> medianFilter(byte[,] inputImage)
        {
            int breadth = inputImage.GetLength(0);
            int height = inputImage.GetLength(1);
            byte[,] tempImage = new byte[breadth + 2, height + 2];

            int size = (breadth + height)/ 2;

            bool isBig = size > 1300;

            //standard
            byte[] pixelVector = isBig ? new byte[25] : new byte[9];
            int correction = isBig ? 2 : 1;

            // Adjust temp image size if big
            tempImage = isBig ? new byte[breadth + 4, height + 4] : new byte[breadth + 2, height + 2];

            byte[,] resultImage = new byte[breadth, height];
            uint averageShade = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < breadth; x++)
                {
                    tempImage[x + correction, y + correction] = inputImage[x, y];
                }
            }


            for (int y = correction; y < height + correction; y++)
            {
                for (int x = correction; x < breadth + correction; x++)
                {
                    int i = 0;
                    for (int h = -correction; h <= correction; h++)
                    {
                        int offsetY = y + h;
                        for (int b = -correction; b <= correction; b++)
                        {
                            pixelVector[i] = tempImage[x + b, offsetY];
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
