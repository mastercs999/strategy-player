using Analyzer.Data;
using Analyzer.TradingBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Strategies.StratStat
{
    public class StratStatBundle : Bundle
    {
        public Func<Table, int, int, int, bool> CanExitFunc { get; set; }
        public int StartRow { get; set; }
        public int Col { get; set; }
        public bool Fiction { get; set; }



        public StratStatBundle(string ticker, string strategyName, DateTimeOffset dateTimeOpened, decimal accountValueOpened, decimal openPrice, ulong shares) : base(ticker, strategyName, dateTimeOpened, accountValueOpened, openPrice, shares)
        {
        }
    }
}
