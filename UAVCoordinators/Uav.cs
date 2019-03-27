using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using GMap.NET;

namespace UAVCoordinators
{
    internal partial class Uav
    {
        internal PointLatLng CurrentPosition;
        internal PointF CurrentPixelPosition;
        internal Color UavColor;
        private MainForm CoordinatorsForm;

        internal Uav(Color c, MainForm coordinatorsForm)
        {
            UavColor = c;
            CoordinatorsForm = coordinatorsForm;
            DrawUavBitmap();
        }

        private Bitmap UavDrawingBmp;
        private Bitmap _uavBitmap;
        internal Bitmap UavBitmap { get { return _uavBitmap; } }

        private void DrawUavBitmap()
        {
            UavDrawingBmp = new Bitmap(46, 50);
            Graphics g = Graphics.FromImage(UavDrawingBmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Point p1 = new Point(0, 3), p2 = new Point(46, 25), p3 = new Point(5, 21);
            Point p4 = new Point(p1.X, 50 - p1.Y), p5 = new Point(p3.X, 50 - p3.Y);
            Point p6 = new Point(3, 25);

            g.FillPolygon(Brushes.White, new Point[] { p1, p2, p3 });
            g.FillPolygon(Brushes.White, new Point[] { p4, p2, p5 });
            g.FillPolygon(new SolidBrush(Color.FromArgb(220, 220, 220)), new Point[] { p6, p3, p2, p5 });
            g.DrawLine(new Pen(Color.FromArgb(190, 190, 190)), p3, p2);
            g.DrawLine(new Pen(Color.FromArgb(190, 190, 190)), p5, p2);
            g.DrawLine(new Pen(Color.FromArgb(180, 180, 180)), p6, p2);
            g.DrawPolygon(new Pen(UavColor), new Point[] { p1, p3, p6, p5, p4, p2 });

            _uavBitmap = new Bitmap(66, 66);
            g = Graphics.FromImage(UavBitmap);
            g.DrawImage(UavDrawingBmp, new Point(10, 8));
        }

        private float _angle = 0;
        internal float Angle
        {
            get { return _angle; }
            set
            {
                _angle = value;
                _uavBitmap = new Bitmap(66, 66);
                Graphics g = Graphics.FromImage(UavBitmap);
                g.TranslateTransform(33, 33);
                g.RotateTransform(-value);
                g.TranslateTransform(-33, -33);
                g.DrawImage(UavDrawingBmp, new Point(10, 8));
            }
        }

        private List<PointLatLng> _waypointsLL;
        internal List<PointLatLng> WaypointsLL
        {
            get { return _waypointsLL; }
            set
            {
                _waypointsLL = value;
                CoordinatorsForm.RefreshMap();
            }
        }
    }
}
