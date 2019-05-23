using System.Drawing;
using System.Windows.Forms;

namespace UAVCoordinators
{
    public partial class MainForm : Form
    {
        private void ShowConnections(Graphics g)
        {
            int xPointer = BodyPadding.Width + 2;
            int yPointer = TopPanelHeight + BodyPadding.Height + 2;
            int maxw = ClientSize.Width - 2 * BodyPadding.Width - 4;
            maxw -= 20;
            xPointer += 10;

            Pen p = new Pen(new SolidBrush(Color.FromArgb(60, 60, 60)), 1);
            Brush b = new SolidBrush(Color.FromArgb(0, 0, 60));

            g.FillRectangle(b, xPointer + maxw - 20, yPointer, 20, 20);
            yPointer += 20 + 2;

            b = new SolidBrush(Color.FromArgb(0, 0, 35));

            foreach (var i in Connections)
            {
                if (i is Uav) p.Color = (i as Uav).UavColor;
                g.DrawRectangle(p, xPointer, yPointer, maxw, 80);
                g.FillRectangle(b, xPointer, yPointer, maxw, 80);
                if (i is Uav)
                {
                    g.DrawImage((i as Uav).UavStaticBitmap, new PointF(xPointer + 7, yPointer + 7));
                }
                yPointer += 80 + 2;
            }
        }
    }
}