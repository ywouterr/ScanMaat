using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using ScanMate.Domain;
using ScanMate.Methods.Processing.Processing;
using System.Windows.Forms;

namespace ScanMate
{
    

    public partial class Pipeline : Form
    {
        //DeskewAndGroup form1 = new DeskewAndGroup(this);

        //public DeskewAndGroup(Pipeline pipe)
        //{

        //}
        //public DeskewAndGroup(Pipeline form)
        //{
        //    this.form = form;
        //}
        //public string ReadPixels()
        //{
        //    form.returnPixels();
        //}


        Color[,] colorResult;// = new Color[workingImage.GetLength(0), workingImage.GetLength(1)];
        List<Tuple<Contour, Color[,]>> adjustedStamps = new List<Tuple<Contour, Color[,]>>();
        List<List<int>> grouping = new List<List<int>>();
        List<Color[,]> groupedStamps = new List<Color[,]>();
        List<Contour> innerContours;    // used to select top HT candidates
        ImageSpecific htAid = new ImageSpecific();

        public Tuple<List<Color[,]>,List<Point>> divAndConqRegions(List<Contour> outerContours, Color[,] oG_Image, sbyte[,] labelImage)//, byte shade)
        {
            for (int i = 0; i < outerContours.Count; i++)
            {
                int wOG, hOG;
                wOG = oG_Image.GetLength(0);
                hOG = oG_Image.GetLength(1);
                colorResult = HT(Cont2Img(outerContours[i], wOG, hOG), wOG, hOG, outerContours[i], i, oG_Image, labelImage);//, shade);
                //oldArr = andImages(oldArr, transferArr);
                //transferArr = HT(Cont2Img(outerContours[i]), workingImage.GetLength(0), workingImage.GetLength(1), outerContours[i], i);
                //oldArr = andImages(oldArr, transferArr);
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
            // chain groups together

            List<List<int>> clusters = grouping;

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

            List<int> doneStamps = new List<int>();
            Color[,] framed = new Color[0, 0];
            List<Point> leftTopCoords = new List<Point>();
            int w = 0;
            int h = 0;

            foreach (List<int> l in grouping)
            {
                if (!doneStamps.Contains(l[0]))
                {
                    int globalmiX = int.MaxValue;
                    int globalmaX = 0;
                    int globalmiY = int.MaxValue;
                    int globalmaY = 0;
                    foreach (int n in l)
                    {
                        if (outerContours[n].minX < globalmiX) globalmiX = outerContours[n].minX;
                        if (outerContours[n].maxX > globalmaX) globalmaX = outerContours[n].maxX;
                        if (outerContours[n].minY < globalmiY) globalmiY = outerContours[n].minY;
                        if (outerContours[n].maxY > globalmaY) globalmaY = outerContours[n].maxY;
                    }
                    int width = globalmaX - globalmiX;
                    int height = globalmaY - globalmiY;
                    w = width;
                    h = height;
                    framed = ResizeArray(framed, 0, 0);
                    framed = ResizeArray(framed, width, height);
                    //for (int y = 0; y < height; y++)
                    //{
                    //    for (int x = 0; x < width; x++)
                    //    {
                    //        framed[x, y] = Color.FromArgb(shade, shade, shade);
                    //    }
                    //}
                    foreach (int n in l)
                    {
                        int yymin = adjustedStamps[n].Item1.minY;
                        int yymax = adjustedStamps[n].Item1.maxY;
                        int xxmin = adjustedStamps[n].Item1.minX;
                        int xxmax = adjustedStamps[n].Item1.maxX;

                        int stampH = adjustedStamps[n].Item2.GetLength(1);
                        int stampW = adjustedStamps[n].Item2.GetLength(0);

                        for (int yy = 0; yy < stampH; yy++)
                        {
                            for (int xx = 0; xx < stampW; xx++)
                            {
                                framed[xx + xxmin - globalmiX, yy + yymin - globalmiY] = adjustedStamps[n].Item2[xx, yy];
                            }
                        }
                    }
                    doneStamps.AddRange(l);
                    groupedStamps.Add(framed);
                    leftTopCoords.Add(new Point(globalmiX, globalmiY));
                }
            }

            return Tuple.Create(groupedStamps, leftTopCoords);
        }

        public Color[,] HT(byte[,] stamp, int m, int n, Contour c, int nr, Color[,] colImage, sbyte[,] labelImage)//, byte shade)
        {
            int x = stamp.GetLength(0) / 2;
            int y = stamp.GetLength(1) / 2;
            double theta_step = Math.PI / m;
            double radial_step = Math.Sqrt(stamp.GetLength(0) * stamp.GetLength(0) + stamp.GetLength(1) * stamp.GetLength(1)) / n;
            int j_map = n / 2;

            int[,] accumulator = new int[m, n];

            int highest = 0;

            // fill HT array to discover salient outer lines of the object
            for (int v = 0; v < stamp.GetLength(1); v++)
            {
                for (int u = 0; u < stamp.GetLength(0); u++)
                {
                    if (stamp[u, v] > 0)
                    {
                        int x_ref = u - x;
                        int y_ref = v - y;
                        for (int i = 0; i < m; i++)
                        {
                            double t = theta_step * i;
                            double r = x_ref * Math.Cos(t) + y_ref * Math.Sin(t);
                            int j = j_map + (int)Math.Round(r / radial_step);
                            if (j >= 0 && j < n)
                            {
                                accumulator[i, j]++;
                                accumulator[i, Math.Max(0, j - 1)]++;
                                accumulator[i, Math.Min(accumulator.GetLength(1) - 1, j + 1)]++;
                                if (accumulator[i, Math.Max(0, j - 1)] > highest) highest = accumulator[i, Math.Max(0, j - 1)];
                                if (accumulator[i, j] > highest) highest = accumulator[i, j];
                                if (accumulator[i, Math.Min(accumulator.GetLength(1) - 1, j + 1)] > highest) highest = accumulator[i, Math.Min(accumulator.GetLength(1) - 1, j + 1)];
                            }
                        }
                    }
                }
            }

            int[,] suppressed = findPeaks(accumulator);

            accumulator = suppressed;

            //List<Tuple<double, int, int>> L = new List<Tuple<double, int, int>>();

            List<Tuple<int, Point>> highscores = new List<Tuple<int, Point>>();
            List<Point> top3 = new List<Point>();

            byte[,] accumByte = new byte[m, n];
            // scale accumulator to byte size
            for (int q = 0; q < accumulator.GetLength(0); q++)
            {
                for (int w = 0; w < accumulator.GetLength(1); w++)
                {
                    highscores.Add(Tuple.Create(accumulator[q, w], new Point(q, w))); // while scaling, add info to ranking list
                    accumByte[q, w] = (byte)(((double)accumulator[q, w] / highest) * 255);
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

            // grow points together to form regions
            for (int grow = 0; grow < 5; grow++)
            {
                accumByte = dilateImage(accumByte);
            }

            // find the regions
            innerContours = htAid.getBestHTs(accumByte);

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

            List<double> offAngles = new List<double>();
            // modulo 90 to obtain the common divergence
            foreach (Point sp in top3)
            {
                offAngles.Add((((double)sp.X / m) * 180) % 90);
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

            weightedAvg /= divider;
            //if (weightedAvg > 45) weightedAvg -= 90;

            if (weightedAvg > 2 || weightedAvg < -2)
                return deskew(c, stamp, weightedAvg, nr, colImage, labelImage);//, shade);
            else return deskew(c, stamp, 0, nr, colImage, labelImage);//, shade);// stamp;
        }

        private byte[,] Cont2Img(Contour c, int w, int h)
        {
            byte[,] frame = new byte[w, h];
            foreach (Point p in c.coordinates)
            {
                frame[p.X, p.Y] = 255;
            }

            return frame;
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

        private byte[,] dilateImage(byte[,] inputImage)
        {
            byte[,] resultImage = new byte[inputImage.GetLength(0), inputImage.GetLength(1)];

            for (int i = 0; i < inputImage.GetLength(0); i++)
            {
                for (int j = 0; j < inputImage.GetLength(1); j++)
                {
                    if (inputImage[i, j] == 255)
                    {
                        resultImage[i, j] = 255;
                        resultImage[i, Math.Max(0, j - 1)] = 255;
                        resultImage[Math.Max(0, i - 1), j] = 255;
                        resultImage[Math.Min(resultImage.GetLength(0) - 1, i + 1), j] = 255;
                        resultImage[i, Math.Min(resultImage.GetLength(1) - 1, j + 1)] = 255;
                    }
                }
            }

            return resultImage;
        }
        private Color[,] deskew(Contour c, byte[,] stamp, double angle, int i, Color[,] colorImage, sbyte[,] labelImage)//, byte shade)
        {
            //sbyte[,] scannedStamps = htAid.getOG_Labels(stamp);
            int w = stamp.GetLength(0);
            int h = stamp.GetLength(1);
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
                        if (labelImage[(int)Math.Floor(xinv), (int)Math.Floor(yinv)] == c.id || labelImage[(int)Math.Ceiling(xinv), (int)Math.Floor(yinv)] == c.id ||
                            labelImage[(int)Math.Floor(xinv), (int)Math.Ceiling(yinv)] == c.id || labelImage[(int)Math.Ceiling(xinv), (int)Math.Ceiling(yinv)] == c.id)
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
                            //(byte)(interPy * ((double)(i+1)/5));

                            if (x > maxX) maxX = x;
                            if (x < minX) minX = x;
                            if (y > maxY) maxY = y;
                            if (y < minY) minY = y;
                        }
                        //else result[x, y] = Color.FromArgb(shade, shade, shade);
                    }
                        //&& stampie[(int)Math.Floor(xinv), (int)Math.Floor(yinv)] != 0)//== c.id) 
                        //result[x, y] = stampie[xinv, yinv];
                    //}
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
                    framed[xx, yy] = result[minX + xx, minY + yy];
                }
            }

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
