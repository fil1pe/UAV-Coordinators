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
        List<Uav> Uavs = new List<Uav>();

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
