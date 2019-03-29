using GMap.NET;
using static System.Math;
using System.Device.Location;
using System.Drawing;

namespace UAVCoordinators
{
    internal partial class Utils
    {
        public static PointLatLng NewLLPoint(PointLatLng old, double dx, double dy)
        {
            return NewLLPoint(old, dx, dy, old.Lat);
        }

        public static PointLatLng NewLLPoint(PointLatLng old, double dx, double dy, double lat)
        {
            double newLat = old.Lat + (180 / PI) * (dy / 6378137);
            double newLng = old.Lng + (180 / PI) * (dx / 6378137) / Cos(PI * lat / 180.0);

            return new PointLatLng(newLat, newLng);
        }

        public static double Distance(PointLatLng p1, PointLatLng p2)
        {
            GeoCoordinate c1 = new GeoCoordinate(p1.Lat, p1.Lng);
            GeoCoordinate c2 = new GeoCoordinate(p2.Lat, p2.Lng);
            return c1.GetDistanceTo(c2);
        }
    }
}
