using System;
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
        private delegate void ButtonClick(object sender, MouseEventArgs e);
        
        private struct Button
        {
            public PointF Position { get; }
            public SizeF Size { get; }

            public Button(MainForm form, PointF position, SizeF size, ButtonClick del)
            {
                Position = position;
                Size = size;
                form.MouseClick += (sender, e) =>
                {
                    if (InsideRectangle(position, size.Width, size.Height, e.Location))
                        del(sender, e);
                };
            }
        }

        private List<Uav> Uavs = new List<Uav>();

        public MainForm()
        {
            InitializeComponent();
            ResizeRedraw = true;
            Size = new Size(640, 640);
            InitMap();
            Paint += DrawTopPanel;
            LoadSettings();

            for (int i = 0; i < 5; i++)
                TopBtnIcons.Add(new Bitmap(@"Images\top-btn-" + i + ".png"));
            for (int i = 0; i < 5; i++)
                TopBtnIcons.Add(new Bitmap(@"Images\top-btn-" + i + "-active.png"));

            // Examples:
            Uavs.Add(new Uav(Color.Coral, this));
            List<PointLatLng> wp = new List<PointLatLng>();
            wp.Add(new PointLatLng(-26.271, -48.8930));
            wp.Add(new PointLatLng(-26.2718, -48.8945));
            wp.Add(new PointLatLng(-26.2701, -48.8935));
            Uavs[0].WaypointsLL = wp;
        }

        private void LoadSettings()
        {
            List<string> settings = File.ReadLines(@"Data\settings").ToList();

            // Set map position:
            string[] mapPos = settings[0].Split(',');
            AuxPosition = Map.Position = new PointLatLng(ParseDouble(mapPos[0]), ParseDouble(mapPos[1]));
            settings.RemoveAt(0);

            // Set cell size and grid coordinates:
            string[] qSize = settings[0].Split(',');
            QSize = new float[] { ParseFloat(qSize[0]), ParseFloat(qSize[1]) };

            GPoint mapPixelPos = Map.MapProvider.Projection.FromLatLngToPixel(AuxPosition, (int)Map.Zoom);

            PointLatLng bottomLeftPoint = Map.MapProvider.Projection.FromPixelToLatLng(
                new GPoint(mapPixelPos.X - Width / 2, mapPixelPos.Y + Height / 2),
                (int)Map.Zoom);

            PointLatLng topRightPoint = Map.MapProvider.Projection.FromPixelToLatLng(
                new GPoint(mapPixelPos.X + Width / 2, mapPixelPos.Y - Height / 2),
                (int)Map.Zoom);
            
            GridCoordinates = new double[]{ bottomLeftPoint.Lat, topRightPoint.Lat, bottomLeftPoint.Lng, topRightPoint.Lng };
            InitGrid();

            settings.RemoveAt(0);

            //
        }

        private const int TopPanelHeight = 45;
        private const int TopPanelButtonSize = 30;
        private List<Button> TopPanelBtns;
        private int TopActiveBtn = 2;
        private List<Bitmap> TopBtnIcons = new List<Bitmap>();

        private void DrawTopPanel(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Draw a bar:
            e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(0, 0, 24)), -5, 0, Width, TopPanelHeight);

            // Draw buttons:
            int btnsNum = 5;
            TopPanelBtns = new List<Button>();
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
                ButtonClick del = (sender, e) => { TopActiveBtn = i; Invalidate(); };
                TopPanelBtns.Add(new Button(this, p2, new SizeF(TopPanelButtonSize, TopPanelButtonSize), del));
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

        private static void DrawContainer(PointF p1, int w, int h, string title, Graphics g)
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
