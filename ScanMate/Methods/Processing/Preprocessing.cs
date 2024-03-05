using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

using System.Diagnostics;
namespace ScanMate.Methods.Processing
{
    class Mat<T>
    {
        public T[,] Data { get; set; }
        public int Rows { get; }
        public int Cols { get; }

        public Mat(int rows, int cols)
        {
            Rows = rows;
            Cols = cols;
            Data = new T[rows, cols];
        }

        public T this[int row, int col]
        {
            get => Data[row, col];
            set => Data[row, col] = value;
        }
    }
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
            var medianImageAndShade = MedianFilterFast(processImage, 5);// MedianFilter2(processImage);
            Console.WriteLine("{0} Median", ((double)sw3.ElapsedMilliseconds / 1000).ToString());
            sw3.Restart();
            processImage = medianImageAndShade.Item1;
            byte shade = medianImageAndShade.Item2;
            processImage = thresholdImage(processImage);
            Console.WriteLine("{0} Threshold", ((double)sw3.ElapsedMilliseconds / 1000).ToString());
            sw3.Stop();
            return /*Tuple.Create(*/processImage/*, shade)*/;
        }
        private Tuple<byte[,], byte> MedianFilter(byte[,] inputImage)
        {
            int breadth = inputImage.GetLength(0);
            int height = inputImage.GetLength(1);
            byte[,] tempImage = new byte[breadth + 2, height + 2];

            int size = (breadth + height) / 2;

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

        static Tuple<byte[,], byte> MedianFilterFast(byte[,] src, int kernelSize)
        {
            int rows = src.GetLength(0);
            int cols = src.GetLength(1);
            byte[,] dst = new byte[rows, cols];

            // Ensure offset is at least 1 to avoid negative indexing
            int offset = Math.Max(1, kernelSize / 2);
            int threshold = kernelSize * kernelSize / 2;
            uint sum = 0;

            for (int i = 0; i < rows; i++)
            {
                var histogram = new int[256];

                for (int k = 0; k < 256; k++)
                    histogram[k] = 0;

                if (i - offset < 0)
                {
                    int mul = (i - offset) * -1;
                    histogram[src[0, 0]] += mul * (kernelSize / 2);
                }

                if (i + offset >= rows)
                {
                    int mul = (i + offset - (rows - 1));
                    histogram[src[rows - 1, 0]] += mul * (kernelSize / 2);
                }

                for (int di = Math.Max(0, i - offset); di <= Math.Min(rows - 1, i + offset); di++)
                {
                    histogram[src[di, 0]] += kernelSize / 2;

                    for (int dj = 0; dj < kernelSize / 2; dj++)
                    {
                        if (di == 0 && i - offset < 0)
                        {
                            int mul = (i - offset) * -1;
                            histogram[src[0, dj]] += mul;
                        }

                        if (di == rows - 1 && i + offset > rows - 1)
                        {
                            int mul = (i + offset - (rows - 1));
                            histogram[src[rows - 1, dj]] += mul;
                        }

                        histogram[src[di, dj]]++;
                    }
                }

                for (int j = 0; j < cols; j++)
                {
                    for (int di = i - offset; di <= i + offset; di++)
                    {
                        histogram[src[Math.Min(Math.Max(di, 0), rows - 1), Math.Min(Math.Max(j + offset, 0), cols - 1)]]++;
                    }

                    // Set median
                    int count = 0;
                    for (int k = 0; k < 256; k++)
                    {
                        count += histogram[k];
                        if (count > threshold)
                        {
                            dst[i, j] = (byte)k;
                            break;
                        }
                    }
                    for (int di = i - offset; di <= i + offset; di++)
                    {
                        histogram[src[Math.Min(Math.Max(di, 0), rows - 1), Math.Min(Math.Max(j - offset, 0), cols - 1)]]--;
                    }

                    sum += src[i, j];
                }
            }

            byte averageByte = (byte)(sum / (rows * cols));
            return Tuple.Create(dst, averageByte);
        }



        //static Tuple<byte[,], byte> MedianFilter2(byte[,] inputImage)
        //{
        //    int m = inputImage.GetLength(0);
        //    int n = inputImage.GetLength(1);
        //    byte[,] result = new byte[m, n];

        //    // Median kernel radius
        //    int r = 2;

        //    // Initialize kernel histogram H and column histograms h1...n
        //    int[] kernelHistogram = new int[256];
        //    int[,] columnHistograms = new int[n, 256];

        //    // Initialization
        //    for (int i = 0; i < r; i++)
        //    {
        //        for (int j = 0; j < n; j++)
        //        {
        //            byte pixel = inputImage[i, j];
        //            columnHistograms[j, pixel]++;
        //            kernelHistogram[pixel]++;
        //        }
        //    }

        //    // Process each pixel
        //    for (int i = 0; i < m; i++)
        //    {
        //        for (int j = 0; j < n; j++)
        //        {
        //            // Update column histograms
        //            if (i - r >= 0)
        //            {
        //                byte oldPixel = inputImage[i - r, j];
        //                columnHistograms[j, oldPixel]--;
        //            }

        //            if (i + r < m)
        //            {
        //                byte newPixel = inputImage[i + r, j];
        //                columnHistograms[j, newPixel]++;
        //            }

        //            // Update kernel histogram
        //            if (j - r - 1 >= 0)
        //            {
        //                for (int k = 0; k < 256; k++)
        //                {
        //                    kernelHistogram[k] -= columnHistograms[j - r - 1, k];
        //                }
        //            }

        //            if (j + r < n)
        //            {
        //                for (int k = 0; k < 256; k++)
        //                {
        //                    kernelHistogram[k] += columnHistograms[j + r, k];
        //                }
        //            }

        //            // Compute median
        //            result[i, j] = (byte)GetMedian(kernelHistogram);
        //        }
        //    }

        //    return Tuple.Create(result, (byte)255);
        //}

        //static int GetMedian(int[] histogram)
        //{
        //    int sum = 0;
        //    for (int i = 0; i < 256; i++)
        //    {
        //        sum += histogram[i];
        //        if (sum > 0.5)
        //            return i;
        //    }
        //    return 0; // Should never reach here
        //}
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
