using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.TradingBase.Composition
{
    public class StrategySettings
    {
        public TradingSystem Strategy { get; set; }
        public decimal Ratio { get; set; }




        public StrategySettings(TradingSystem strategy, decimal ratio)
        {
            Strategy = strategy;
            Ratio = ratio;
        }
    }
}
