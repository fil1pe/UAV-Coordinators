using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using GMap.NET;
using GMap.NET.WindowsForms;
using static UAVCoordinators.Utils;
using static System.Math;
using GMap.NET.WindowsForms.Markers;

namespace UAVCoordinators
{
    public partial class MainForm : Form
    {
        private const double LngMRatio = 1.00179 * 0.00001, LatMRatio = 8.98315 * 0.000001;
        private SizeF QSize; // Quadrant size
        private SizeF QSizeBackup;
        private double[] GridCoordinates;
        private GMapOverlay GridOverlay = new GMapOverlay();
        public List<Tuple<double, double>> Quadrants = new List<Tuple<double, double>>();
        private Dictionary<PointLatLng, GMapPolygon> GridPolygons = new Dictionary<PointLatLng, GMapPolygon>();
        private bool _areaDefined = false;
        public bool AreaDefined { get { return _areaDefined; } }

        private PointLatLng DiscreteLLPoint(PointLatLng p)
        {
            return new PointLatLng(
                Ceiling(p.Lat / (QSize.Height * LatMRatio)) * QSize.Height * LatMRatio,
                Floor(p.Lng / (QSize.Width * LngMRatio)) * QSize.Width * LngMRatio);
        }

        private Tuple<double, double> Quadrant(PointLatLng p)
        {
            return new Tuple<double, double>(
                Ceiling(p.Lat / (QSize.Height * LatMRatio)), Floor(p.Lng / (QSize.Width * LngMRatio)));
        }

        public void CompletelyRefreshGrid()
        {
            Map.Overlays.Remove(GridOverlay);
            GridOverlay = new GMapOverlay();
            InitGrid();
        }

        private bool GridPolygonsClickable = false;
        private Panel QuadrantsSelectionPanel = new Panel();
        private List<Tuple<double, double>> QuadrantsBackup;

        private void StartQuadrantsSelection()
        {
            _areaDefined = false;
            GridPolygonsClickable = true;
            GridPolygons = new Dictionary<PointLatLng, GMapPolygon>();
            QuadrantsBackup = Quadrants;
            Quadrants = new List<Tuple<double, double>>();
            CompletelyRefreshGrid();
            Map.MouseDown += MouseDownOnGrid;
            Map.MouseMove += MouseMoveOnGrid;
            Controls.Add(QuadrantsSelectionPanel);
            QuadrantsSelectionPanel.Visible = true;
            QuadrantsSelectionPanel.Size = new Size(ClientSize.Width, 40);
            QuadrantsSelectionPanel.Location = new Point(0, ClientSize.Height - QuadrantsSelectionPanel.Height);
            QuadrantsSelectionPanel.BringToFront();
            Map.Cursor = Cursors.Hand;
        }

        private void StopQuadrantsSelection(bool canceled)
        {
            Controls.Remove(QuadrantsSelectionPanel);
            Map.MouseDown -= MouseDownOnGrid;
            Map.MouseMove -= MouseMoveOnGrid;
            if (canceled)
            {
                QSize = QSizeBackup;
                Quadrants = QuadrantsBackup;
            }
            _areaDefined = Quadrants.Count > 0;
            GridPolygonsClickable = false;
            GridPolygons = new Dictionary<PointLatLng, GMapPolygon>();
            CompletelyRefreshGrid();
            Map.Cursor = Cursors.Arrow;
        }

        private PointLatLng MouseDownDiscretePos;
        private object[] QDrawingTools = new object[] { new Pen(Color.FromArgb(32, 255, 255, 255), 2), new SolidBrush(Color.Empty), new SolidBrush(Color.FromArgb(128, 0, 0, 0)), new SolidBrush(Color.FromArgb(80, Color.HotPink)) };

        private void MouseDownOnGrid(object sender, MouseEventArgs e)
        {
            MouseDownDiscretePos = DiscreteLLPoint(Map.FromLocalToLatLng(e.X, e.Y));
            MouseMoveOnGrid(sender, e);
        }

        private void MouseMoveOnGrid(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;

            var mouseDownPos = new PointLatLng(MouseDownDiscretePos.Lat - QSize.Height*LatMRatio/2, MouseDownDiscretePos.Lng + QSize.Width*LngMRatio/2);
            var mouseCurPos = DiscreteLLPoint(Map.FromLocalToLatLng(e.X, e.Y));

            if (mouseCurPos == MouseDownDiscretePos)
            {
                GridPolygons[mouseCurPos].Fill = (SolidBrush)QDrawingTools[3];
                mouseCurPos = new PointLatLng(mouseCurPos.Lat - QSize.Height*LatMRatio/2, mouseCurPos.Lng + QSize.Width*LngMRatio/2);
                var quadrant = Quadrant(mouseCurPos);
                if (!Quadrants.Contains(quadrant)) Quadrants.Add(quadrant);
                return;
            }

            mouseCurPos = new PointLatLng(mouseCurPos.Lat - QSize.Height*LatMRatio/2, mouseCurPos.Lng + QSize.Width*LngMRatio/2);

            var first = new PointLatLng(Max(mouseDownPos.Lat, mouseCurPos.Lat), Min(mouseDownPos.Lng, mouseCurPos.Lng));
            var temp = first;
            var last = new PointLatLng(Min(mouseDownPos.Lat, mouseCurPos.Lat), Max(mouseDownPos.Lng, mouseCurPos.Lng));

            while (first.Lat > last.Lat)
            {
                while (first.Lng < last.Lng)
                {
                    GridPolygons[DiscreteLLPoint(first)].Fill = (SolidBrush)QDrawingTools[3];
                    var quadrant = Quadrant(first);
                    if (!Quadrants.Contains(quadrant)) Quadrants.Add(quadrant);
                    first.Lng += QSize.Width * LngMRatio;
                }

                first.Lng = temp.Lng;
                first.Lat -= QSize.Height * LatMRatio;
            }
        }

        private void InitGrid()
        {
            Map.Overlays.Add(GridOverlay);

            PointLatLng first = DiscreteLLPoint(new PointLatLng(GridCoordinates[1], GridCoordinates[2]));
            var temp = first;

            while (first.Lat > GridCoordinates[0])
            {
                while (first.Lng < GridCoordinates[3])
                {
                    DrawCell(first, Quadrants.Contains(Quadrant(
                        new PointLatLng(first.Lat - QSize.Height*LatMRatio/2, first.Lng + QSize.Width*LngMRatio/2)    
                    )));
                    first.Lng += QSize.Width * LngMRatio;
                }

                GridCoordinates[3] = first.Lng;

                first.Lng = temp.Lng;
                first.Lat -= QSize.Height * LatMRatio;
            }
            GridCoordinates[0] = first.Lat;

            GridCoordinates[1] = temp.Lat;
            GridCoordinates[2] = temp.Lng;
        }

        // Draw a cell from left to right:
        private void DrawCell(PointLatLng first, bool hasq)
        {
            List<PointLatLng> points = new List<PointLatLng>();

            points.Add(first);
            first.Lng += QSize.Width * LngMRatio;
            points.Add(first);
            first.Lat -= QSize.Height * LatMRatio;
            points.Add(first);
            first.Lng -= QSize.Width * LngMRatio;
            points.Add(first);

            var pos = DiscreteLLPoint(new PointLatLng(points[0].Lat/2 + points[2].Lat/2, points[0].Lng/2 + points[1].Lng/2));

            var polygon = new GMapPolygon(points, "a grid cell");
            DrawCellPolygon(polygon, hasq);
            GridPolygons.Add(pos, polygon);
        }

        // Draw a cell from right to left:
        private void DrawCellR(PointLatLng first, bool hasq)
        {
            List<PointLatLng> points = new List<PointLatLng>();

            points.Add(first);
            first.Lng -= QSize.Width * LngMRatio;
            points.Add(first);
            first.Lat -= QSize.Height * LatMRatio;
            points.Add(first);
            first.Lng += QSize.Width * LngMRatio;
            points.Add(first);

            var pos = DiscreteLLPoint(new PointLatLng(points[0].Lat / 2 + points[2].Lat / 2, points[0].Lng / 2 + points[1].Lng / 2));

            var polygon = new GMapPolygon(points, "a grid cell");
            DrawCellPolygon(polygon, hasq);
            GridPolygons.Add(pos, polygon);
        }
        
        private void DrawCellPolygon(GMapPolygon cell, bool hasq)
        {
            cell.IsHitTestVisible = GridPolygonsClickable;
            cell.Stroke = (Pen)QDrawingTools[0];
            cell.Fill = (Brush)QDrawingTools[hasq || !_areaDefined ? 1 : 2];
            GridOverlay.Polygons.Add(cell);
        }

        public void RefreshGrid(PointLatLng[] points)
        {
            double[] newGridCoordinates = new double[] { GridCoordinates[0], GridCoordinates[1], GridCoordinates[2], GridCoordinates[3] };
            foreach (PointLatLng i in points)
            {
                if (i.Lat < newGridCoordinates[0])
                    newGridCoordinates[0] = i.Lat;
                else if (i.Lat > newGridCoordinates[1])
                    newGridCoordinates[1] = i.Lat;
                if (i.Lng < newGridCoordinates[2])
                    newGridCoordinates[2] = i.Lng;
                else if (i.Lng > newGridCoordinates[3])
                    newGridCoordinates[3] = i.Lng;
            }
            RefreshGrid(newGridCoordinates);
        }
        
        private void RefreshGrid(double[] newGridCoordinates)
        {
            PointLatLng first = new PointLatLng(GridCoordinates[1], GridCoordinates[2]);
            while (first.Lat < newGridCoordinates[1])
            {
                first.Lat += QSize.Height * LatMRatio;

                while (first.Lng < GridCoordinates[3])
                {
                    DrawCell(first, false);
                    first.Lng += QSize.Width * LngMRatio;
                }

                first.Lng = GridCoordinates[2];
            }
            GridCoordinates[1] = first.Lat;


            first = new PointLatLng(GridCoordinates[0], GridCoordinates[2]);
            while (first.Lat > newGridCoordinates[0])
            {
                while (first.Lng < GridCoordinates[3])
                {
                    DrawCell(first, false);
                    first.Lng += QSize.Width * LngMRatio;
                }

                first.Lng = GridCoordinates[2];
                first.Lat -= QSize.Height * LatMRatio;
            }
            GridCoordinates[0] = first.Lat;


            first = new PointLatLng(GridCoordinates[1], GridCoordinates[2]);
            while (first.Lat > GridCoordinates[0])
            {
                while (first.Lng > newGridCoordinates[2])
                {
                    DrawCellR(first, false);
                    first.Lng -= QSize.Width * LngMRatio;
                }

                newGridCoordinates[2] = first.Lng;
                first.Lng = GridCoordinates[2];
                first.Lat -= QSize.Height * LatMRatio;
            }
            GridCoordinates[2] = newGridCoordinates[2];
            

            first = new PointLatLng(GridCoordinates[1], GridCoordinates[3]);
            while (first.Lat > GridCoordinates[0])
            {
                while (first.Lng < newGridCoordinates[3])
                {
                    DrawCell(first, false);
                    first.Lng += QSize.Width * LngMRatio;
                }

                newGridCoordinates[3] = first.Lng;
                first.Lng = GridCoordinates[3];
                first.Lat -= QSize.Height * LatMRatio;
            }
            GridCoordinates[3] = newGridCoordinates[3];
        }
    }
}