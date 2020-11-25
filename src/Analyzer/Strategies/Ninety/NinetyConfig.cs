using Analyzer.TradingBase;
using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Strategies.Ninety
{
    public class NinetyConfig : SharedConfig
    {
        public int Bundles { get; set; }
        public int LongerSmaPeriod { get; set; }
        public int ShorterSmaPeriod { get; set; }
        public int RsiPeriod { get; set; }
    }
}
