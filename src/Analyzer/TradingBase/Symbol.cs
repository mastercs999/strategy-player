using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.TradingBase
{
    [Serializable]
    public class Symbol
    {
        public string Ticker { get; set; }
        public long Volume { get; set; }
    }
}
