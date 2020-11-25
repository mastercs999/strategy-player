using Analyzer.Strategies.FBLL;
using Analyzer.Strategies.Ninety;
using Analyzer.Strategies.StratStat;
using Analyzer.Strategies.Ultimate;
using Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.TradingBase
{
    [Serializable]
    public class StockBar
    {
        public DateTimeOffset Date { get; set; }
        public Symbol Symbol { get; set; }

        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Volume { get; set; }

        public decimal AdjustedOpen { get; set; }
        public decimal AdjustedHigh { get; set; }
        public decimal AdjustedLow { get; set; }
        public decimal AdjustedClose { get; set; }
        public decimal AdjustedVolume { get; set; }

        public Dictionary<string, object> NameToBarData { get; private set; }

        public StockBar()
        {
            NameToBarData = new Dictionary<string, object>();
        }




        public T GetBarData<T>(string name)
        {
            return (T)NameToBarData[name];
        }

        public override string ToString()
        {
            return String.Join(Utilities.CsvSeparator, Date.ToString(), Symbol.Ticker, Open, High, Low, Close, Volume, AdjustedOpen, AdjustedHigh, AdjustedLow, AdjustedClose, AdjustedVolume);
        }

        public static List<StockBar> Load(string file)
        {
            List<StockBar> bars = new List<StockBar>();

            // Load all lines
            string[] lines = File.ReadAllLines(file).Skip(1).ToArray();

            // Parse data
            for (int i = 0; i < lines.Length; ++i)
            {
                // Get fields
                string[] fields = lines[i].Split(new char[] { ',' });

                // Parse fields
                DateTimeOffset date = DateTimeOffset.ParseExact(fields[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
                decimal open = decimal.Parse(fields[1], CultureInfo.InvariantCulture);
                decimal high = decimal.Parse(fields[2], CultureInfo.InvariantCulture);
                decimal low = decimal.Parse(fields[3], CultureInfo.InvariantCulture);
                decimal close = decimal.Parse(fields[4], CultureInfo.InvariantCulture);
                decimal volume = decimal.Parse(fields[5], CultureInfo.InvariantCulture);

                // Adjust rest of the data
                decimal adjustedOpen = decimal.Parse(fields[6], CultureInfo.InvariantCulture);
                decimal adjustedHigh = decimal.Parse(fields[7], CultureInfo.InvariantCulture);
                decimal adjustedLow = decimal.Parse(fields[8], CultureInfo.InvariantCulture);
                decimal adjustedClose = decimal.Parse(fields[9], CultureInfo.InvariantCulture);
                decimal adjustedVolume = decimal.Parse(fields[10], CultureInfo.InvariantCulture);

                // Parse
                StockBar b = new StockBar()
                {
                    Date = date,

                    Open = open,
                    High = high,
                    Low = low,
                    Close = close,
                    Volume = volume,

                    AdjustedOpen = adjustedOpen,
                    AdjustedHigh = adjustedHigh,
                    AdjustedLow = adjustedLow,
                    AdjustedClose = adjustedClose,
                    AdjustedVolume = adjustedVolume
                };

                bars.Add(b);
            }

            return bars.ToList();
        }
    }
}
