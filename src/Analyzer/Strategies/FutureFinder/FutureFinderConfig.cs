using Analyzer.TradingBase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Strategies.FutureFinder
{
    public class FutureFinderConfig : SharedConfig
    {
        public int Bundles { get; set; }
        public string MinDistanceFile => Path.Combine(BaseDirectory, "MinDistance", "min_distace.bin");
    }
}
