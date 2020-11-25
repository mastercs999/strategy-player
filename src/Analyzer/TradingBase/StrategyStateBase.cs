using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.TradingBase
{
    public class StrategyStateBase
    {
        public List<Bundle> Bundles { get; set; }
        public decimal MaxAccountValue { get; set; }
        public decimal MaxDrawdown { get; set; }
        public decimal CurrentDrawdown { get; set; }

        public StrategyStateBase()
        {
            Bundles = new List<Bundle>();
        }
    }
}
