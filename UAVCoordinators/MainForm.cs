using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using GMap.NET;
using static UAVCoordinators.Utils;

namespace UAVCoordinators
{
    public partial class MainForm : Form
    {
        private delegate void ButtonClick(object sender, MouseEventArgs e);

        private struct CustomButton
        {
            public PointF Position { get; }
            public SizeF Size { get; }
            private MainForm _Form;
            private MouseEventHandler ClickEvent;
            public void RemoveEvents() { _Form.MouseClick -= ClickEvent; }

            public CustomButton(MainForm form, PointF position, SizeF size, ButtonClick del)
            {
                Position = position;
                Size = size;
                _Form = form;
                ClickEvent = (sender, e) => {
                    if (InsideRectangle(position, size.Width, size.Height, e.Location))
                        del(sender, e);
                };
                form.MouseClick += ClickEvent;
            }
        }

        private Color NextUavColor
        {
            get
            {
                var rnd = new Random();
                return Color.FromArgb(rnd.Next(256), rnd.Next(256), rnd.Next(256));
            }
        }

        private Mutex ConnectionsListMutex = new Mutex();
        internal ConcurrentBag<Connection> Connections = new ConcurrentBag<Connection>();
        private string SupervisingPort = "";
        private Thread SupervisorThread;

        internal Connection GetConnection(string name, string ip, string port, int type)
        {
            ConnectionsListMutex.WaitOne();
            foreach (var i in Connections)
                if (i.Ip == ip && i.Port == port && i.ConnectionType == (type == 1 || type == 2 ? "tcp" : "udp"))
                    return i;
            ConnectionsListMutex.ReleaseMutex();

            Connection con = null;
            Invoke((MethodInvoker)delegate {
                con = AddConnection(name, ip, port, type);
            });
            return con;
        }

        public MainForm()
        {
            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("en-US");
            InitializeComponent();
            ResizeRedraw = true;
            Size = new Size(640, 640);
            InitMap();
            Paint += DrawTopPanel;
            InitHiddenComponents();
            LoadWorkspace();
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);

            for (int i = 0; i < 5; i++)
                TopBtnIcons.Add(new Bitmap(@"Images\top-btn-" + i + ".png"));
            for (int i = 0; i < 5; i++)
                TopBtnIcons.Add(new Bitmap(@"Images\top-btn-" + i + "-active.png"));

            new Thread(new ThreadStart(() => {
                while (true)
                {
                    ConnectionsListMutex.WaitOne();
                    foreach (var i in Connections)
                    {
                        if (!(i is Uav)) continue;
                        try
                        {
                            var uavInfo = GetUavInfo(i as Uav);
                            (i as Uav).CurrentPosition = (PointLatLng)uavInfo[0];
                            (i as Uav).Angle = (float)uavInfo[1];
                            (i as Uav).CurrentWpNum = (int)uavInfo[2];
                        }
                        catch (Exception)
                        {
                            /*LogBox.Invoke((MethodInvoker)delegate {
                                var text = "Could not obtain " + i.Name + "'s state\n";
                                if (LogBox.Text == "None") LogBox.Text = text;
                                else LogBox.Text += text;
                            });*/
                        }
                    }
                    ConnectionsListMutex.ReleaseMutex();
                    Thread.Sleep(100);
                }
            })).Start();
        }
        
        private PointLatLng DefinedMapPosition;
        private bool _isSupervisor = false;
        private bool IsSupervisor
        {
            get { return _isSupervisor; }
            set
            {
                if (value)
                {
                    SupervisorThread = new Thread(new ThreadStart(Supervisor));
                    SupervisorThread.Start();
                }
                else
                    SupervisorThread.Abort();

                _isSupervisor = value;
            }
        }

        private void LoadWorkspace()
        {
            List<string> settings = File.ReadLines(@"Data\settings").ToList();

            // Set map position:
            string[] mapPos = settings[0].Split(',');
            DefinedMapPosition = new PointLatLng(ParseDouble(mapPos[0]), ParseDouble(mapPos[1]));
            Map.Position = DefinedMapPosition;
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

            settings.RemoveAt(0);

            // Load quadrants of operation:
            List<string> quads = File.ReadLines(@"Data\areaofoperation").ToList();
            foreach (var i in quads)
            {
                string[] info = i.Split(',');
                Quadrants.Add(new Tuple<double, double>(
                    ParseDouble(info[0]), ParseDouble(info[1])
                ));
            }
            _areaDefined = Quadrants.Count > 0;

            InitGrid();

            // Load other settings:
            IsSupervisor = settings[0] == "1"; settings.RemoveAt(0);
            SupervisingPort = settings[0]; settings.RemoveAt(0);

            // Load UAVs and coordinators:
            List<string> conns = File.ReadLines(@"Data\connections").ToList();
            foreach (var i in conns)
            {
                string[] info = i.Split(',');
                AddConnection(info[0], info[1], info[2], int.Parse(info[3]), Color.FromArgb(int.Parse(info[4])), info[5], info[6]);
            }
        }

        private const int TopPanelHeight = 45;
        private const int TopPanelButtonSize = 30;
        private List<CustomButton> TopPanelBtns= new List<CustomButton>();
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
            TopPanelBtns = new List<CustomButton>();
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
                TopPanelBtns.Add(new CustomButton(this, p2, new SizeF(TopPanelButtonSize, TopPanelButtonSize),
                    (x, y) => { TopActiveBtn = btNum; Invalidate(); }
                ));
                p1.X += spacing;
            }
        }

        private Size ContainerMargin = new Size(20, 20);
        private const int ContainerTopPadding = 12;

        private void TopBtnAction(int btnNum, Graphics g)
        {
            switch (btnNum)
            {
                case 0:
                    QuadrantsSelectionPanel.Visible = false;
                    ConnectionsContainer.Visible = false;
                    NewConnectionPanel.Visible = false;
                    Map.Visible = false;
                    SettingsPanel.Visible = false;
                    DrawContainer("Logs", g);
                    LogBox.Visible = true;
                    LogBox.Size = new Size(ClientSize.Width - 2 * ContainerMargin.Width - 5, ClientSize.Height - TopPanelHeight - 2 * ContainerMargin.Height - 2 - ContainerTopPadding);
                    break;
                case 1:
                    QuadrantsSelectionPanel.Visible = false;
                    NewConnectionPanel.Visible = false;
                    Map.Visible = false;
                    SettingsPanel.Visible = false;
                    LogBox.Visible = false;
                    DrawContainer("Connections", g);
                    NewConnectionPanel.Location = new Point(ClientSize.Width/2 - NewConnectionPanel.Width/2, ContainerTopPadding + TopPanelHeight + ContainerMargin.Height);
                    NewConnectionPanel.Visible = true;
                    ConnectionsContainer.Visible = true;
                    ConnectionsContainer.Size = new Size(ClientSize.Width - 2 * ContainerMargin.Width - 5, ClientSize.Height - TopPanelHeight - 2 * ContainerMargin.Height - 2 - ContainerTopPadding - NewConnectionPanel.Height - 6);
                    break;
                case 2:
                    QuadrantsSelectionPanel.Visible = true;
                    ConnectionsContainer.Visible = false;
                    LogBox.Visible = false;
                    SettingsPanel.Visible = false;
                    NewConnectionPanel.Visible = false;
                    Map.Visible = true;
                    break;
                case 3:
                    QuadrantsSelectionPanel.Visible = false;
                    ConnectionsContainer.Visible = false;
                    NewConnectionPanel.Visible = false;
                    Map.Visible = false;
                    LogBox.Visible = false;
                    DrawContainer("Settings", g);
                    SettingsPanel.Visible = true;
                    SettingsPanel.Size = new Size(ClientSize.Width - 2 * ContainerMargin.Width - 5, ClientSize.Height - TopPanelHeight - 2 * ContainerMargin.Height - 2 - ContainerTopPadding);
                    break;
                default:
                    QuadrantsSelectionPanel.Visible = false;
                    ConnectionsContainer.Visible = false;
                    NewConnectionPanel.Visible = false;
                    SettingsPanel.Visible = false;
                    Map.Visible = false;
                    LogBox.Visible = false;
                    DrawContainer("About", g);
                    break;
            }
        }

        private void DrawContainer(string s, Graphics g)
        {
            DrawContainer(new PointF(ContainerMargin.Width, TopPanelHeight + ContainerMargin.Height), ClientSize.Width - 2 * ContainerMargin.Width, ClientSize.Height - TopPanelHeight - 2 * ContainerMargin.Height, s, g);
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

        #region Hidden components

        private RichTextBox LogBox;
        private Panel NewConnectionPanel, ConnectionsContainer, SettingsPanel;
        private TextBox ConNameTextBox, ConIpTextBox, ConPortTextBox;
        private ComboBox ConTypeCBox;
        private System.Windows.Forms.Button AddConButton;

        private void InitHiddenComponents()
        {
            // Text box for logs:
            LogBox = new RichTextBox();
            LogBox.Location = new Point(3 + ContainerMargin.Width, ContainerTopPadding + TopPanelHeight + ContainerMargin.Height);
            LogBox.Visible = false;
            LogBox.ScrollBars = RichTextBoxScrollBars.Vertical;
            LogBox.ReadOnly = true;
            LogBox.BorderStyle = BorderStyle.None;
            LogBox.BackColor = Color.FromArgb(0, 0, 28);
            LogBox.ForeColor = Color.White;
            LogBox.Text = "None";
            Controls.Add(LogBox);


            // Panel for adding connection:
            NewConnectionPanel = new Panel();
            NewConnectionPanel.Location = new Point(0, 0);
            NewConnectionPanel.Visible = false;

            Label[] labels = { new Label(), new Label(), new Label(), new Label() };
            ConIpTextBox = new TextBox();
            ConPortTextBox = new TextBox();
            ConNameTextBox = new TextBox();
            ConTypeCBox = new ComboBox();

            labels[0].Text = "IP:";
            labels[0].Size = new Size(/*20*/38, 20);
            labels[0].Location = new Point(0, 22);

            ConIpTextBox.TabIndex = 2;
            ConIpTextBox.MaxLength = 15;
            ConIpTextBox.Location = new Point(labels[0].Width + 2, 22);

            labels[1].Text = "Port:";
            labels[1].Size = new Size(/*29*/34, 20);
            labels[1].Location = new Point(ConIpTextBox.Location.X + ConIpTextBox.Width + 2, 22);

            ConPortTextBox.TabIndex = 3;
            ConPortTextBox.MaxLength = 5;
            ConPortTextBox.Location = new Point(labels[1].Location.X + labels[1].Width + 2, 22);

            labels[2].Text = "Name:";
            labels[2].Size = new Size(38, 20);

            ConNameTextBox.TabIndex = 0;
            ConNameTextBox.MaxLength = 15;
            ConNameTextBox.Location = new Point(labels[2].Width + 2, 0);

            labels[3].Text = "Type:";
            labels[3].Size = new Size(34, 20);
            labels[3].Location = new Point(ConNameTextBox.Location.X + ConNameTextBox.Width + 2, 0);

            ConTypeCBox.DropDownStyle = ComboBoxStyle.DropDownList;
            ConTypeCBox.TabIndex = 0;
            ConTypeCBox.Location = new Point(labels[3].Location.X + labels[3].Width + 2, 0);
            ConTypeCBox.Width = ConPortTextBox.Width;
            ConTypeCBox.Items.AddRange(new object[]{ "UDP UAV", "TCP UAV", "TCP Coordinator" });
            ConTypeCBox.SelectedIndex = 0;

            AddConButton = new System.Windows.Forms.Button();
            AddConButton.BackColor = SystemColors.Control;
            AddConButton.Size = new Size(40, 20);
            AddConButton.Location = new Point(ConTypeCBox.Location.X + ConTypeCBox.Width + 2, 0);
            AddConButton.Text = "Add";
            AddConButton.TabIndex = 4;

            AddConButton.Click += AddConnection_Click;

            foreach (var i in labels)
            {
                i.TextAlign = ContentAlignment.MiddleRight;
                i.ForeColor = Color.White;
                NewConnectionPanel.Controls.Add(i);
            }
            NewConnectionPanel.Controls.Add(ConNameTextBox);
            NewConnectionPanel.Controls.Add(ConTypeCBox);
            NewConnectionPanel.Controls.Add(ConIpTextBox);
            NewConnectionPanel.Controls.Add(ConPortTextBox);
            NewConnectionPanel.Controls.Add(AddConButton);

            NewConnectionPanel.Size = new Size(AddConButton.Location.X + AddConButton.Width, 42);

            Controls.Add(NewConnectionPanel);


            // Panel for connections:
            ConnectionsContainer = new Panel();
            ConnectionsContainer.Visible = false;
            ConnectionsContainer.HorizontalScroll.Maximum = 0;
            ConnectionsContainer.AutoScroll = false;
            ConnectionsContainer.VerticalScroll.Visible = false;
            ConnectionsContainer.AutoScroll = true;
            ConnectionsContainer.Size = new Size(ClientSize.Width - 2 * ContainerMargin.Width - 5, ClientSize.Height - TopPanelHeight - 2 * ContainerMargin.Height - 2 - ContainerTopPadding - NewConnectionPanel.Height - 6);
            ConnectionsContainer.Location = new Point(3 + ContainerMargin.Width, ContainerTopPadding + TopPanelHeight + ContainerMargin.Height + NewConnectionPanel.Height + 6);
            Controls.Add(ConnectionsContainer);
            ConnectionsContainer.Resize += (sender, e) => {
                foreach (Control i in ConnectionsContainer.Controls)
                    i.Width = ConnectionsContainer.ClientSize.Width;
            };


            // Panel for settings:
            SettingsPanel = new Panel();
            SettingsPanel.Visible = false;
            SettingsPanel.AutoScroll = true;
            SettingsPanel.Location = LogBox.Location;

            labels = new Label[] { new Label(), new Label(), new Label(), new Label(), new Label(), new Label(), new Label(), new Label() };
            TextBox[] textBoxes = new TextBox[] { new TextBox(), new TextBox(), new TextBox(), new TextBox(), new TextBox() };

            labels[0].Text = "Coordinator position";
            labels[1].Text = "Latitude:";
            labels[2].Text = "Longitude:";
            labels[3].Text = "Area of operation";
            labels[4].Text = "Other";
            labels[5].Text = "Grid width:";
            labels[6].Text = "Grid height:";
            labels[7].Text = "Port:";

            var subtitles = new Label[] { labels[0], labels[3], labels[4] };

            Font labelFont = new Font(new FontFamily("Arial"), 11);
            foreach (var i in labels)
            {
                var f = subtitles.Contains(i) ? labelFont : new Font(labelFont.FontFamily, labelFont.Size - 2);
                i.Height = textBoxes[0].Height;
                i.TextAlign = ContentAlignment.MiddleLeft;
                i.Font = f;
                i.Width = TextRenderer.MeasureText(i.Text, f).Width;
                i.ForeColor = Color.White;
                SettingsPanel.Controls.Add(i);
            }

            labels[1].Location = new Point(0, labels[0].Location.Y + labels[0].Height + 2);
            textBoxes[0].Location = new Point(labels[1].Location.X + labels[1].Width + 2, labels[1].Location.Y);
            labels[2].Location = new Point(textBoxes[0].Location.X + textBoxes[0].Width + 2, textBoxes[0].Location.Y);
            textBoxes[1].Location = new Point(labels[2].Location.X + labels[2].Width + 2, labels[2].Location.Y);

            EventHandler changePosition = (sender, e) => {
                if (UserChangedPosition)
                {
                    Map.Position = DefinedMapPosition = new PointLatLng(ParseDouble(textBoxes[0].Text), ParseDouble(textBoxes[1].Text));
                    DraggingResizingOrZoomingMap();
                    UserChangedPosition = false;
                }
            };

            textBoxes[0].Leave += changePosition;
            textBoxes[0].TextChanged += (sender, e) => UserChangedPosition = true;
            textBoxes[1].Leave += changePosition;
            textBoxes[1].TextChanged += (sender, e) => UserChangedPosition = true;

            SettingsPanel.Controls.AddRange(textBoxes);

            labels[3].Location = new Point(0, textBoxes[1].Location.Y + textBoxes[1].Height + 10);

            labels[5].Location = new Point(0, labels[3].Location.Y + labels[3].Height + 2);
            textBoxes[2].Location = new Point(labels[5].Location.X + labels[5].Width + 2, labels[5].Location.Y);
            labels[6].Location = new Point(textBoxes[2].Location.X + textBoxes[2].Width + 2, labels[5].Location.Y);
            textBoxes[3].Location = new Point(labels[6].Location.X + labels[6].Width + 2, labels[5].Location.Y);

            var btn = new System.Windows.Forms.Button();
            btn.Text = "Define new area";
            btn.Width = 100;
            btn.Location = new Point(0, labels[5].Location.Y + labels[5].Height + 2);
            btn.BackColor = SystemColors.Control;
            btn.Click += (sender, e) => {
                QSizeBackup = QSize;
                QSize = new SizeF(ParseFloat(textBoxes[2].Text), ParseFloat(textBoxes[3].Text));
                TopActiveBtn = 2;
                Invalidate();
                StartQuadrantsSelection();
            };
            SettingsPanel.Controls.Add(btn);

            labels[4].Location = new Point(0, btn.Location.Y + btn.Height + 10);

            var chBox = new CheckBox();
            chBox.Text = "Is supervisor";
            chBox.Font = new Font(labelFont.FontFamily, labelFont.Size - 2);
            chBox.ForeColor = Color.White;
            chBox.CheckAlign = ContentAlignment.MiddleLeft;
            chBox.Height = 20;
            chBox.Location = new Point(0, labels[4].Location.Y + labels[4].Height + 2);
            SettingsPanel.Controls.Add(chBox);
            chBox.CheckedChanged += (sender, e) => IsSupervisor = chBox.Checked;

            textBoxes[4].Location = new Point(labels[7].Width, chBox.Location.Y + chBox.Height);
            textBoxes[4].TextChanged += (sender, e) => UserChangedPort = true;
            textBoxes[4].MaxLength = 5;
            EventHandler changePort = (sender, e) => {
                if (UserChangedPort)
                {
                    SupervisingPort = textBoxes[4].Text;
                    UserChangedPort = false;
                }
            };
            textBoxes[4].Leave += changePort;
            labels[7].Location = new Point(0, textBoxes[4].Location.Y);

            SettingsPanel.Paint += (sender, e) => {
                var y = subtitles[0].Height / 2;
                foreach (var i in subtitles)
                    e.Graphics.DrawLine(new Pen(Color.White), i.Location.X + i.Width, i.Location.Y + y, SettingsPanel.Width, i.Location.Y + y);
            };

            SettingsPanel.VisibleChanged += (sender, e) => {
                changePosition.Invoke(sender, e);
                changePort.Invoke(sender, e);
                textBoxes[0].Text = DefinedMapPosition.Lat + "";
                textBoxes[1].Text = DefinedMapPosition.Lng + "";
                textBoxes[2].Text = QSize.Width + "";
                textBoxes[3].Text = QSize.Height + "";
                textBoxes[4].Text = SupervisingPort;
                chBox.Checked = IsSupervisor;
            };

            Controls.Add(SettingsPanel);


            // Panel for quadrants selection:
            QuadrantsSelectionPanel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right | AnchorStyles.Left;

            var saveBtn = new System.Windows.Forms.Button();
            saveBtn.Text = "Save";
            saveBtn.BackColor = SystemColors.Control;
            saveBtn.Location = new Point(QuadrantsSelectionPanel.Width - saveBtn.Width - 10, QuadrantsSelectionPanel.Height / 2 - saveBtn.Height / 2);
            QuadrantsSelectionPanel.Controls.Add(saveBtn);
            saveBtn.Anchor = AnchorStyles.Right;
            saveBtn.Click += (sender, e) => StopQuadrantsSelection(false);

            var cancelBtn = new System.Windows.Forms.Button();
            cancelBtn.Text = "Cancel";
            cancelBtn.BackColor = SystemColors.Control;
            cancelBtn.Location = new Point(saveBtn.Location.X - saveBtn.Width - 2, saveBtn.Location.Y);
            QuadrantsSelectionPanel.Controls.Add(cancelBtn);
            cancelBtn.Anchor = AnchorStyles.Right;
            cancelBtn.Click += (sender, e) => StopQuadrantsSelection(true);
        }

        private bool UserChangedPosition = false, UserChangedPort = false;

        #endregion

        private void Log(string s)
        {
            if (LogBox.Text == "None") LogBox.Text = "";
            LogBox.Text += s;
        }

        private void AddConnection_Click(object sender, EventArgs e)
        {
            var ip = ConIpTextBox.Text;
            var port = ConPortTextBox.Text;
            var name = ConNameTextBox.Text;

            AddConnection(ConNameTextBox.Text, ConIpTextBox.Text, ConPortTextBox.Text, ConTypeCBox.SelectedIndex);

            ConIpTextBox.Text = ConPortTextBox.Text = ConNameTextBox.Text = "";
            ConTypeCBox.SelectedIndex = 0;

            Invalidate();
        }

        private Connection AddConnection(string name, string ip, string port, int type)
        {
            if (type == 0 || type == 1) return AddConnection(name, ip, port, type, NextUavColor, "", "");
            return AddConnection(name, ip, port, type, Color.Empty, "", "");
        }

        private Connection AddConnection(string name, string ip, string port, int type, Color uavColor, string fileName, string missionCode)
        {
            int panelHeight = 74;

            Panel conPanel = new Panel();
            conPanel.Location = new Point(0, ConnectionsContainer.Controls.Count * panelHeight);
            conPanel.Size = new Size(ConnectionsContainer.ClientSize.Width, panelHeight);

            var labels = new Label[] { new Label(), new Label() };
            labels[0].Text = name;
            labels[1].Text = "IP: " + ip + ", port: " + port;

            var f = new Font(new FontFamily("Arial"), 11);
            foreach (var i in labels)
            {
                i.ForeColor = Color.White;
                i.Font = f;
                i.Width = TextRenderer.MeasureText(i.Text, f).Width;
                conPanel.Controls.Add(i);
            }

            int y = panelHeight - (labels[0].Height + labels[1].Height + 4);
            labels[0].Location = new Point(6 + 74 + 6, y);
            labels[1].Location = new Point(6 + 74 + 6, y + labels[0].Height + 4);

            Connection con = null;

            int btnWidth = 95;

            List<System.Windows.Forms.Button> btns = new List<System.Windows.Forms.Button>();
            btns.Add(new System.Windows.Forms.Button());
            btns[0].Text = "Delete";
            btns[0].Width = btnWidth;
            btns[0].Location = new Point(conPanel.ClientSize.Width - btns[0].Width, panelHeight/2 - btns[0].Height/2);

            if (type == 0 || type == 1)
            {
                con = new Uav(type == 0 ? "udp" : "tcp", ip, port, name, uavColor, this);
                if (fileName != "" && missionCode != "") LoadMission(con as Uav, fileName, missionCode);
                Connections.Add(con);

                var bmp = new Bitmap(74, 74);
                Graphics.FromImage(bmp).DrawImage((con as Uav).UavDrawingBmp, new Point(6, 8 + 4));
                conPanel.BackgroundImage = bmp;
                conPanel.BackgroundImageLayout = ImageLayout.None;

                btns[0].Location = new Point(conPanel.ClientSize.Width - btns[0].Width, panelHeight / 2 - btns[0].Height - 1);

                btns.Add(new System.Windows.Forms.Button());
                btns[1].Text = "Start mission";
                btns[1].Width = btnWidth;
                if ((con as Uav).MissionFileName == "") btns[1].Enabled = false;
                btns[1].Location = new Point(btns[0].Location.X - 2 - btns[1].Width, btns[0].Location.Y);
                btns[1].Click += (e, sender) => {
                    PauseResumeStartMission(con as Uav, btns[1]);
                };

                btns.Add(new System.Windows.Forms.Button());
                btns[2].Text = "Take off";
                btns[2].Width = btnWidth;
                btns[2].Location = new Point(conPanel.ClientSize.Width - btns[2].Width, btns[0].Location.Y + 2 + btns[0].Height);
                btns[2].Click += (e, sender) => { TakeOff(con as Uav); };

                btns.Add(new System.Windows.Forms.Button());
                btns[3].Text = "Home";
                btns[3].Width = btnWidth;
                btns[3].Location = new Point(btns[2].Location.X - 2 - btns[3].Width, btns[2].Location.Y);
                btns[3].Click += (e, sender) => { ReturnToHome(con as Uav); };

                btns.Add(new System.Windows.Forms.Button());
                btns[4].Text = (con as Uav).MissionFileName == "" ? "Load mission" : "Clear mission";
                btns[4].Width = btnWidth;
                btns[4].Location = new Point(btns[1].Location.X - 2 - btns[4].Width, btns[1].Location.Y);
                btns[4].Click += (sender, e) => {
                    try
                    {
                        if ((con as Uav).MissionFileName == "")
                        {
                            LoadMission(con as Uav);
                            btns[1].Enabled = true;
                            btns[1].Text = "Start mission";
                            btns[4].Text = "Clear mission";
                        }
                        else
                        {
                            (con as Uav).MissionFileName = "";
                            (con as Uav).MissionCode = "";
                            (con as Uav).MissionStatus = 0;
                            (con as Uav).WaypointsLL = null;
                            btns[1].Enabled = false;
                            btns[1].Text = "Start mission";
                            btns[4].Text = "Load mission";
                        }
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Invalid file format", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };
            }
            else
            {
                con = new Coordinator(ip, port, name, this);
                Connections.Add(con);

                btns.Add(new System.Windows.Forms.Button());
                btns[1].Text = "Connect";
                btns[1].Width = btnWidth;
                btns[1].Location = new Point(btns[0].Location.X - 2 - btns[1].Width, btns[0].Location.Y);
                btns[1].Click += (e, sender) => {
                    Cursor = Cursors.WaitCursor;

                    Coordinator c = con as Coordinator;
                    try
                    {
                        if (!c.Connected) c.Connect(); else c.Disconnect();
                        btns[1].Text = c.Connected ? "Disconnect" : "Connect";
                    }
                    catch(Exception err)
                    {
                        MessageBox.Show(err.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                    Cursor = Cursors.Default;
                };
            }
            
            foreach (var i in btns)
            {
                i.Anchor = AnchorStyles.Right;
                i.BackColor = SystemColors.Control;
                conPanel.Controls.Add(i);
            }

            btns[0].Click += (e, sender) => {
                btns[0].Enabled = false;
                new Thread(new ThreadStart(() => {
                    ConnectionsListMutex.WaitOne();
                    Connections = new ConcurrentBag<Connection>(Connections.Except(new[] { con }));
                    ConnectionsListMutex.ReleaseMutex();
                    Invoke((MethodInvoker)delegate {
                        var ind = ConnectionsContainer.Controls.IndexOf(conPanel);
                        for (int i = ind; i < ConnectionsContainer.Controls.Count; i++)
                        {
                            ConnectionsContainer.Controls[i].Location = new Point(0, ConnectionsContainer.Controls[i].Location.Y - panelHeight);
                        }
                        ConnectionsContainer.Controls.Remove(conPanel);
                    });
                })).Start();
            };

            ConnectionsContainer.Controls.Add(conPanel);

            return con;
        }

        private void ClosingForm(object sender, FormClosingEventArgs e)
        {
            // Save current settings to file:
            List<string> settings = new List<string>();
            settings.Add(DefinedMapPosition.Lat + "," + DefinedMapPosition.Lng);
            settings.Add("" + Map.Zoom);
            settings.Add(QSize.Width + "," + QSize.Height);
            settings.Add(IsSupervisor ? "1" : "0");
            settings.Add(SupervisingPort);
            File.WriteAllLines(@"Data\settings", settings);

            // Save connections to file:
            List<string> conns = new List<string>();
            foreach (var i in Connections)
            {
                string desc = i.Name + "," + i.Ip + "," + i.Port + ",";
                string missionFile = "", missionCode = "";
                if (i is Uav)
                {
                    if ((i as Uav).ConnectionType == "udp") desc += "0,";
                    else desc += "1,";
                    missionFile = (i as Uav).MissionFileName;
                    missionCode = (i as Uav).MissionCode;
                }
                else
                {
                    desc += "2,";
                    if ((i as Coordinator).Connected) (i as Coordinator).Disconnect(); // Disconnect with the coordinator
                }
                desc += i is Uav ? (i as Uav).UavColor.ToArgb().ToString() : "0";
                conns.Add(desc + "," + missionFile + "," + missionCode);
            }
            File.WriteAllLines(@"Data\connections", conns);

            // Save quadrants of operation:
            List<string> quads = new List<string>();
            foreach (var i in Quadrants)
            {
                quads.Add(i.Item1 + "," + i.Item2);
            }
            File.WriteAllLines(@"Data\areaofoperation", quads);

            // Leave the program:
            Environment.Exit(0);
        }
    }
}
