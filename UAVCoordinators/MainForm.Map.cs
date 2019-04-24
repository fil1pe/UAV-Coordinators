using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using GMap.NET;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using static UAVCoordinators.Utils;
using static System.Math;

namespace UAVCoordinators
{
    public partial class MainForm : Form
    {
        private GMapOverlay MapOverlay = new GMapOverlay();
        private double InitialZoom;
        private GMapMarker InitialPosMarker;
        private SizeF MapSize;

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
            Map.MouseDown += MouseDownOnMap;
            Map.MouseMove += MouseMoveOnMap;
            Map.MouseUp += MouseReleasedOnMap;
            Map.Resize += MapResized;
            MapSize = Map.Size;
            Map.Load += MapLoad;
            Map.OnMapZoomChanged += MapZoomed;
        }

        private void MapLoad(object sender, EventArgs e)
        {
            InitialPosMarker = new GMarkerGoogle(Map.Position, GMarkerGoogleType.arrow);
            InitialPosMarker.IsVisible = false;
            MapOverlay.Markers.Add(InitialPosMarker);
            Origin = new PointF(Map.Width/2, Map.Height/2);
        }

        private PointF Origin;

        private PointF PixelPosition(PointF abstractPos)
        {
            float T = (float)Pow(2, Map.Zoom - InitialZoom); // The coordinate transformation coefficient
            return new PointF(
                abstractPos.X * T + Origin.X,
                abstractPos.Y * T + Origin.Y
            );
        }

        #region Handling of moving, resizing and zooming of the map

        private PointF DragMousePos;
        private bool Dragging = false;
        private void MouseDownOnMap(object sender, MouseEventArgs e)
        {
            Dragging = true;
            DragMousePos = e.Location;
        }
        private void MouseMoveOnMap(object sender, MouseEventArgs e)
        {
            if (Dragging)
            {
                var oldMousePos = DragMousePos;
                DragMousePos = e.Location;
                Origin.X += DragMousePos.X - oldMousePos.X;
                Origin.Y += DragMousePos.Y - oldMousePos.Y;
            }
        }
        private void MouseReleasedOnMap(object sender, MouseEventArgs e)
        {
            Dragging = false;
        }
        private void MapResized(object sender, EventArgs e)
        {
            SizeF oldMapSize = MapSize;
            MapSize = Map.Size;
            Origin.X += (MapSize.Width - oldMapSize.Width) / 2;
            Origin.Y += (MapSize.Height - oldMapSize.Height) / 2;
        }
        private void MapZoomed()
        {
            Origin = ToPointF(PixelPosition(InitialPosMarker.Position));
        }

        #endregion

        private GPoint PixelPosition(PointLatLng pos) { return Map.FromLatLngToLocal(pos); }

        internal PointF AbstractPosition(PointLatLng pos)
        {
            float T = (float)Pow(2, Map.Zoom - InitialZoom);
            GPoint gp = PixelPosition(pos);
            return new PointF((gp.X - Origin.X)/T, (gp.Y - Origin.Y)/T);
        }

        private void PaintOnMap(object sender, PaintEventArgs e)
        {
            PointF t1 = PixelPosition(new PointF(0, 0));
            PointF t2 = PixelPosition(new PointF(20, 100));
            e.Graphics.DrawLine(new Pen(Brushes.Aqua, 5), t1, t2);

            foreach (Uav i in Uavs)
            {
                Color c = i.UavColor;

                for (int j = 0; j < i.WaypointsLL.Count - 1; j++)
                {
                    PointF p1 = ToPointF(PixelPosition(i.WaypointsLL[j])),
                        p2 = ToPointF(PixelPosition(i.WaypointsLL[j+1]));
                    System.Diagnostics.Debug.WriteLine("{0} {1}", p1, p2);
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

        private PointLatLng LatLngPosition(GPoint pos) { return Map.FromLocalToLatLng((int)pos.X, (int)pos.Y); }

        // Expand the grid if necessary:
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

        internal void RefreshMap() { Map.Invalidate(); }
    }
}
