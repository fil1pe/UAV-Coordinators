using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UAVCoordinators
{
    public class Connection
    {
        public string IpAddress { get; set; }
        public string Port { get; set; }
        public byte Status { get; set; }
    }
}
