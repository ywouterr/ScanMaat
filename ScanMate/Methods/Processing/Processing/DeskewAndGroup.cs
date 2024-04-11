using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using ScanMate.Domain;
using ScanMate.Methods.Processing.Processing;
using System.Windows.Forms;
using System.Diagnostics;

namespace ScanMate
{
    

    public partial class Pipeline : Form
    {
        static Stopwatch sw2 = new Stopwatch();
        Color[,] colorResult;
        List<Tuple<Contour, Color[,]>> adjustedStamps = new List<Tuple<Contour, Color[,]>>();
        List<List<int>> grouping = new List<List<int>>();
        List<Tuple<Color[,], Point, Color[,], byte[,]>> groupedStamps = new List<Tuple<Color[,], Point, Color[,], byte[,]>>();
        List<Contour> innerContours;    // used to select top HT candidates
        ImageSpecific htAid = new ImageSpecific();

        public List<Tuple<Color[,], Point, Color[,], byte[,]>> divAndConqRegions(List<Contour> outerContours, Color[,] oG_Image, sbyte[,] labelImage)
        {
            sw2.Start();
            int wOG, hOG;
            wOG = oG_Image.GetLength(0);
            hOG = oG_Image.GetLength(1);

            for (int i = 0; i < outerContours.Count; i++)
            {
                adjustedStamps.Add(Tuple.Create(outerContours[i], colorResult));

                // group close stamps together
                grouping.Add(new List<int>());
                grouping[i].Add(i);

                int pixels = Variables.ClustDist * Variables.ClustDist; // Squared cause calculated dist will be squared as well

                if (adjustedStamps.Count > 1)
                {
                    for (int j = i - 1; j >= 0; j--)
                    {
                        foreach (Point p in outerContours[i].outlookCoords)
                        {
                            foreach (Point q in outerContours[j].outlookCoords)
                            {
                                int distance = (p.X - q.X) * (p.X - q.X) + (p.Y - q.Y) * (p.Y - q.Y);
                                
                                if (distance < pixels) //USER VARIABLE
                                {
                                    if (!grouping[i].Contains(j))
                                    {
                                        grouping[i].Add(j); // add only to latest - when finished we traverse the lists from the back
                                    }
                                    if (!grouping[j].Contains(i))
                                    {
                                        grouping[j].Add(i);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // unify stamps which indirectly link

            for (int i = 0; i < grouping.Count; i++)
            {
                for (int j = 0; j < grouping.Count; j++)
                {
                    if (i != j)
                    {
                        for (int ii = 0; ii < grouping[i].Count; ii++)
                        {
                            for (int jj = 0; jj < grouping[j].Count; jj++)
                            {
                                if (grouping[i][ii] == grouping[j][jj])
                                {
                                    grouping[i] = grouping[i].Union(grouping[j]).ToList();
                                    grouping[j] = grouping[j].Union(grouping[i]).ToList();
                                    goto nextList;
                                }

                            }
                        }
                    }
                nextList: continue;
                }
            }



            Console.WriteLine("Link stamps: {0} seconds elapsed", ((double)sw2.ElapsedMilliseconds / 1000).ToString());
            sw2.Restart();

            Console.WriteLine("Starting deskewing");

            List<int> doneStamps = new List<int>();
            List<Point> leftTopCoords = new List<Point>();

            // collect deskewing data first, so the stamps can be deskewed in parallel
            //deskew(Contour c, byte[,] stamp, double angle, List<int> ids, Color[,] colorImage, sbyte[,] labelImage)
            //add later: Color[,] colorImage, byte[,] stamp, sbyte[,] labeliImage
            List<Tuple<Contour, double, List<int>, Color[,], byte[,]>> deskewQueue = new List<Tuple<Contour, double, List<int>, Color[,], byte[,]>>();

            foreach (List<int> l in grouping)
            {
                if (!doneStamps.Contains(l[0]))
                {

                    List<int> indexToLabelId = new List<int>();
                    foreach (int n in l)
                    {
                        indexToLabelId.Add(outerContours[n].id);
                    }
                    if (l.Count > 1)
                        //unify
                    {
                        Console.WriteLine("-----------------------");
                        Console.WriteLine("Unifying");
                        // in HT: calculate angle with dilated unification --> use the contour of the unification
                        byte[,] dilatedGroup = unifyAndDilateGroup(labelImage, indexToLabelId);

                        // find contour
                        ImageSpecific imSp = new ImageSpecific();
                        Tuple<List<Contour>, sbyte[,]> contsAndLabels = imSp.findObjects(dilatedGroup);
                        List<Contour> unifiedOuterContour = contsAndLabels.Item1;
                        sbyte[,] unifiedLabelArray = contsAndLabels.Item2;

                        Console.WriteLine("Unifying {0} stamps took: {1} seconds", l.Count, ((double)sw2.ElapsedMilliseconds / 1000).ToString());
                        sw2.Restart();
                        byte[,] stampContour = Cont2Image(unifiedOuterContour[0], wOG, hOG);
                        Color[,] debuggingStamp = byteToColor(stampContour);

                        Tuple<Contour, double, List<int>, byte[,]> deskewData = HT(stampContour, wOG/4, hOG/4, unifiedOuterContour[0], indexToLabelId, oG_Image, labelImage);
                        Tuple<Contour, double, List<int>, Color[,], byte[,]> deskewData2 = Tuple.Create(deskewData.Item1, deskewData.Item2, deskewData.Item3, debuggingStamp, deskewData.Item4);

                        deskewQueue.Add(deskewData2);
                    }
                    else
                        //single stamp
                    {
                        Console.WriteLine("-----------------------");
                        Console.WriteLine("Single stamp");
                        byte[,] stampContour = Cont2Image(outerContours[l[0]], wOG, hOG);
                        Color[,] debuggingStamp = byteToColor(stampContour);
                        Tuple<Contour, double, List<int>, byte[,]> deskewData = HT(stampContour, wOG/4, hOG/4, outerContours[l[0]], indexToLabelId, oG_Image, labelImage);
                        Tuple<Contour, double, List<int>, Color[,], byte[,]> deskewData2 = Tuple.Create(deskewData.Item1, deskewData.Item2, deskewData.Item3, debuggingStamp, deskewData.Item4);
                        deskewQueue.Add(deskewData2);
                        sw2.Restart();
                    }
                    doneStamps.AddRange(l);
                }
            }
            sw2.Restart();
            //deskew the stamps here

            int maxDegreeOfParallelism = (int)Math.Floor(Environment.ProcessorCount * 0.25); // Set to the number of processor cores by default

            // Create ParallelOptions object with max degree of parallelism
            ParallelOptions options = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism 
            };

            //Console.WriteLine("Parallel {0}", maxDegreeOfParallelism);

            Parallel.ForEach(deskewQueue, options, deskewItem =>
            {
                Color[,] result = deskew(deskewItem.Item1, wOG, hOG, deskewItem.Item2, deskewItem.Item3, oG_Image, labelImage);
                lock (groupedStamps)
                {
                    groupedStamps.Add(Tuple.Create(result, new Point(deskewItem.Item1.minX, deskewItem.Item1.minY), deskewItem.Item4, deskewItem.Item5));
                }
            });
            Console.WriteLine("Deskewing in parallel took: {0} seconds", ((double)sw2.ElapsedMilliseconds / 1000).ToString());



            //groupedStamps.Add(unifiedResult);


            return groupedStamps;//Tuple.Create(groupedStamps, leftTopCoords);
        }


        byte[,] unifyAndDilateGroup(sbyte[,] labelImage, List<int> labels)
        {
            HashSet<int> labelsInImage = new HashSet<int>();
            byte[,] dilatedGroup = new byte[labelImage.GetLength(0), labelImage.GetLength(1)];
            for (int y = 0; y < labelImage.GetLength(1); y++)
            {
                for (int x = 0; x < labelImage.GetLength(0); x++)
                {
                    labelsInImage.Add(labelImage[x, y]);
                    if (labels.Contains(labelImage[x, y]))
                    {
                        dilatedGroup[x, y] = 255;
                    }
                }
            }

            dilatedGroup = dilateImage(dilatedGroup, true);

            return dilatedGroup;
        }

        Color[,] byteToColor(byte[,] input)
        {
            Color[,] output = new Color[input.GetLength(0)-2, input.GetLength(1)-2];
            for (int i = 0; i < output.GetLength(0); i++)
            {
                for (int j = 0; j < output.GetLength(1); j++)
                {
                    if(input[i+1,j+1] == 255) output[i, j] = Color.FromArgb(255, 255, 255, 255);
                }
            }

            return output;
        }

        public Tuple<Contour, double, List<int>, byte[,]> HT(byte[,] stamp, int m, int n, Contour c, List<int> ids, Color[,] colImage, sbyte[,] labelImage)//, byte shade)
        {
            Console.WriteLine("starting HT with {0} ids", ids.Count);
            int xCentre = stamp.GetLength(0) / 2;
            int yCentre = stamp.GetLength(1) / 2;
            double theta_step = Math.PI / m;
            double[] thetaArray = new double[m];
            for (int i = 0; i < m; i++)
            {
                thetaArray[i] = theta_step * i;
            }
            double radial_step = Math.Sqrt(stamp.GetLength(0) * stamp.GetLength(0) + stamp.GetLength(1) * stamp.GetLength(1)) / n;
            int j_map = n/2;

            int[,] accumulator = new int[m, n];

            int highest = 0;

            //fill HT array to discover salient outer lines of the object
            foreach(Point p in c.coordinates)
            {
                int x_ref = p.X - xCentre;
                int y_ref = p.Y - yCentre;
            
                for (int i = 0; i < m; i++)
                {
                    double t = thetaArray[i];
                    double r = x_ref * Math.Cos(t) + y_ref * Math.Sin(t);
                    int j = j_map + (int)Math.Round(r / radial_step);
                    if (j >= 0 && j < n)
                    {
                        accumulator[i, j]++;
                        accumulator[i, Math.Max(0, j - 1)]++;
                        accumulator[i, Math.Min(n - 1, j + 1)]++;
                        if (accumulator[i, Math.Max(0, j - 1)] > highest) highest = accumulator[i, Math.Max(0, j - 1)];
                        if (accumulator[i, j] > highest) highest = accumulator[i, j];
                        if (accumulator[i, Math.Min(accumulator.GetLength(1) - 1, j + 1)] > highest) highest = accumulator[i, Math.Min(accumulator.GetLength(1) - 1, j + 1)];
                    }

                }
            }
            //Define the number of threads to use
            //int numThreads = Environment.ProcessorCount;
            //Console.WriteLine("number of processors on machine {0}", numThreads);

            //// Parallelize the outer loop using Parallel.For
            //Parallel.For(0, stamp.GetLength(1), new ParallelOptions { MaxDegreeOfParallelism = numThreads }, v =>
            //{
            //    for (int u = 0; u < stamp.GetLength(0); u++)
            //    {
            //        if (stamp[u, v] > 0)
            //        {
            //            int x_ref = u - x;
            //            int y_ref = v - y;
            //            for (int i = 0; i < m; i++)
            //            {
            //                double t = theta_step * i;
            //                double r = x_ref * Math.Cos(t) + y_ref * Math.Sin(t);
            //                int j = j_map + (int)Math.Round(r / radial_step);
            //                if (j >= 0 && j < n)
            //                {
            //                    // Ensure atomicity when incrementing accumulator values
            //                    // to prevent race conditions
            //                    lock (accumulator)
            //                    {
            //                        accumulator[i, j]++;
            //                        accumulator[i, Math.Max(0, j - 1)]++;
            //                        accumulator[i, Math.Min(accumulator.GetLength(1) - 1, j + 1)]++;
            //                        if (accumulator[i, Math.Max(0, j - 1)] > highest) highest = accumulator[i, Math.Max(0, j - 1)];
            //                        if (accumulator[i, j] > highest) highest = accumulator[i, j];
            //                        if (accumulator[i, Math.Min(accumulator.GetLength(1) - 1, j + 1)] > highest) highest = accumulator[i, Math.Min(accumulator.GetLength(1) - 1, j + 1)];
            //                    }
            //                }
            //            }
            //        }
            //    }
            //});

            Console.WriteLine("HT Array filled in {0} seconds", ((double)sw2.ElapsedMilliseconds / 1000).ToString());
            sw2.Restart();

            int[,] suppressed = findPeaks(accumulator);

            //accumulator = suppressed;

            //List<Tuple<double, int, int>> L = new List<Tuple<double, int, int>>();

            List<Tuple<int, Point>> highscores = new List<Tuple<int, Point>>();
            List<Point> top3 = new List<Point>();

            byte[,] accumByte = new byte[suppressed.GetLength(0), suppressed.GetLength(1)];

            byte[,] debugByte = new byte[suppressed.GetLength(0), suppressed.GetLength(1)];
            // scale accumulator to byte size
            for (int q = 0; q < suppressed.GetLength(0); q++)
            {
                for (int w = 0; w < suppressed.GetLength(1); w++)
                {
                    debugByte[q, w] = (byte)(((double)accumulator[q, w] / highest) * 255);
                    accumByte[q, w] = (byte)(((double)suppressed[q, w] / highest) * 255);
                    highscores.Add(Tuple.Create(suppressed[q, w], new Point(q, w))); // while scaling, add info to ranking list
                }
            }
            // only keep significant points
            for (int q = 0; q < accumByte.GetLength(0); q++)
            {
                for (int w = 0; w < accumByte.GetLength(1); w++)
                {
                    if (accumByte[q, w] < 150) accumByte[q, w] = 0;
                    else accumByte[q, w] = 255;
                }
            }
            Console.WriteLine("{0} Filtering significant points", ((double)sw2.ElapsedMilliseconds / 1000).ToString());
            sw2.Restart();

            // grow points together to form regions
            for (int grow = 0; grow < 5; grow++)
            {
                accumByte = dilateImage(accumByte, false);
            }

            Console.WriteLine("{0} HT dilation", ((double)sw2.ElapsedMilliseconds / 1000).ToString());
            sw2.Restart();

            // find the regions
            innerContours = htAid.getBestHTs(accumByte);

            Console.WriteLine("{0} Inner contours to find regions", ((double)sw2.ElapsedMilliseconds / 1000).ToString());
            sw2.Restart();

            // sort highscores, biggest first
            highscores.Sort((s, p) => p.Item1.CompareTo(s.Item1));

            // collect highest points from different regions
            List<int> undiscoveredRegions = new List<int>();
            foreach (Contour cont in innerContours)
            {
                undiscoveredRegions.Add(cont.id);
            }
            foreach (Tuple<int, Point> sp in highscores)
            {
                foreach (Contour cont in innerContours)
                {
                    if (undiscoveredRegions.Contains(cont.id) && cont.body.Contains(sp.Item2))
                    {
                        top3.Add(sp.Item2);
                        undiscoveredRegions.Remove(cont.id);
                        break;
                    }
                }
                if (top3.Count > 3) break;
            }


            Console.WriteLine("{0} Prominent point collection", ((double)sw2.ElapsedMilliseconds / 1000).ToString());
            sw2.Restart();
            Console.WriteLine("angles for contour {0}", c.id);

            List<double> offAngles = new List<double>();
            // modulo 90 to obtain the common divergence
            foreach (Point sp in top3)
            {
                double angle = (((double)sp.X / m) * 180) % 90;
                Console.WriteLine("angle {0}", angle);
                offAngles.Add(angle);
            }


            double weightedAvg = 0;
            int divider = 0;
            for (int i = offAngles.Count; i > 0; i--)
            {
                double temp = offAngles[offAngles.Count - i];
                if (temp > 45) temp -= 90;
                weightedAvg += temp * i;
                divider += i;
            }

            Console.WriteLine("{0} Calculating the average angle {1}", ((double)sw2.ElapsedMilliseconds / 1000).ToString(), weightedAvg);
            sw2.Restart();

            Console.WriteLine("weightedAvg {0} Divider {1}", weightedAvg, divider);

            weightedAvg /= Math.Max(1, divider);
            //if (weightedAvg > 45) weightedAvg -= 90;

            // pass labelImageS in stead of labelImage
            //if (weightedAvg > 2 || weightedAvg < -2)
                return Tuple.Create(c, weightedAvg, ids, accumByte);//deskew(c, stamp, weightedAvg, ids, colImage, labelImage);//, shade);
            //else return Tuple.Create(c, 0.0, ids, debugByte); //deskew(c, stamp, 0, ids, colImage, labelImage);//, shade);// stamp;
        }

        private byte[,] Cont2Image(Contour c, int w, int h)
        {
            byte[,] frame = new byte[w, h];
            foreach (Point p in c.coordinates)
            {
                frame[p.X, p.Y] = 255;
            }

            return frame;
        }

        private Color[,] Label2Stamp(sbyte[,] labelImage, Color[,] originalImage, Contour c)
        {
            
            int w = labelImage.GetLength(0);
            int h = labelImage.GetLength(1);
            int w2 = labelImage.GetLength(0);
            int h2 = labelImage.GetLength(1);

            int minX = int.MaxValue;
            int maxX = 0;
            int minY = int.MaxValue;
            int maxY = 0;

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    if(labelImage[x, y] == c.id)
                    {
                        if (x > maxX) maxX = x - 1;
                        if (x < minX) minX = x - 1;
                        if (y > maxY) maxY = y - 1;
                        if (y < minY) minY = y - 1;
                    }
                }
            }

            c.minX = minX;
            c.maxX = maxX;
            c.minY = minY;
            c.maxY = maxY;
            int width = maxX - minX;
            int height = maxY - minY;

            Color[,] framed = new Color[width, height];
            for (int yy = 0; yy < height; yy++)
            {
                for (int xx = 0; xx < width; xx++)
                {
                    framed[xx, yy] = originalImage[minX + xx, minY + yy];
                }
            }

            return framed;

        }

        public static T[,] ResizeArray<T>(T[,] original, int rows, int cols)
        {
            var newArray = new T[rows, cols];
            int minRows = Math.Min(rows, original.GetLength(0));
            int minCols = Math.Min(cols, original.GetLength(1));
            for (int i = 0; i < minRows; i++)
                for (int j = 0; j < minCols; j++)
                    newArray[i, j] = original[i, j];
            return newArray;
        }

        private int[,] findPeaks(int[,] acc)
        {
            int[,] suppressed = new int[acc.GetLength(0), acc.GetLength(1)];

            for (int q = 0; q < acc.GetLength(0); q++)
            {
                for (int w = 0; w < acc.GetLength(1); w++)
                {
                    if (acc[q, w] > 20)
                    {
                        if (acc[q, w] > acc[Math.Max(q - 1, 0), Math.Max(w - 1, 0)] &&
                            acc[q, w] > acc[q, Math.Max(w - 1, 0)] &&
                            acc[q, w] > acc[Math.Min(q + 1, acc.GetLength(0) - 1), Math.Max(w - 1, 0)] &&
                            acc[q, w] > acc[Math.Max(q - 1, 0), w] &&
                            acc[q, w] > acc[Math.Min(q + 1, acc.GetLength(0) - 1), w] &&
                            acc[q, w] > acc[Math.Max(q - 1, 0), Math.Min(w + 1, acc.GetLength(1) - 1)] &&
                            acc[q, w] > acc[q, Math.Min(w + 1, acc.GetLength(1))] &&
                            acc[q, w] > acc[Math.Min(q + 1, acc.GetLength(1)), Math.Min(w + 1, acc.GetLength(1))]
                            ) suppressed[q, w] = acc[q, w];
                    }
                }
            }
            return suppressed;
        }

        private byte[,] dilateImage(byte[,] inputImage, bool unification)
        {
            byte[,] resultImage = new byte[inputImage.GetLength(0), inputImage.GetLength(1)];
            int expansion = 1;
            if (unification) expansion = Variables.ClustDist;

            for (int i = 0; i < inputImage.GetLength(0); i++)
            {
                for (int j = 0; j < inputImage.GetLength(1); j++)
                {
                    if (inputImage[i, j] == 255)
                    {
                        for (int e = 1; e <= expansion; e++)
                        {
                            resultImage[i, j] = 255;
                            resultImage[i, Math.Max(0, j - e)] = 255;
                            resultImage[Math.Max(0, i - e), j] = 255;
                            resultImage[Math.Min(resultImage.GetLength(0) - 1, i + e), j] = 255;
                            resultImage[i, Math.Min(resultImage.GetLength(1) - 1, j + e)] = 255;
                        }
                    }
                }
            }

            return resultImage;
        }
        private Color[,] deskew(Contour c, int w, int h, double angle, List<int> ids, Color[,] colorImage, sbyte[,] labelImage)
        {
            Console.WriteLine("Starting deskewing for contour {0}", c.id);
            //int w = stamp.GetLength(0);
            //int h = stamp.GetLength(1);
            Color[,] result = new Color[w, h];

            int minX = int.MaxValue;
            int maxX = 0;
            int minY = int.MaxValue;
            int maxY = 0;

            double angleInRad = (angle) * (Math.PI / 180);
            double slope = Math.Tan(angleInRad - 90);
            double cosTh = Math.Cos(-angleInRad);
            double sinTh = Math.Sin(-angleInRad);
            int cx = c.centroid.X;
            int cy = c.centroid.Y;

            double xinv, yinv;
            double interPx1r, interPx1g, interPx1b, interPx2r, interPx2g, interPx2b;
            byte interPyr, interPyg, interPyb; // Px1 and Px2 to get interpolation over x-axis for top and bottom points, then interpolate over those

            int quadraticRadius = (c.coordinates[0].X - cx) * (c.coordinates[0].X - cx) + (c.coordinates[0].Y - cy) * (c.coordinates[0].Y - cy);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    xinv = ((x - cx) * cosTh + (y - cy) * sinTh) + cx;
                    yinv = (-(x - cx) * sinTh + (y - cy) * cosTh) + cy;

                    if (xinv >= 0 && Math.Ceiling(xinv) < w && yinv >= 0 && Math.Ceiling(yinv) < h)
                    {
                        if (ids.Contains(labelImage[(int)Math.Floor(xinv), (int)Math.Floor(yinv)]) || ids.Contains(labelImage[(int)Math.Ceiling(xinv), (int)Math.Floor(yinv)]) ||
                            ids.Contains(labelImage[(int)Math.Floor(xinv), (int)Math.Ceiling(yinv)]) || ids.Contains(labelImage[(int)Math.Ceiling(xinv), (int)Math.Ceiling(yinv)]))
                        //if (c.body.Contains(new Point((int)Math.Floor(xinv), (int)Math.Floor(yinv))) || c.body.Contains(new Point((int)Math.Ceiling(xinv), (int)Math.Floor(yinv))) ||
                        //    c.body.Contains(new Point((int)Math.Floor(xinv), (int)Math.Ceiling(yinv))) || c.body.Contains(new Point((int)Math.Ceiling(xinv), (int)Math.Ceiling(yinv))) )
                        {
                            // get pixels colors
                            Color pixelColorNW = colorImage[(int)Math.Floor(xinv), (int)Math.Floor(yinv)];
                            Color pixelColorNE = colorImage[(int)Math.Ceiling(xinv), (int)Math.Floor(yinv)];

                            Color pixelColorSW = colorImage[(int)Math.Floor(xinv), (int)Math.Ceiling(yinv)];
                            Color pixelColorSE = colorImage[(int)Math.Ceiling(xinv), (int)Math.Ceiling(yinv)];


                            //R
                            interPx1r = (1 - (xinv - (int)xinv)) * pixelColorNW.R + (xinv - (int)xinv) * pixelColorNE.R;
                            //G
                            interPx1g = (1 - (xinv - (int)xinv)) * pixelColorNW.G + (xinv - (int)xinv) * pixelColorNE.G;
                            //B
                            interPx1b = (1 - (xinv - (int)xinv)) * pixelColorNW.B + (xinv - (int)xinv) * pixelColorNE.B;

                            //R
                            interPx2r = (1 - (xinv - (int)xinv)) * pixelColorSW.R + (xinv - (int)xinv) * pixelColorSE.R;
                            //G
                            interPx2g = (1 - (xinv - (int)xinv)) * pixelColorSW.G + (xinv - (int)xinv) * pixelColorSE.G;
                            //B
                            interPx2b = (1 - (xinv - (int)xinv)) * pixelColorSW.B + (xinv - (int)xinv) * pixelColorSE.B;

                            //R
                            interPyr = (byte)((1 - (yinv - (int)yinv)) * interPx1r + (yinv - (int)yinv) * interPx2r);
                            //G
                            interPyg = (byte)((1 - (yinv - (int)yinv)) * interPx1g + (yinv - (int)yinv) * interPx2g);
                            //B
                            interPyb = (byte)((1 - (yinv - (int)yinv)) * interPx1b + (yinv - (int)yinv) * interPx2b);

                            result[x, y] = Color.FromArgb(interPyr, interPyg, interPyb);

                            if (x > maxX) maxX = x;
                            if (x < minX) minX = x;
                            if (y > maxY) maxY = y;
                            if (y < minY) minY = y;
                        }
                    }
                }
            c.minX = minX;
            c.maxX = maxX;
            c.minY = minY;
            c.maxY = maxY;
            }


            int width = maxX - minX;
            int height = maxY - minY;


            Color[,] framed = new Color[width, height];
            for (int yy = 0; yy < height; yy++)
            {
                for (int xx = 0; xx < width; xx++)
                {
                    framed[xx, yy] = result[minX + xx, minY + yy];
                }
            }

            Console.WriteLine("Deskewing took {0} seconds", ((double)sw2.ElapsedMilliseconds / 1000).ToString());
            sw2.Restart();

            return framed;
        }



        public Tuple<double, double> convToSlopeInt(Point p, byte[,] stamp, int m, int n)
        {
            // convert back from index to real distances
            double r_units = (p.Y - 200) * (Math.Sqrt(stamp.GetLength(0) * stamp.GetLength(0) + stamp.GetLength(1) * stamp.GetLength(1)) / n);
            double t_units = p.X * Math.PI / m;

            double x = (r_units * Math.Cos(t_units));
            double y = (r_units * Math.Sin(t_units));

            // calculate the line equation on which our line of interest is perpendicular
            double slope = y / x;
            double b = y - slope * x;

            // line equation of interest
            double a_p = -1 / slope;
            double b_p = a_p * -x + y;

            return Tuple.Create<double, double>(a_p, b_p/*x, y*/);
        }
    }
}
