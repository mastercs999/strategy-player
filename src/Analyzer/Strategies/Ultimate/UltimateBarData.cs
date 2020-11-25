using Analyzer.TradingBase;
using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Strategies.Ultimate
{
    public class UltimateBarData
    {
        public decimal UltimateOscillator { get; set; }

        public UltimateBarData()
        {
        }

        public override string ToString()
        {
            return (base.ToString() + Utilities.CsvSeparator + String.Join(Utilities.CsvSeparator, UltimateOscillator));
        }
    }
}
