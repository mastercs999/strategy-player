using Analyzer.TradingBase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Strategies.Ultimate
{
    public class UltimateConfig : SharedConfig
    {
        public int Bundles { get; set; }
        public int UltimateOscillatorPeriod1 { get; set; }
        public int UltimateOscillatorPeriod2 { get; set; }
        public int UltimateOscillatorPeriod3 { get; set; }
    }
}
