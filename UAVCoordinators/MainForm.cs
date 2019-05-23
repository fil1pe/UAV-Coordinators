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
            private MainForm _Form;
            private MouseEventHandler ClickEvent;
            public void RemoveEvents() { _Form.MouseClick -= ClickEvent; }

            public Button(MainForm form, PointF position, SizeF size, ButtonClick del)
            {
                Position = position;
                Size = size;
                _Form = form;
                ClickEvent = (sender, e) =>
                {
                    if (InsideRectangle(position, size.Width, size.Height, e.Location))
                        del(sender, e);
                };
                form.MouseClick += ClickEvent;
            }
        }

        private List<Connection> Connections = new List<Connection>();

        public MainForm()
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("en-US");
            InitializeComponent();
            ResizeRedraw = true;
            Size = new Size(640, 640);
            InitMap();
            Paint += DrawTopPanel;
            LoadSettings();
            InitHiddenComponents();

            for (int i = 0; i < 5; i++)
                TopBtnIcons.Add(new Bitmap(@"Images\top-btn-" + i + ".png"));
            for (int i = 0; i < 5; i++)
                TopBtnIcons.Add(new Bitmap(@"Images\top-btn-" + i + "-active.png"));

            // Examples:
            Connections.Add(new Uav(Color.Coral, this));
            Connections.Add(new Uav(Color.Aqua, this));
            List<PointLatLng> wp = new List<PointLatLng>();
            wp.Add(new PointLatLng(-26.271, -48.8930));
            wp.Add(new PointLatLng(-26.2718, -48.8945));
            wp.Add(new PointLatLng(-26.2701, -48.8935));
            (Connections[0] as Uav).WaypointsLL = wp;
        }

        private void LoadSettings()
        {
            List<string> settings = File.ReadLines(@"Data\settings").ToList();

            // Set map position:
            string[] mapPos = settings[0].Split(',');
            Map.Position = new PointLatLng(ParseDouble(mapPos[0]), ParseDouble(mapPos[1]));
            settings.RemoveAt(0);

            // Set map zoom:
            InitialZoom = ParseDouble(settings[0]);
            Map.Zoom = InitialZoom;
            settings.RemoveAt(0);

            // Set cell size and grid coordinates:
            string[] qSize = settings[0].Split(',');
            QSize = new SizeF (ParseFloat(qSize[0]), ParseFloat(qSize[1]));

            GPoint mapPixelPos = Map.MapProvider.Projection.FromLatLngToPixel(Map.Position, (int)Map.Zoom);

            PointLatLng bottomLeftPoint = Map.MapProvider.Projection.FromPixelToLatLng(
                new GPoint(mapPixelPos.X - Width / 2, mapPixelPos.Y + Height / 2),
                (int)Map.Zoom);

            PointLatLng topRightPoint = Map.MapProvider.Projection.FromPixelToLatLng(
                new GPoint(mapPixelPos.X + Width / 2, mapPixelPos.Y - Height / 2),
                (int)Map.Zoom);
            
            GridCoordinates = new double[]{ bottomLeftPoint.Lat, topRightPoint.Lat, bottomLeftPoint.Lng, topRightPoint.Lng };
            InitGrid();

            settings.RemoveAt(0);
        }

        private const int TopPanelHeight = 45;
        private const int TopPanelButtonSize = 30;
        private List<Button> TopPanelBtns= new List<Button>();
        private int TopActiveBtn = 2;
        private List<Bitmap> TopBtnIcons = new List<Bitmap>();

        private void DrawTopPanel(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Draw a bar:
            e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(0, 0, 24)), -5, 0, Width, TopPanelHeight);

            // Draw buttons:
            int btnsNum = 5;
            foreach (var i in TopPanelBtns) i.RemoveEvents();
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
                int btNum = i;
                TopPanelBtns.Add(new Button(this, p2, new SizeF(TopPanelButtonSize, TopPanelButtonSize),
                    (x, y) => { TopActiveBtn = btNum; Invalidate(); }
                ));
                p1.X += spacing;
            }
        }

        #region Hidden components

        private void InitHiddenComponents()
        {
        }

        #endregion

        private Size BodyPadding = new Size(20, 20);

        private void TopBtnAction(int btnNum, Graphics g)
        {
            switch (btnNum)
            {
                case 0:
                    Map.Visible = false;
                    DrawContainer("Logs", g);
                    break;
                case 1:
                    Map.Visible = false;
                    DrawContainer("Connections", g);
                    ShowConnections(g);
                    break;
                case 2:
                    Map.Visible = true;
                    break;
                case 3:
                    Map.Visible = false;
                    DrawContainer("Settings", g);
                    break;
                default:
                    Map.Visible = false;
                    DrawContainer("About", g);
                    break;
            }
        }

        private void DrawContainer(string s, Graphics g)
        {
            DrawContainer(new PointF(BodyPadding.Width, TopPanelHeight + BodyPadding.Height), ClientSize.Width - 2 * BodyPadding.Width, ClientSize.Height - TopPanelHeight - 2 * BodyPadding.Height, s, g);
        }

        private Font DefaultFont = new Font(new FontFamily("Arial"), 14);

        private void DrawContainer(PointF p1, int w, int h, string title, Graphics g)
        {
            SizeF strSize = g.MeasureString(title, DefaultFont);
            g.DrawString(title, DefaultFont, new SolidBrush(Color.White), new PointF(p1.X, p1.Y - strSize.Height/2));
            PointF p2 = new PointF(p1.X + strSize.Width, p1.Y),
                p3 = new PointF(p1.X + w, p1.Y),
                p4 = new PointF(p1.X + w, p1.Y + h),
                p5 = new PointF(p1.X, p1.Y + h);
            g.DrawLines(new Pen(Color.White, 2), new PointF[] { p2, p3, p4, p5, p1 });
        }

        private void ClosingForm(object sender, FormClosingEventArgs e)
        {
            // Save current settings to file:
            List<string> settings = new List<string>();
            PointLatLng mapPos = Map.Position;
            /*var dialogResult = MessageBox.Show("Do you want to save your workspace?", "Confirmation", MessageBoxButtons.YesNoCancel);
            if (dialogResult == DialogResult.Yes)
            {
                //
            }
            else if (dialogResult == DialogResult.Cancel)
            {
                e.Cancel = true;
                return;
            }*/
            settings.Add(mapPos.Lat + "," + mapPos.Lng);
            settings.Add("" + Map.Zoom);
            settings.Add(QSize.Width + "," + QSize.Height);
            File.WriteAllLines(@"Data\settings", settings);

            // Leave the program:
            Environment.Exit(0);
        }
    }
}
