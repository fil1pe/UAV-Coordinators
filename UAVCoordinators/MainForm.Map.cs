using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using GMap.NET;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using static UAVCoordinators.Utils;

namespace UAVCoordinators
{
    public partial class MainForm : Form
    {
        private GMapOverlay MapOverlay = new GMapOverlay();

        private void InitMap()
        {
            Map.MapProvider = GMap.NET.MapProviders.GoogleSatelliteMapProvider.Instance;
            Map.Overlays.Add(MapOverlay);
            Map.DragButton = MouseButtons.Left;
            Map.ShowCenter = false;
            Map.Paint += PaintOnMap;
            Map.OnMapDrag += DraggingResizingOrZoomingMap;
            Map.OnMapZoomChanged += DraggingResizingOrZoomingMap;
            Map.Resize += (sender, args) => { DraggingResizingOrZoomingMap(); };
            
            // ;-)
            GPoint a = new GPoint(0, 10), b = new GPoint(0, 125);
            PointLatLng lla = LatLngPosition(a), llb = LatLngPosition(b);
            Map.Zoom = 19;
            GPoint a1 = PixelPosition(lla), b1 = PixelPosition(llb);
            Map.Zoom = 20;
            GPoint a2 = PixelPosition(lla), b2 = PixelPosition(llb);
            Map.Zoom = 21;
            GPoint a3 = PixelPosition(lla), b3 = PixelPosition(llb);
            System.Diagnostics.Debug.WriteLine("============================");
            System.Diagnostics.Debug.WriteLine("Abstract coordinates => x: {0}\t{1}", a.X, b.X);
            System.Diagnostics.Debug.WriteLine("            Zoom: 19 => x: {0}\t{1}", a1.X, b1.X);
            System.Diagnostics.Debug.WriteLine("            Zoom: 20 => x: {0}\t{1}", a2.X, b2.X);
            System.Diagnostics.Debug.WriteLine("            Zoom: 21 => x: {0}\t{1}", a3.X, b3.X);
            System.Diagnostics.Debug.WriteLine("============================");
        }

        private void PaintOnMap(object sender, PaintEventArgs e)
        {
            foreach (Uav i in Uavs)
            {
                Color c = i.UavColor;

                for (int j = 0; j < i.WaypointsLL.Count - 1; j++)
                {
                    PointF p1 = ToPointF(PixelPosition(i.WaypointsLL[j])),
                        p2 = ToPointF(PixelPosition(i.WaypointsLL[j+1]));
                    e.Graphics.DrawLine(new Pen(c, 3), p1, p2);
                }
                int count = 1;
                foreach (var wp in i.WaypointsLL)
                    DrawWaypoint(count++, c, PixelPosition(wp), e.Graphics);

                if (i.HasPosition)
                {
                    PointF p = i.CurrentPixelPosition;
                    e.Graphics.DrawImage(i.UavBitmap, new PointF(p.X - 33, p.Y - 33));
                }
            }
        }

        private void DrawWaypoint(int wpNum, Color c, GPoint p, Graphics g)
        {
            Bitmap bmp = new Bitmap(28, 28);
            p.X -= bmp.Width / 2;
            p.Y -= bmp.Height / 2;
            Graphics g1 = Graphics.FromImage(bmp);
            g1.SmoothingMode = SmoothingMode.AntiAlias;

            g1.FillEllipse(new SolidBrush(c), 1, 1, 26, 26); // draw a circle

            // Draw the waypoint number:
            StringFormat strFormat = new StringFormat();
            strFormat.LineAlignment = StringAlignment.Center;
            strFormat.Alignment = StringAlignment.Center;
            Font f = new Font("Arial", 13, FontStyle.Bold);
            SizeF strSize = g.MeasureString(wpNum + "", f);

            g1.DrawString(wpNum + "", f, new SolidBrush(Color.White), new PointF(14, 15), strFormat);

            g.DrawImage(bmp, ToPointF(p));
        }

        private GPoint PixelPosition(PointLatLng pos) { return Map.FromLatLngToLocal(pos); }

        private PointLatLng LatLngPosition(GPoint pos) { return Map.FromLocalToLatLng((int)pos.X, (int)pos.Y); }

        // Expand grid if necessary:
        private void DraggingResizingOrZoomingMap()
        {
            GPoint p1 = PixelPosition(Map.Position);
            p1.X -= Width/2;
            p1.Y -= Height/2;
            GPoint p2 = p1;
            p2.X += Width;
            p2.Y += Height;

            RefreshGrid(new PointLatLng[] { LatLngPosition(p1), LatLngPosition(p2) });
        }

        private void FormHasClosed(object sender, FormClosedEventArgs e) { Environment.Exit(0); }

        internal void RefreshMap() { Map.Invalidate(); }
    }
}
