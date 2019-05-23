using GMap.NET;
using static System.Math;
using System.Device.Location;
using System.Drawing;

namespace UAVCoordinators
{
    internal partial class Utils
    {
        public static double Distance(PointLatLng p1, PointLatLng p2)
        {
            GeoCoordinate c1 = new GeoCoordinate(p1.Lat, p1.Lng);
            GeoCoordinate c2 = new GeoCoordinate(p2.Lat, p2.Lng);
            return c1.GetDistanceTo(c2);
        }

        public static bool InsideRectangle(PointF rectPos, float w, float h, PointF pos)
        {
            return pos.X >= rectPos.X && pos.X <= rectPos.X + w && pos.Y >= rectPos.Y && pos.Y <= rectPos.Y + h;
        }

        public static PointF ToPointF(GPoint p)
        {
            return new PointF(p.X, p.Y);
        }

        public static double Distance(PointF p1, PointF p2)
        {
            return Sqrt(Pow(p2.X - p1.X, 2) + Pow(p2.Y - p1.Y, 2));
        }

        /*
         Return the point that belongs to the line segment between given points p1 and p2 such that the distance
         between p1 and this point is equal to given distance dist
         */
        public static PointF DistanceProjection(PointF p1, PointF p2, double dist)
        {
            if (p1.X == p2.X)
                return new PointF(p2.X, p2.Y > p1.Y ? p1.Y + (float)dist : p1.Y - (float)dist);

            double a = (double)(p2.Y - p1.Y) / (p2.X - p1.X);
            double b = p1.Y - a * p1.X;

            double x = dist / Sqrt(1 + Pow(a, 2));
            if (p1.X > p2.X) x *= -1;
            x += p1.X;
            double y = a * x + b;

            return new PointF((float)x, (float)y);
        }

        // Distance projection using distance between given points p1 and p:
        public static PointF DistanceProjection(PointF p1, PointF p2, PointF p)
        {
            return DistanceProjection(p1, p2, Distance(p1, p));
        }
    }
}
