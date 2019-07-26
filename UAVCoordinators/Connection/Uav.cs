using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
using GMap.NET;

namespace UAVCoordinators
{
    internal class Uav : Connection
    {
        private bool _hasPosition = false;
        public bool HasPosition { get { return _hasPosition; } }
        private Color _uavColor;
        public Color UavColor
        {
            get { return _uavColor; }
            set
            {
                _uavColor = UavColor;
                DrawUavBitmap();
                CoordinatorForm.RefreshMap();
            }
        }

        private string _missionFileName = "";
        public string MissionFileName
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get { return _missionFileName; }
            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                _missionFileName = value;
                CoordinatorForm.SendMissionToSup(this);
            }
        }
        public string MissionCode = "";
        private int _missionStatus = 0;
        public int MissionStatus
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get { return _missionStatus; }
            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                _missionStatus = value;
                CoordinatorForm.SendMissionStatusToSup(this);
            }
        }

        public Uav(string conType, string ip, string port, string name, Color c, MainForm coordinatorsForm)
        {
            ConnectionType = conType;
            Ip = ip;
            Port = port;
            Name = name;
            _uavColor = c;
            CoordinatorForm = coordinatorsForm;
            DrawUavBitmap();
        }

        private Bitmap _uavDrawingBmp;
        public Bitmap UavDrawingBmp
        {
            get
            {
                Bitmap bmp = new Bitmap(66, 66);
                Graphics.FromImage(bmp).DrawImage(_uavDrawingBmp, new Point(10, 8));
                return bmp;
            }
        }
        private Bitmap _uavBitmap;
        public Bitmap UavBitmap { get { return _uavBitmap; } }

        private void DrawUavBitmap()
        {
            _uavDrawingBmp = new Bitmap(46, 50);
            Graphics g = Graphics.FromImage(_uavDrawingBmp);
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
            g.DrawPolygon(new Pen(_uavColor), new Point[] { p1, p3, p6, p5, p4, p2 });

            _uavBitmap = new Bitmap(66, 66);
            g = Graphics.FromImage(UavBitmap);
            g.DrawImage(_uavDrawingBmp, new Point(10, 8));
        }

        public PointLatLng CurrentPosition;
        public PointF CurrentPixelPosition
        {
            get { return CoordinatorForm.AbstractPosition(CurrentPosition); }
        }

        private float _angle = 0;
        public float Angle
        {
            get { return _angle; }
            set
            {
                _angle = value;
                _uavBitmap = new Bitmap(66, 66);
                Graphics g = Graphics.FromImage(_uavBitmap);
                g.TranslateTransform(33, 33);
                g.RotateTransform(-value);
                g.TranslateTransform(-33, -33);
                g.DrawImage(_uavDrawingBmp, new Point(10, 8));
            }
        }

        public int CurrentWpNum = -1;

        private List<PointLatLng> _waypointsLL;
        public List<PointLatLng> WaypointsLL
        {
            get { return _waypointsLL; }
            set
            {
                _waypointsLL = value;
                _waypointsAP = null;
                CoordinatorForm.RefreshMap();
            }
        }

        private List<PointF> _waypointsAP;
        public List<PointF> WaypointsAP
        {
            get
            {
                if(_waypointsAP == null)
                {
                    if (_waypointsLL == null) return null;
                    _waypointsAP = new List<PointF>();
                    foreach (var i in _waypointsLL) _waypointsAP.Add(CoordinatorForm.AbstractPosition(i));
                }

                return _waypointsAP;
            }
        }
    }
}
