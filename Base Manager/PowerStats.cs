using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IngameScript
{
    public struct PowerStats
    {
        public PowerStats(double capacity, double production, double consumption, double stored)
        {
            Capacity = capacity;
            Production = production;
            Consumption = consumption;
            Stored = stored;
        }

        public double Capacity { get; set; }
        public double Production { get; set; }
        public double Consumption { get; set; }
        public double Stored { get; set; }
    }
}
