using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ScanMate.Domain;
using System.Drawing;
using System.Windows.Forms;

namespace ScanMate.Methods.Processing.Processing
{

    public partial class ImageSpecific : Form
    {
        public byte[,] pixelImage;
        public sbyte[,] labelImage;
        public sbyte[,] scannedStamps;  // a copy is made of OG labels, as labelImage
                                        // will be reused to find top HT lines
        sbyte regionId = 0;

        List<Contour> outerContours = new List<Contour>();
        List<Contour> innerContours = new List<Contour>();

        public Tuple<List<Contour>,sbyte[,]> findObjects(byte[,] workingImage)
        {
            makeAuxArrays(workingImage);
            findContours(false);

            scannedStamps = labelImage;

            return Tuple.Create(outerContours, labelImage);
        }

        public List<Contour> getBestHTs(byte[,] htImage)
        {
            makeAuxArrays(htImage);
            findContoursHT(true);
            return innerContours;
        }

        public sbyte[,] getOG_Labels(byte[,] workingImage)
        {
            makeAuxArrays(workingImage);
            return labelImage;
        }

        public void makeAuxArrays(byte[,] image)
        {
            pixelImage = new byte[image.GetLength(0) + 2, image.GetLength(1) + 2];
            labelImage = new sbyte[image.GetLength(0) + 2, image.GetLength(1) + 2];

            for (int v = 0; v < image.GetLength(1); v++)
            {
                for (int u = 0; u < image.GetLength(0); u++)
                {
                    if (image[u, v] != 0) pixelImage[u + 1, v + 1] = 1;
                }
            }
        }

        Contour traceOuterContour(int cx, int cy, sbyte label, int size)
        {
            Contour cont = new Contour(label, size);
            traceContour(cx, cy, label, 0, cont, size);
            return cont;
        }

        Contour traceContour(int xS, int yS, sbyte label, int dS, Contour cont, int spacing)
        {
            labelImage[xS, yS] = label;
            Point pStart = new Point(xS, yS); // starting / current point
            Point pSecond;
            Point pPrev;
            Point pNext; // next point
            //int bigX = 0;   // those four vars are used to restrict the area during deskewing
            //int smolX = int.MaxValue;
            //int bigY = 0;
            //int smolY = int.MaxValue;

            cont.addPoint(pStart, spacing);
            var nextPointInfo = findNextPoint(pStart, dS);
            int dNext = nextPointInfo.Item1; // direction
            pNext = nextPointInfo.Item2;
            pSecond = pNext;

            bool done = (dNext == -1);
            if (!done) cont.addPoint(pSecond, spacing);

            while (!done)
            {
                labelImage[pNext.X, pNext.Y] = label;

                //pt = new Point(xC, yC);
                //int dSearch = (dNext + 6) % 8;
                int newDir = mod((dNext - 3), 8);
                nextPointInfo = findNextPoint(pNext, newDir);
                pPrev = pNext;
                dNext = nextPointInfo.Item1;
                pNext = nextPointInfo.Item2;
                //if (pNext.X > bigX) bigX = pNext.X;
                //if (pNext.X < smolX) smolX = pNext.X;
                //if (pNext.Y > bigY) bigY = pNext.Y;
                //if (pNext.Y < smolY) smolY = pNext.Y;
                done = (pStart == pPrev && pNext == pSecond);
                if (!done) cont.addPoint(pNext, spacing);
            }
            //cont.ogMaxX = bigX;
            //cont.ogMinX = smolX;
            //cont.ogMaxY = bigY;
            //cont.ogMinY = smolY;
            return cont;
        }

        int mod(int x, int m)
        {
            return (x % m + m) % m;
        }

        Tuple<int, Point> findNextPoint(Point pt, int dir)
        {
            int startdir = dir;
            int[,] delta = new int[,] { { -1, -1 }, { 0, -1 }, { 1, -1 }, { 1, 0 }, { 1, 1 }, { 0, 1 }, { -1, 1 }, { -1, 0 } };
            int FGdir = -1; // first encountered foreground pixel direction
            Point FGcoordinates = new Point(-1, -1);
            for (int i = 0; i < 8; i++)
            {
                int x = pt.X + delta[dir, 0];
                int y = pt.Y + delta[dir, 1];
                if (pixelImage[x, y] == 0)
                {
                    labelImage[x, y] = -1;
                }
                else
                {
                    if (labelImage[pt.X + delta[mod((dir - 1), 8), 0], pt.Y + delta[mod((dir - 1), 8), 1]] == -1)
                    //if (FGdir == -1)
                    {
                        FGdir = dir;
                        FGcoordinates = new Point(x, y);
                        return Tuple.Create(FGdir, FGcoordinates);
                    }
                }
                dir = mod((dir + 1), 8);
            }
            int testwaarde = FGdir;
            int xTest = FGcoordinates.X;
            int yTest = FGcoordinates.Y;
            return Tuple.Create(FGdir, FGcoordinates);
        }
        public List<Contour> findContoursHT(bool hough)//-// 2 different methods 
        {
            sbyte label = 0;

            if (hough) regionId = 0;

            for (int v = 1; v < pixelImage.GetLength(1) - 1; v++)
            {
                label = 0;
                for (int u = 1; u < pixelImage.GetLength(0) - 1; u++)
                {
                    if (pixelImage[u, v] == 1)
                    {
                        if (label != 0)
                        {
                            if (labelImage[u, v] == 0)
                            {
                                labelImage[u, v] = label;
                            }
                        }
                        else
                        {
                            label = labelImage[u, v];
                            if (label == 0)
                            {
                                regionId++;
                                label = regionId;
                                
                                Contour oc = traceOuterContour(u, v, label, 100);
                                if (!hough)
                                {
                                    if (oc.outlookCoords.Count > 10) outerContours.Add(oc);
                                }

                                if (hough) innerContours.Add(oc);
                                fillContour(oc, label, hough);
                            }
                        }
                    }
                }
            }
            Contour.moveContoursBy(innerContours, -1, -1);
            return innerContours;
        }

        public void findContours(bool hough)
        {
            sbyte label = 0;

            regionId = 0;

            for (int v = 1; v < pixelImage.GetLength(1) - 1; v++)
            {
                for (int u = 1; u < pixelImage.GetLength(0) - 1; u++)
                {
                    if (pixelImage[u, v] == 1 && labelImage[u, v] == 0)
                    {
                        {
                            regionId++;
                            label = regionId;
                            labelImage[u, v] = label;
                            int spacing = Math.Max((labelImage.GetLength(0) / 150 + labelImage.GetLength(1) / 150) / 2, 1); // space between checkpoints
                            Contour oc = traceOuterContour(u, v, label, spacing);
                            if (!hough)
                            {
                                //discard contours of noise //+// parameterize
                                if (oc.outlookCoords.Count > 10) outerContours.Add(oc);
                            }

                            if (hough) innerContours.Add(oc);
                            fillContour(oc, label, hough);
                        }
                    }
                }
            }
            Contour.moveContoursBy(outerContours, -1, -1);
        }
        void fillContour(Contour cont, sbyte label, bool hough)
        {
            List<Point> orderedXY = cont.coordinates;
            orderedXY = orderedXY.OrderBy(p => p.Y).ThenBy(p => p.X).ToList();

            //orderedXY.Sort(new PointComparer());

            int cxmin = int.MaxValue;
            int cxmax = 0;
            int cymin = int.MaxValue;
            int cymax = 0;

            /*if (hough) */
            cont.body = new HashSet<Point>();

            for (int i = 0; i < orderedXY.Count; i++)
            {
                if (orderedXY[i].X < cxmin) cxmin = orderedXY[i].X;
                if (orderedXY[i].X > cxmax) cxmax = orderedXY[i].X;
                if (orderedXY[i].Y < cymin) cymin = orderedXY[i].Y;
                if (orderedXY[i].Y > cymax) cymax = orderedXY[i].Y;
            }

            for (int i = 0; i < orderedXY.Count; i++)
            {
                if (orderedXY[i].Y == orderedXY[Math.Min(orderedXY.Count - 1, i + 1)].Y)
                {
                    int j = 1;
                    //while (orderedXY[i].X + j <= orderedXY[Math.Min(orderedXY.Count - 1, i + 1)].X)
                    while (orderedXY[i].X + j <= orderedXY[Math.Min(orderedXY.Count - 1, i + 1)].X && orderedXY[i].X + j <= cxmax) //== orderedXY[Math.Min(orderedXY.Count - 1, i + 1)].Y)
                    {

                        if (hough)
                        {
                            cont.body.Add(new Point(orderedXY[i].X + j, orderedXY[i].Y));

                        }
                        if (!hough)
                        {
                            labelImage[orderedXY[i].X + j, orderedXY[i].Y] = label;
                            //pixelImage[orderedXY[i].X + j, orderedXY[i].Y] = 1;
                            cont.body.Add(new Point(orderedXY[i].X + j, orderedXY[i].Y));
                        }
                        j++;
                    }
                }
            }

            cont.centroid = new Point(cxmax - (cxmax - cxmin) / 2, (cymax - (cymax - cymin) / 2));
        }
    }
}
