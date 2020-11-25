using Analyzer.Indicators;
using Analyzer.TradingBase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Common.Extensions;
using System.Threading.Tasks;
using Common;

namespace Analyzer.Data
{
    public class Table
    {
        public Symbol[] Symbols { get; private set; }
        public List<StockBar[]> Bars { get; set; }

        private DataManager DataManager;
        private Dictionary<string, int> TickerToColumnIndex;




        public Table(DataManager dataManager, List<Symbol> symbols, string historicalDataPath)
        {
            // Load files
            Dictionary<Symbol, List<StockBar>> data = symbols.ToDictionary(x => x, x => StockBar.Load(Path.Combine(historicalDataPath, x.Ticker) + ".csv"));

            // Set properties
            Symbols = symbols.ToArray();
            Bars = CreateBarMatrix(data);

            DataManager = dataManager;
            TickerToColumnIndex = Symbols.WithIndex().ToDictionary(x => x.value.Ticker, x => x.index);
        }




        public void AppendOnlineData()
        {
            // Download
            decimal[] prices = DataManager.FetchOnlineDataParallel(Symbols);

            // Create bars
            StockBar[] newBars = new StockBar[Symbols.Length];
            for (int i = 0; i < newBars.Length; ++i)
                newBars[i] = new StockBar()
                {
                    Symbol = Symbols[i],
                    Date = DateTimeOffset.UtcNow.UtcDateTime.Date,

                    Open = decimal.MaxValue,
                    High = decimal.MaxValue,
                    Low = decimal.MaxValue,
                    Close = prices[i],
                    Volume = decimal.MaxValue,

                    AdjustedOpen = decimal.MaxValue,
                    AdjustedHigh = decimal.MaxValue,
                    AdjustedLow = decimal.MaxValue,
                    AdjustedClose = prices[i],
                    AdjustedVolume = decimal.MaxValue
                };

            // Append
            Bars.Add(newBars);
        }

        public StockBar FindLastBar(string ticker) => FindLastBar(ticker, Bars.Count - 1);
        public StockBar FindLastBar(string ticker, int rowIndex)
        {
            int column = TickerToColumnIndex[ticker];

            while (true)
                if (rowIndex < 0)
                    return null;
                else if (Bars[rowIndex][column] != null)
                    return Bars[rowIndex][column];
                else
                    --rowIndex;
        }

        public void PrintErrors()
        {
            for (int i = 1; i < Bars[0].Length; ++i)
            {
                StockBar previous = Bars[0][i];
                for (int j = 0; j < Bars.Count; ++j)
                {
                    // Make sure we know previous and current bar for the stock
                    StockBar current = Bars[j][i];
                    if (current == null)
                        continue;
                    if (previous == null)
                    {
                        previous = current;
                        continue;
                    }

                    // Determine error
                    decimal dayReturn = Utilities.Return(previous.AdjustedClose, current.AdjustedClose) * 100;
                    if (dayReturn < -35m)
                        Console.WriteLine($"{current.Symbol.Ticker}\t{previous.Date.Date.ToShortDateString()}\t{Math.Round(previous.AdjustedClose, 2)} -> {Math.Round(current.AdjustedClose, 2)}\t{Math.Round(dayReturn, 2)}%");

                    previous = current;
                }
            }
        }

        public void FixSplits()
        {
            // Bad splits found from investigating of PrintErrors
            //AGN     2/16/1993       11.69 -> 7.05   -39.70% -> No split
            //AGN     11/12/2001      46.64 -> 28.23  -39.47% -> No split
            //AIG     9/12/2008       190.99 -> 74.89 -60.79% -> No split
            //AIG     9/16/2008       59.00 -> 32.25  -45.33% -> No split
            //BA      5/17/1966       2.16 -> 1.16    -46.43% -> Split 2:1
            //BIIB    2/25/2005       67.28 -> 38.65  -42.55% -> No split
            //BK      11/9/1983       1.96 -> 1.00    -48.94% -> Split 2:1
            //BK      8/6/1996        8.58 -> 4.40    -48.72% -> Split 2:1
            //C       3/12/1987       45.89 -> 23.43  -48.94% -> Split 2:1
            //C       2/26/2009       23.86 -> 14.55  -39.02% -> No split
            //CAT     7/2/1964        2.72 -> 1.41    -48.12% -> Split 2:1
            //COF     7/16/2002       42.51 -> 25.61  -39.76% -> No split
            //CVX     12/10/1973      4.84 -> 2.40    -50.42% -> Split 2:1
            //CVX     3/10/1981       6.81 -> 3.35    -50.75% -> Split 2:1
            //DIS     11/15/1967      0.40 -> 0.20    -48.57% -> Split 2:1
            //DIS     2/26/1971       1.35 -> 0.71    -47.61% -> Split 2:1
            //HAL     12/6/2001       8.28 -> 4.77    -42.45% -> No split
            //IBM     4/22/1968       16.79 -> 8.65   -48.51% -> Split 2:1
            //KO      2/18/1965       0.39 -> 0.20    -49.92% -> Split 2:1
            //KO      5/31/1968       0.40 -> 0.20    -49.83% -> Split 2:1
            //LMT     9/8/1983        28.34 -> 9.21   -67.48% -> Split 3:1
            //LMT     3/15/1995       23.88 -> 15.13  -36.65% -> Split 1.63:1
            //PCLN    9/26/2000       111.84 -> 64.5  -42.33% -> No split
            //PCLN    10/4/2000       56.28 -> 34.86  -38.06% -> No split
            //PCLN    9/10/2001       30.0 -> 18.06   -39.80% -> No split
            //PG      5/18/1970       1.43 -> 0.69    -51.95% -> Split 2:1
            //RTN     10/11/1999      26.09 -> 14.71  -43.60% -> No split
            //USB     5/18/1998       21.37 -> 7.28   -65.91% -> Split 3:1
            Split[] badSplits = new Split[]
            {
                new Split("BA", new DateTimeOffset(1966, 5, 17, 0, 0, 0, TimeSpan.Zero), 2),
                new Split("BK", new DateTimeOffset(1983, 11, 9, 0, 0, 0, TimeSpan.Zero), 2),
                new Split("BK", new DateTimeOffset(1996, 8, 6, 0, 0, 0, TimeSpan.Zero), 2),
                new Split("C", new DateTimeOffset(1987, 3, 12, 0, 0, 0, TimeSpan.Zero), 2),
                new Split("CAT", new DateTimeOffset(1964, 7, 2, 0, 0, 0, TimeSpan.Zero), 2),
                new Split("CVX", new DateTimeOffset(1973, 12, 10, 0, 0, 0, TimeSpan.Zero), 2),
                new Split("CVX", new DateTimeOffset(1981, 3, 10, 0, 0, 0, TimeSpan.Zero), 2),
                new Split("DIS", new DateTimeOffset(1967, 11, 15, 0, 0, 0, TimeSpan.Zero), 2),
                new Split("DIS", new DateTimeOffset(1971, 2, 26, 0, 0, 0, TimeSpan.Zero), 2),
                new Split("IBM", new DateTimeOffset(1968, 4, 22, 0, 0, 0, TimeSpan.Zero), 2),
                new Split("KO", new DateTimeOffset(1965, 2, 18, 0, 0, 0, TimeSpan.Zero), 2),
                new Split("KO", new DateTimeOffset(1968, 5, 31, 0, 0, 0, TimeSpan.Zero), 2),
                new Split("LMT", new DateTimeOffset(1983, 9, 8, 0, 0, 0, TimeSpan.Zero), 3),
                new Split("LMT", new DateTimeOffset(1995, 3, 15, 0, 0, 0, TimeSpan.Zero), 1.63m),
                new Split("PG", new DateTimeOffset(1970, 5, 18, 0, 0, 0, TimeSpan.Zero), 2),
                new Split("USB", new DateTimeOffset(1998, 5, 18, 0, 0, 0, TimeSpan.Zero), 3),
            };

            foreach (Split split in badSplits)
            {
                int column = TickerToColumnIndex[split.Ticker];

                for (int i = Bars.Count - 1; i >= 0; --i)
                {
                    StockBar bar = Bars[i][column];
                    if (bar == null || bar.Date > split.LastDay)
                        continue;

                    bar.AdjustedOpen /= split.Ratio;
                    bar.AdjustedHigh /= split.Ratio;
                    bar.AdjustedLow /= split.Ratio;
                    bar.AdjustedClose /= split.Ratio;
                }
            }
        }

        public override string ToString()
        {
            StringBuilder str = new StringBuilder();

            // Pick one ticker to know how many fields are there
            string oneTickerRecord = Bars.First().First(x => x != null).ToString();
            int fields = oneTickerRecord.Count(x => x == Utilities.CsvSeparator[0]);
            string sepataratorsForEmtpy = new string(Utilities.CsvSeparator[0], fields);

            // Headers
            str.AppendLine(Utilities.CsvSeparator + String.Join(sepataratorsForEmtpy, Symbols.Select(x => x.Ticker)));

            // Data
            foreach (StockBar[] row in Bars)
                str.AppendLine(String.Join(Utilities.CsvSeparator, row.Select(x => x == null ? sepataratorsForEmtpy : x.ToString())));

            return str.ToString();
        }




        private List<StockBar[]> CreateBarMatrix(Dictionary<Symbol, List<StockBar>> data)
        {
            // Create helper dictionary that translater on what index is given datetime in every stock
            Dictionary<DateTimeOffset, int>[] dateToIndex = new Dictionary<DateTimeOffset, int>[Symbols.Length];
            for (int i = 0; i < Symbols.Length; ++i)
            {
                Symbol symbols = Symbols[i];

                dateToIndex[i] = new Dictionary<DateTimeOffset, int>();
                for (int k = 0; k < data[symbols].Count; ++k)
                    dateToIndex[i].Add(data[symbols][k].Date, k);
            }

            // Get max and min date
            DateTimeOffset minDate = data.Select(x => x.Value.First().Date).Min();
            DateTimeOffset maxDate = data.Select(x => x.Value.Last().Date).Max();

            // Create the data
            List<StockBar[]> bars = new List<StockBar[]>();
            for (DateTimeOffset date = minDate; date <= maxDate; date = date.AddDays(1))
            {
                // Bars on this date
                List<StockBar> dateBars = new List<StockBar>();

                // Find bar for every ticker
                for (int i = 0; i < Symbols.Length; ++i)
                {
                    Symbol symbol = Symbols[i];
                    if (dateToIndex[i].TryGetValue(date, out int index))
                        dateBars.Add(new StockBar()
                        {
                            Date = data[symbol][index].Date,
                            Symbol = symbol,

                            Open = data[symbol][index].Open,
                            High = data[symbol][index].High,
                            Low = data[symbol][index].Low,
                            Close = data[symbol][index].Close,
                            Volume = data[symbol][index].Volume,

                            AdjustedOpen = data[symbol][index].AdjustedOpen,
                            AdjustedHigh = data[symbol][index].AdjustedHigh,
                            AdjustedLow = data[symbol][index].AdjustedLow,
                            AdjustedClose = data[symbol][index].AdjustedClose,
                            AdjustedVolume = data[symbol][index].AdjustedVolume
                        });
                    else
                        dateBars.Add(null);
                }

                // No data for today
                if (dateBars.All(x => x == null))
                    continue;

                bars.Add(dateBars.ToArray());
            }

            // Set the data
            return bars;
        }




        private class Split
        {
            public string Ticker { get; set; }
            public DateTimeOffset LastDay { get; set; }
            public decimal Ratio { get; set; }




            public Split(string ticker, DateTimeOffset lastDay, decimal ratio)
            {
                Ticker = ticker;
                LastDay = lastDay;
                Ratio = ratio;
            }
        }
    }
}
