using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UAVCoordinators
{
    internal partial class Utils
    {
        public static double ParseDouble(string str)
        {
            return double.Parse(str, CultureInfo.InvariantCulture);
        }

        public static float ParseFloat(string str)
        {
            return float.Parse(str, CultureInfo.InvariantCulture);
        }
    }
}
