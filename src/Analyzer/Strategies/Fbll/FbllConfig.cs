using Analyzer.TradingBase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Strategies.FBLL
{
    public class FbllConfig : SharedConfig
    {
        public int Bundles { get; set; }
        public int SmaPeriod { get; set; }
    }
}
