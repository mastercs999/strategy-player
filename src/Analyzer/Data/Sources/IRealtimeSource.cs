using Analyzer.TradingBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Data.Sources
{
    public interface IRealtimeSource
    {
        Dictionary<Symbol, decimal?> DownloadRealtime(List<Symbol> symbols, string workingDirectory);
        Dictionary<Symbol, decimal?> DownloadRealtime(List<Symbol> symbols, string workingDirectory, Action<Symbol, decimal> onPriceFound);
    }
}
