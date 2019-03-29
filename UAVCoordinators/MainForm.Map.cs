using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using GMap.NET;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;

namespace UAVCoordinators
{
    public partial class MainForm : Form
    {
        private PointF MapOrigin;
        private GMapOverlay MapOverlay = new GMapOverlay();
        List<Uav> Uavs = new List<Uav>();

        private void PaintOnMap(object sender, PaintEventArgs e)
        {
            foreach (Uav i in Uavs)
            {
                Color c = i.UavColor;

                for (int j = 0; j < i.WaypointsLL.Count - 1; j++)
                {
                    PointF p1 = PixelPosition(i.WaypointsLL[j]),
                        p2 = PixelPosition(i.WaypointsLL[j+1]);
                    e.Graphics.DrawLine(new Pen(c, 3), p1, p2);
                }
                int count = 1;
                foreach (var wp in i.WaypointsLL)
                    DrawWaypoint(count++, c, PixelPosition(wp), e.Graphics);

                if (i.CurrentPixelPosition != null)
                {
                    PointF p = i.CurrentPixelPosition;
                    e.Graphics.DrawImage(i.UavBitmap, new PointF(p.X - 33 + MapOrigin.X, p.Y - 33 + MapOrigin.Y));
                }
            }
        }

        private void DrawWaypoint(int wpNum, Color c, PointF p, Graphics g)
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

            g.DrawImage(bmp, p);
        }

        private GPoint MapPixelPos;

        private PointF PixelPosition(PointLatLng pos)
        {
            GPoint temp = Map.MapProvider.Projection.FromLatLngToPixel(pos, (int)Map.Zoom);
            return new PointF(
                temp.X - MapPixelPos.X + MapOrigin.X,
                temp.Y - MapPixelPos.Y + MapOrigin.Y
            );
        }
        
        #region Map dragging control

        private Point MousePositionOnMap;
        private bool DraggingMap = false;

        private void MouseDownOnMap(object sender, MouseEventArgs e)
        {
            MousePositionOnMap = e.Location;
            DraggingMap = true;
        }

        private void MouseMoveOnMap(object sender, MouseEventArgs e)
        {
            if (DraggingMap)
            {
                MapOrigin.X += e.X - MousePositionOnMap.X;
                MapOrigin.Y += e.Y - MousePositionOnMap.Y;
                MousePositionOnMap = e.Location;
            }
        }

        private void MouseUpOnMap(object sender, MouseEventArgs e) { DraggingMap = false; }

        #endregion

        private void MapResizedOrZoomed(object sender, EventArgs e)
        {
            MapOrigin.X = (float)Map.Width / 2;
            MapOrigin.Y = (float)Map.Height / 2;
            MapPixelPos = Map.MapProvider.Projection.FromLatLngToPixel(Map.Position, (int)Map.Zoom);
            Map.Invalidate();
        }

        private double MapZoom = -1;

        private void MapZoomed(object sender, EventArgs e)
        {
            if (MapZoom == Map.Zoom) return;
            MapResizedOrZoomed(sender, e);
            MapZoom = Map.Zoom;
        }

        private void FormHasClosed(object sender, FormClosedEventArgs e) { Environment.Exit(0); }

        internal void RefreshMap() { Map.Invalidate(); }
    }
}
