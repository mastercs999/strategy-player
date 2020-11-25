using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [Serializable]
    public class Stats
    {
        public double Mean { get; set; }
        public double StdDev { get; set; }

        public Stats(double mean, double stdDev)
        {
            Mean = mean;
            StdDev = stdDev;
        }
    }
}
