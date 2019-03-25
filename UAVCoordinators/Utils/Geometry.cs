using System.Drawing;

namespace UAVCoordinators
{
    public partial class Utils
    {
        public static bool InsideRectangle(PointF rectPos, int w, int h, PointF pos)
        {
            return pos.X >= rectPos.X && pos.X <= rectPos.X + w && pos.Y >= rectPos.Y && pos.Y <= rectPos.Y + h;
        }
    }
}
