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

namespace UAVCoordinators
{
    public partial class MainForm : Form
    {
        private PointLatLng AuxPosition;
        private float[] QSize = new float[]{ 10, 10 }; // Quadrant size
        private double[] GridCoordinates;
        private GMapOverlay GridOverlay = new GMapOverlay();

        private void InitGrid()
        {
            AuxPosition = Map.Position;
            GridCoordinates = new double[] { AuxPosition.Lat - 0.001, AuxPosition.Lat + 0.001, AuxPosition.Lng - 0.001, AuxPosition.Lng + 0.001 };
            Map.Overlays.Add(GridOverlay);

            PointLatLng first = new PointLatLng(GridCoordinates[1], GridCoordinates[2]);

            while (first.Lat > GridCoordinates[0])
            {
                while (first.Lng < GridCoordinates[3])
                {
                    DrawCell(first);
                    first = NewLLPoint(first, QSize[0], 0, AuxPosition.Lat);
                }

                GridCoordinates[3] = first.Lng;

                first.Lng = GridCoordinates[2];
                first = NewLLPoint(first, 0, -QSize[1], AuxPosition.Lat);
            }
            GridCoordinates[0] = first.Lat;
        }

        // Draw a cell from left to right:
        private void DrawCell(PointLatLng first)
        {
            List<PointLatLng> points = new List<PointLatLng>();

            points.Add(first);
            points.Add(NewLLPoint(points[0], QSize[0], 0, AuxPosition.Lat));
            points.Add(NewLLPoint(points[0], QSize[0], -QSize[1], AuxPosition.Lat));
            points.Add(NewLLPoint(points[0], 0, -QSize[1], AuxPosition.Lat));

            DrawCellPolygon(new GMapPolygon(points, "a grid cell"));
        }

        // Draw a cell from right to left:
        private void DrawCellR(PointLatLng first)
        {
            List<PointLatLng> points = new List<PointLatLng>();

            points.Add(first);
            points.Add(NewLLPoint(points[0], -QSize[0], 0, AuxPosition.Lat));
            points.Add(NewLLPoint(points[0], -QSize[0], -QSize[1], AuxPosition.Lat));
            points.Add(NewLLPoint(points[0], 0, -QSize[1], AuxPosition.Lat));

            DrawCellPolygon(new GMapPolygon(points, "a grid cell"));
        }

        private object[] QDrawingTools = new object[] { new Pen(Color.FromArgb(32, 255, 255, 255), 2), new SolidBrush(Color.Empty) };

        private void DrawCellPolygon(GMapPolygon cell)
        {
            cell.Stroke = (Pen)QDrawingTools[0];
            cell.Fill = (Brush)QDrawingTools[1];
            GridOverlay.Polygons.Add(cell);
        }

        public void MissionChanged(List<PointLatLng> waypointsLL)
        {
            //??
        }

        // Expand the Grid:
        private void RefreshGrid(double[] newGridCoordinates)
        {
            PointLatLng first = new PointLatLng(GridCoordinates[1], GridCoordinates[2]);
            while (first.Lat < newGridCoordinates[1])
            {
                first = NewLLPoint(first, 0, QSize[1], AuxPosition.Lat);
                while (first.Lng < GridCoordinates[3])
                {
                    DrawCell(first);
                    first = NewLLPoint(first, QSize[0], 0, AuxPosition.Lat);
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
                    first = NewLLPoint(first, QSize[0], 0, AuxPosition.Lat);
                }
                first.Lng = GridCoordinates[2];
                first = NewLLPoint(first, 0, -QSize[1], AuxPosition.Lat);
            }
            GridCoordinates[0] = first.Lat;


            first = new PointLatLng(GridCoordinates[1], GridCoordinates[2]);
            while (first.Lat > GridCoordinates[0])
            {
                while (first.Lng > newGridCoordinates[2])
                {
                    DrawCellR(first);
                    first = NewLLPoint(first, -QSize[0], 0, AuxPosition.Lat);
                }
                newGridCoordinates[2] = first.Lng;
                first.Lng = GridCoordinates[2];
                first = NewLLPoint(first, 0, -QSize[1], AuxPosition.Lat);
            }
            GridCoordinates[2] = newGridCoordinates[2];


            first = new PointLatLng(GridCoordinates[1], GridCoordinates[3]);
            while (first.Lat > GridCoordinates[0])
            {
                while (first.Lng < newGridCoordinates[3])
                {
                    DrawCell(first);
                    first = NewLLPoint(first, QSize[0], 0, AuxPosition.Lat);
                }
                newGridCoordinates[3] = first.Lng;
                first.Lng = GridCoordinates[3];
                first = NewLLPoint(first, 0, -QSize[1], AuxPosition.Lat);
            }
            GridCoordinates[3] = newGridCoordinates[3];
        }
    }
}