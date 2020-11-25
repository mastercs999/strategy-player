using Analyzer.Data;
using Analyzer.Mocking.Time;
using Analyzer.TradingBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Mocking.Data
{
    public interface IDataProvider
    {
        IDateTimeProvider DateTimeProvider { get; }
        bool HasData { get; }

        List<Symbol> GetSymbols();
        Table GetHistory();
        StockBar[] AppendOnlineData();
        decimal[] DownloadOnlineData(Symbol[] symbols);
        void CalculateIndicators(IEnumerable<Action<Table>> calculateActions);
    }
}
