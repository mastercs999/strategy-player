using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using Common.Extensions;

namespace Analyzer.TradingBase
{
    public class PerformanceReport
    {
        public List<Bundle> Trades { get; private set; }
        public decimal InitialCapital { get; private set; }

        public List<(int year, decimal gain)> YearGains { get; private set; }
        public List<(int year, int month, decimal gain)> MonthGains { get; private set; }

        public decimal Profit { get; private set; }
        public decimal GrossProfit { get; private set; }
        public decimal GrossLoss { get; private set; }
        public int NumberOfTrades { get; private set; }
        public double ProfitableTrades { get; private set; }
        public double LosingTrades { get; private set; }
        public decimal ProfitFactor { get; private set; }
        public decimal AverageMonthReturn { get; private set; }
        public decimal MedianMonthReturn { get; private set; }
        public decimal AverageYearReturn { get; private set; }
        public decimal MedianYearReturn { get; private set; }
        public decimal Return { get; private set; }
        public decimal DrawdownPercentage { get; private set; }
        public decimal DrawdownAbsolute { get; private set; }
        public decimal SafeRatio { get; private set; }
        public double AverageTradeDuration { get; private set; }




        public PerformanceReport(List<Bundle> trades, decimal initialCapital)
        {
            Trades = trades.OrderBy(x => x.DateTimeClosed).ThenByDescending(x => x.DateTimeOpened).ThenBy(x => x.Ticker).ThenBy(x => x.Shares).ToList();
            InitialCapital = initialCapital;

            decimal balance = initialCapital;
            YearGains = Trades.GroupBy(x => x.DateTimeClosed.Year).OrderBy(x => x.Key).Select(x =>
            {
                decimal profit = x.Sum(y => y.Profit);
                decimal gain = profit / balance * 100;

                balance += profit;

                return (x.Key, gain);
            }).ToList();

            balance = initialCapital;
            MonthGains = Trades.GroupBy(x => (year: x.DateTimeClosed.Year, month: x.DateTimeClosed.Month)).OrderBy(x => x.Key.year).ThenBy(x => x.Key.month).Select(x =>
            {
                decimal profit = x.Sum(y => y.Profit);
                decimal gain = profit / balance * 100;

                balance += profit;

                return (x.Key.year, x.Key.month, gain);
            }).ToList();

            // No analysis needed
            if (!Trades.Any())
                return;

            Profit = Trades.Sum(x => x.Profit);
            GrossProfit = Trades.Select(x => x.Profit).GrossProfit();
            GrossLoss = Trades.Select(x => x.Profit).GrossLoss();
            NumberOfTrades = Trades.Count;
            ProfitableTrades = Trades.Count(x => x.Profit >= 0) / (double)Trades.Count * 100;
            LosingTrades = Trades.Count(x => x.Profit < 0) / (double)Trades.Count * 100;
            ProfitFactor = Trades.Select(x => x.Profit).ProfitFactor();
            AverageMonthReturn = MonthGains.Average(x => x.gain);
            MedianMonthReturn = MonthGains.Select(x => x.gain).Median();
            AverageYearReturn = YearGains.Average(x => x.gain);
            MedianYearReturn = YearGains.Select(x => x.gain).Median();
            Return = Trades.Sum(x => x.Profit) / initialCapital * 100;
            DrawdownPercentage = Trades.Select(x => x.Profit).DrawdownPercentage(initialCapital);
            DrawdownAbsolute = Trades.Select(x => x.Profit).DrawdownAbsolute(initialCapital);
            SafeRatio = AverageYearReturn / (DrawdownPercentage == 0 ? 1 : DrawdownPercentage) * 100;
            AverageTradeDuration = Trades.Average(x => (x.DateTimeClosed - x.DateTimeOpened).TotalDays);
        }




        public void PrintStats()
        {
            int padRight = 35;

            Console.WriteLine();
            Console.WriteLine(String.Join("\n", new string[]
            {
                "Profit:".PadRight(padRight) + Math.Round(Profit, 2),
                "Gross Profit:".PadRight(padRight) + Math.Round(GrossProfit, 2),
                "Gross Loss:".PadRight(padRight) + Math.Round(GrossLoss, 2),
                "Trades:".PadRight(padRight) + NumberOfTrades,
                "Profitable Trades [%]:".PadRight(padRight) + Math.Round(ProfitableTrades, 2),
                "Losing Trades [%]:".PadRight(padRight) + Math.Round(LosingTrades, 2),
                "Profit Factor:".PadRight(padRight) + Math.Round(ProfitFactor, 2),
                "Average Month Return [%]:".PadRight(padRight) + Math.Round(AverageMonthReturn, 2),
                "Median Month Return [%]:".PadRight(padRight) + Math.Round(MedianMonthReturn, 2),
                "Average Year Return [%]:".PadRight(padRight) + Math.Round(AverageYearReturn, 2),
                "Median Year Return [%]:".PadRight(padRight) + Math.Round(MedianYearReturn, 2),
                "Return [%]:".PadRight(padRight) + Math.Round(Return, 2),
                "Drawdown [%]:".PadRight(padRight) + Math.Round(DrawdownPercentage, 2),
                "Drawdown [$]:".PadRight(padRight) + Math.Round(DrawdownAbsolute, 2),
                "Safe Ratio:".PadRight(padRight) + Math.Round(SafeRatio, 2),
                "Average Trade Duration [days]:".PadRight(padRight) + Math.Round(AverageTradeDuration, 2)
            }));
            Console.WriteLine();
        }
        public void ExportToExcel(string basicFilePath)
        {
            // Balances
            File.WriteAllLines(basicFilePath.AddVersion("Balance"), Trades.Select(x => x.Profit).CumulativeSum(InitialCapital).Select(x => x.ToString()));

            // Year gain
            File.WriteAllLines(basicFilePath.AddVersion("Year gain"), YearGains.Select(x => $"{x.year}{Utilities.CsvSeparator}{x.gain}"));

            // Month gains
            File.WriteAllLines(basicFilePath.AddVersion("Month gain"), MonthGains.Select(x => $"{x.year}{Utilities.CsvSeparator}{x.month}{Utilities.CsvSeparator}{x.gain}"));

            // All trades
            File.WriteAllLines(basicFilePath.AddVersion("Trades"), Trades.Select(x => String.Join("\n", x.ToString())));

            // Now calculate stats
            File.WriteAllLines(basicFilePath.AddVersion("Stats"), new string[]
            {
                "Profit" + Utilities.CsvSeparator + Profit,
                "Gross Profit" + Utilities.CsvSeparator + GrossProfit,
                "Gross Loss" + Utilities.CsvSeparator + GrossLoss,
                "Trades" + Utilities.CsvSeparator + NumberOfTrades,
                "Profitable Trades [%]" + Utilities.CsvSeparator + ProfitableTrades,
                "Losing Trades [%]" + Utilities.CsvSeparator + LosingTrades,
                "Profit Factor" + Utilities.CsvSeparator + ProfitFactor,
                "Average Month Return [%]" + Utilities.CsvSeparator + AverageMonthReturn,
                "Median Month Return [%]" + Utilities.CsvSeparator + MedianMonthReturn,
                "Average Year Return [%]" + Utilities.CsvSeparator + AverageYearReturn,
                "Median Year Return [%]" + Utilities.CsvSeparator + MedianYearReturn,
                "Return [%]" + Utilities.CsvSeparator + Return,
                "Drawdown [%]" + Utilities.CsvSeparator + DrawdownPercentage,
                "Drawdown [$]" + Utilities.CsvSeparator + DrawdownAbsolute,
                "Safe Ratio" + Utilities.CsvSeparator + SafeRatio,
                "Average Trade Duration [days]" + Utilities.CsvSeparator + AverageTradeDuration,
            });
        }
    }
}
