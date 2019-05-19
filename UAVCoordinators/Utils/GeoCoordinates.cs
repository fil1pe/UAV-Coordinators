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
    }
}
