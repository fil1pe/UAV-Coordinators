﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using GMap.NET;
using static UAVCoordinators.Utils;

namespace UAVCoordinators
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            ResizeRedraw = true;
            Size = new Size(640, 640);
            MapOrigin = new PointF((float)Map.Width/2, (float)Map.Height/2);
            Paint += DrawTopPanel;
            Map.MapProvider = GMap.NET.MapProviders.GoogleSatelliteMapProvider.Instance;
            Map.Overlays.Add(MapOverlay);
            Map.DragButton = MouseButtons.Left;
            Map.ShowCenter = false;
            LoadSettings();
            Map.Paint += PaintOnMap;
            Map.MouseDown += MouseDownOnMap;
            Map.MouseMove += MouseMoveOnMap;
            Map.MouseUp += MouseUpOnMap;
            Map.Resize += MapResizedOrZoomed;
            Map.MouseWheel += MapZoomed;
            MouseClick += MouseClickOnTopPanel;

            for (int i = 0; i < 5; i++)
                TopBtnIcons.Add(new Bitmap(@"Images\top-btn-" + i + ".png"));
            for (int i = 0; i < 5; i++)
                TopBtnIcons.Add(new Bitmap(@"Images\top-btn-" + i + "-active.png"));

            // Examples:
            Uavs.Add(new Uav(Color.Coral, this));
            Uavs[0].CurrentPosition = new PointLatLng(-26.271, -48.8940);
            List<PointLatLng> wp = new List<PointLatLng>();
            wp.Add(new PointLatLng(-26.271, -48.8930));
            wp.Add(new PointLatLng(-26.2718, -48.8945));
            wp.Add(new PointLatLng(-26.2701, -48.8935));
            Uavs[0].WaypointsLL = wp;
        }

        private void LoadSettings()
        {
            List<String> settings = File.ReadLines(@"Data\settings").ToList();

            // Set the map position:
            String[] mapPos = settings[0].Split(',');
            Map.Position = new PointLatLng(ParseDouble(mapPos[0]), ParseDouble(mapPos[1]));
            settings.RemoveAt(0);

            //
        }

        private const int TopPanelHeight = 45;
        private const int TopPanelButtonSize = 30;
        private List<PointF> TopPanelBtns;
        private int TopActiveBtn = 2;
        private List<Bitmap> TopBtnIcons = new List<Bitmap>();

        private void DrawTopPanel(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Draw a bar:
            e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(0, 0, 24)), -5, 0, Width, TopPanelHeight);

            // Draw buttons:
            int btnsNum = 5;
            TopPanelBtns = new List<PointF>();
            float spacing = (float)ClientSize.Width / (1 + btnsNum);
            PointF p1 = new PointF(spacing, TopPanelHeight/2 - TopPanelButtonSize/2);

            for (int i = 0; i < btnsNum; i++)
            {
                PointF p2 = new PointF(p1.X - TopPanelButtonSize/2, p1.Y);
                if (TopActiveBtn == i)
                {
                    e.Graphics.DrawImage(TopBtnIcons[i+5], p2);
                    TopBtnAction(i, e.Graphics);
                }
                else
                    e.Graphics.DrawImage(TopBtnIcons[i], p2);
                TopPanelBtns.Add(p2);
                p1.X += spacing;
            }
        }

        private Size BodyPadding = new Size(20, 20);

        private void TopBtnAction(int btnNum, Graphics g)
        {
            switch (btnNum)
            {
                case 0:
                    Map.Visible = false;
                    DrawContainer(new PointF(BodyPadding.Width, TopPanelHeight + BodyPadding.Height), ClientSize.Width - 2*BodyPadding.Width, ClientSize.Height - TopPanelHeight - 2*BodyPadding.Height, "Logs", g);
                    break;
                case 2:
                    Map.Visible = true;
                    break;
                default:
                    Map.Visible = false;
                    break;
            }
        }

        private void MouseClickOnTopPanel(object sender, MouseEventArgs e)
        {
            for (int i = 0; i < TopPanelBtns.Count; i++)
                if (InsideRectangle(TopPanelBtns[i], TopPanelButtonSize, TopPanelButtonSize, e.Location))
                {
                    TopActiveBtn = i;
                    Invalidate();
                }
        }

        private void DrawContainer(PointF p1, int w, int h, string title, Graphics g)
        {
            Font f = new Font(new FontFamily("Arial"), 14);
            SizeF strSize = g.MeasureString(title, f);
            g.DrawString(title, f, new SolidBrush(Color.White), new PointF(p1.X, p1.Y - strSize.Height/2));
            PointF p2 = new PointF(p1.X + strSize.Width, p1.Y),
                p3 = new PointF(p1.X + w, p1.Y),
                p4 = new PointF(p1.X + w, p1.Y + h),
                p5 = new PointF(p1.X, p1.Y + h);
            g.DrawLines(new Pen(Color.White, 2), new PointF[] { p2, p3, p4, p5, p1 });
        }
    }
}