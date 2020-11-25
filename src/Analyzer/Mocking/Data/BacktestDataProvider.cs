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
    public class BacktestDataProvider : IDataProvider
    {
        private List<Symbol> Symbols;
        private Table Table;
        private List<StockBar[]> AllBars;
        private bool IndicatorsCalculated;
        private Dictionary<DateTimeOffset, StockBar[]> DateToBars;

        public IDateTimeProvider DateTimeProvider { get; private set; }
        public bool HasData => DateTimeProvider.Now <= AllBars.Last().First(x => x != null).Date;




        public BacktestDataProvider(string dataDirectory, IDateTimeProvider dateTimeProvider)
        {
            DateTimeProvider = dateTimeProvider;

            // Init privates
            DataManager dataManager = new DataManager(dataDirectory);

            Symbols = dataManager.FetchSymbols(true);
            Table = dataManager.CreateDataTable(true, true);
            AllBars = Table.Bars;
            IndicatorsCalculated = false;
            DateToBars = AllBars.ToDictionary(x => x.First(y => y != null).Date, x => x);

            // We begin with nothing history
            Table.Bars = new List<StockBar[]>(AllBars.Count);
        }




        public List<Symbol> GetSymbols()
        {
            return Symbols; 
        }

        public Table GetHistory()
        {
            Table.Bars.AddRange(MissingBarsTillToday(false));

            return Table;
        }

        public StockBar[] AppendOnlineData()
        {
            Table.Bars.AddRange(MissingBarsTillToday(true));

            return Table.Bars.Last();
        }

        public decimal[] DownloadOnlineData(Symbol[] symbols)
        {
            Dictionary<string, StockBar> tickerToBar = MissingBarsTillToday(true).Single().Where(x => x != null).ToDictionary(x => x.Symbol.Ticker);

            return symbols.Select(x => tickerToBar.TryGetValue(x.Ticker, out StockBar bar) ? bar.AdjustedClose : 0).ToArray();
        }

        public void CalculateIndicators(IEnumerable<Action<Table>> calculateActions)
        {
            if (!IndicatorsCalculated)
            {
                // Set all bars to the table
                List<StockBar[]> bars = Table.Bars;
                Table.Bars = AllBars;

                // Calculate them
                foreach (Action<Table> action in calculateActions)
                    action(Table);

                // Get it back
                Table.Bars = bars;

                IndicatorsCalculated = true;
            }
        }




        private IEnumerable<StockBar[]> MissingBarsTillToday(bool includeToday)
        {
            // Find from and to
            DateTimeOffset from = Table.Bars.Count == 0 ? AllBars.First().First(x => x != null).Date : Table.Bars.Last().First(x => x != null).Date.AddDays(1);
            DateTimeOffset to = DateTimeProvider.Today;

            // Add missing bars
            for (DateTimeOffset date = from; includeToday ? date <= to : date < to; date = date.AddDays(1))
                if (DateToBars.TryGetValue(date, out StockBar[] bars))
                    yield return bars;
        }
    }
}
