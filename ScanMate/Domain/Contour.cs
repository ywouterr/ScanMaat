using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;

namespace ScanMate.Domain
{
    public partial class Contour : Form
    {
        public List<Point> coordinates { get; set; }
        public int id;
        public Point centroid;
        public HashSet<Point> body;
        public List<Point> outlookCoords;
        public int bg; // size of background - determining outlookcoords
        public int minX;
        public int maxX;
        public int minY;
        public int maxY;
        public int ogMinX;
        public int ogMaxX;
        public int ogMinY;
        public int ogMaxY;

        public Contour(int label, int size)
        {
            coordinates = new List<Point>();
            this.id = label;
            outlookCoords = new List<Point>();
            this.bg = size;
        }

        public void addPoint(Point n, int spacing)
        {
            if ((coordinates.Count % spacing) == 0) outlookCoords.Add(n); // this list is used to check for neighboring stamps within set vicinity
            coordinates.Add(n);                                     // the mod should be image dependent
        }

        void moveBy(int dx, int dy)
        {
            List<Point> transCoordinates = new List<Point>();
            foreach (Point pt in coordinates)
            {
                transCoordinates.Add(new Point(pt.X + dx, pt.Y + dy));
            }
            coordinates = transCoordinates;
        }

        public static void moveContoursBy(List<Contour> contours, int dx, int dy)
        {
            foreach (Contour c in contours)
            {
                c.moveBy(dx, dy);
            }
        }    
    }
}
