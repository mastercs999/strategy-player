using Analyzer;
using Analyzer.Indicators;
using Analyzer.TradingBase;
using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Strategies.Ninety
{
    public class NinetyBarData
    {
        public bool IsValid { get; set; }
        public decimal LongerSma { get; set; }
        public decimal ShorterSma { get; set; }
        public decimal Rsi { get; set; }

        public NinetyBarData()
        {
        }

        public override string ToString()
        {
            return (base.ToString() + Utilities.CsvSeparator + String.Join(Utilities.CsvSeparator, IsValid, LongerSma, ShorterSma, Rsi));
        }
    }
}
