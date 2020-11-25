using Analyzer.TradingBase;
using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Strategies.Neural
{
    [Serializable]
    public class NeuralBarData
    {
        public decimal? NextChange { get; set; }
        public decimal Sma { get; set; }

        public NeuralBarData()
        {
            NextChange = null;
        }

        public override string ToString()
        {
            return (base.ToString() + Utilities.CsvSeparator + String.Join(Utilities.CsvSeparator, NextChange));
        }
    }
}
