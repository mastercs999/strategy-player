using Analyzer.TradingBase;
using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Strategies.FBLL
{
    public class FbllBarData
    {
        public decimal Sma { get; set; }

        public StockBar PreviousBar1 { get; set; }
        public StockBar PreviousBar2 { get; set; }
        public StockBar PreviousBar3 { get; set; }

        public FbllBarData()
        {
        }

        public override string ToString()
        {
            string prev1 = PreviousBar1 == null ? "" : PreviousBar1.Date.ToString();
            string prev2 = PreviousBar2 == null ? "" : PreviousBar2.Date.ToString();
            string prev3 = PreviousBar3 == null ? "" : PreviousBar3.Date.ToString();

            return (base.ToString() + Utilities.CsvSeparator + String.Join(Utilities.CsvSeparator, Sma, prev1, prev2, prev3));
        }
    }
}
