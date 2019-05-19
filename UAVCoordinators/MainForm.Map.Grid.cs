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
        private double[] GridCoordinates;
        private GMapOverlay GridOverlay = new GMapOverlay();

        private PointLatLng DiscreteLLPoint(PointLatLng p)
        {
            return new PointLatLng(
                Ceiling(p.Lat / (QSize.Height * LatMRatio)) * QSize.Height * LatMRatio,
                Floor(p.Lng / (QSize.Width * LngMRatio)) * QSize.Width * LngMRatio);
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
                    DrawCell(first);
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
        private void DrawCell(PointLatLng first)
        {
            List<PointLatLng> points = new List<PointLatLng>();

            points.Add(first);
            first.Lng += QSize.Width * LngMRatio;
            points.Add(first);
            first.Lat -= QSize.Height * LatMRatio;
            points.Add(first);
            first.Lng -= QSize.Width * LngMRatio;
            points.Add(first);

            DrawCellPolygon(new GMapPolygon(points, "a grid cell"));
        }

        // Draw a cell from right to left:
        private void DrawCellR(PointLatLng first)
        {
            List<PointLatLng> points = new List<PointLatLng>();

            points.Add(first);
            first.Lng -= QSize.Width * LngMRatio;
            points.Add(first);
            first.Lat -= QSize.Height * LatMRatio;
            points.Add(first);
            first.Lng += QSize.Width * LngMRatio;
            points.Add(first);

            DrawCellPolygon(new GMapPolygon(points, "a grid cell"));
        }

        private object[] QDrawingTools = new object[] { new Pen(Color.FromArgb(32, 255, 255, 255), 2), new SolidBrush(Color.Empty) };

        private void DrawCellPolygon(GMapPolygon cell)
        {
            cell.Stroke = (Pen)QDrawingTools[0];
            cell.Fill = (Brush)QDrawingTools[1];
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
                    DrawCell(first);
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
                    DrawCell(first);
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
                    DrawCellR(first);
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
                    DrawCell(first);
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