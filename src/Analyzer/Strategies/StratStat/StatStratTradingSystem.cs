using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Analyzer.Data;
using Analyzer.Mocking;
using Analyzer.Mocking.Api;
using Analyzer.Mocking.Api.Models;
using Analyzer.TradingBase;
using Common.Loggers;
using Common.Extensions;
using Analyzer.Indicators;
using System.IO;
using Common;

namespace Analyzer.Strategies.StratStat
{
    public class StatStratTradingSystem : TradingSystem
    {
        private SharedConfig Config;

        public StatStratTradingSystem(SharedConfig config) : base(config)
        {
            Config = config;
        }




        private int DaysLookAhead;
        private int IndicatorIndex1;
        private int PredictorHistoryLength;
        private int Start = 8000;
        private int End;
        public override void Backtest()
        {
            // Load them
            Table table = new DataManager(Config.DataDirectory).CreateDataTable(true, true);
            End = table.Bars.Count;
            CalculateJustIndicators(table, new SilentLogger());

            List<(PerformanceReport report, string configuration)> reports = new List<(PerformanceReport report, string configuration)>(5000);

            string progressFile = Config.CustomFile.AddVersion("PROGRESS");
            HashSet<string> progressList = new HashSet<string>();
            if (File.Exists(progressFile))
                progressList = File.ReadAllLines(progressFile).ToHashSet();

            // Calculate indicators
            // RUN2 Results_21_30_1_10_True_10_0,75_Year gain
            // EMA CROSS Results_179_30_0_30_True_10_0,8_Year gain
            // STOCHASTICS CROSS Results_1061_10_8_5_2_True_20_0.8_Year gain
            // STOCHASTICS CROSS Results_1082_30_7_5_10_True_10_0.7_Year gain
            // STOCHASTIC CROSS Results_77_30_6_1000_True_20_0.6_Year gain
            foreach (int daysLookahead in new int[] { 3, 5, 10, 20, 30 })
                foreach (int indicatorIndex1 in new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14 })
                    foreach (int predictorHistoryLength in new int[] { 2, 3, 5, 10, 20, 30, 100, 300, 1000 })
                    {
                        DaysLookAhead = daysLookahead;
                        IndicatorIndex1 = indicatorIndex1;
                        PredictorHistoryLength = predictorHistoryLength;

                        CalculateIndicators(table, new SilentLogger());

                        foreach (bool progressive in new bool[] { false, true })
                            foreach (int maxBundles in new int[] { 3, 5, 10, 20 })
                                foreach (double border in new double[] { 0.1, 0.2, 0.25, 0.3, 0.4, 0.6, 0.7, 0.75, 0.8, 0.9 })
                                {
                                    string tag = $"{daysLookahead}_{indicatorIndex1}_{predictorHistoryLength}_{progressive}_{maxBundles}_{border}";
                                    Console.WriteLine(tag);
                                    if (progressList.Contains(tag))
                                        continue;

                                    // Make trading
                                    List<StratStatBundle> allBundles = new List<StratStatBundle>(5000);
                                    List<StratStatBundle> currentBundles = new List<StratStatBundle>(30);

                                    // We have to track available cash
                                    decimal cash = Config.Capital;

                                    for (int i = Start; i < End; ++i)
                                    {
                                        // Exit all trades that acoomplish the condition
                                        for (int k = currentBundles.Count - 1; k >= 0; --k)
                                        {
                                            // Find trade and bar
                                            StratStatBundle trade = currentBundles[k];
                                            StockBar closingBar = table.Bars[i].SingleOrDefault(x => x != null && x.Symbol.Ticker == trade.Ticker);
                                            if (closingBar == null)
                                                continue;

                                            // Check the condition
                                            if (trade.CanExitFunc(table, trade.StartRow, trade.Col, i))
                                            {
                                                // Exit the trade
                                                currentBundles.RemoveAt(k);
                                                trade.Exit(closingBar.Date, closingBar.AdjustedClose);
                                                allBundles.Add(trade);

                                                // Update cash
                                                cash += trade.CloseAssetValue;
                                            }
                                        }

                                        // Current opened trades
                                        HashSet<string> openedTickers = currentBundles.Select(x => x.Ticker).ToHashSet();

                                        // Determine bundle size
                                        int availableBundles = maxBundles - currentBundles.Count;
                                        decimal bundleSize = Math.Max(0, availableBundles == 0 ? 0 : (cash - 100) / availableBundles);

                                        // Open new position if possible
                                        StockBar barToOpen = table.Bars[i].Where(x => x != null && x.GetBarData<StratStatBarData>(Name).HasMatch && (border > 0.5 && x.GetBarData<StratStatBarData>(Name).BestPredictorResult.ProbabilityOfSuccess > border || border < 0.5 && x.GetBarData<StratStatBarData>(Name).BestPredictorResult.ProbabilityOfSuccess < border) && !openedTickers.Contains(x.Symbol.Ticker)).OrderByDescending(x => border > 0.5 ? x.GetBarData<StratStatBarData>(Name).BestPredictorResult.ProbabilityOfSuccess : -x.GetBarData<StratStatBarData>(Name).BestPredictorResult.ProbabilityOfSuccess).FirstOrDefault();
                                        if (barToOpen != null && currentBundles.Count + 1 <= maxBundles)
                                        {
                                            ulong shares = (ulong)(bundleSize / barToOpen.AdjustedClose);

                                            StratStatBundle trade = new StratStatBundle(barToOpen.Symbol.Ticker, Name, barToOpen.Date, 0, barToOpen.AdjustedClose, shares)
                                            {
                                                CanExitFunc = barToOpen.GetBarData<StratStatBarData>(Name).BestPredictorResult.CanExitFunc,
                                                StartRow = i,
                                                Col = table.Bars[i].ToList().IndexOf(barToOpen)
                                            };
                                            //if (allBundles.Count >= 3 && allBundles.Skip(allBundles.Count - 2).All(x => x.Profit > 0))
                                            //    trade.Fiction = true;

                                            currentBundles.Add(trade);

                                            // Update cash
                                            cash -= shares * barToOpen.AdjustedClose;
                                        }

                                        // Add to existings positions
                                        if (progressive)
                                        {
                                            List<StockBar> barsToAdd = table.Bars[i].Where((x, k) =>
                                                x != null &&
                                                openedTickers.Contains(x.Symbol.Ticker) &&
                                                currentBundles.Where(y => y.Ticker == x.Symbol.Ticker).OrderByDescending(y => y.DateTimeOpened).First().OpenPrice > x.AdjustedClose &&
                                                currentBundles.Count(y => y.Ticker == x.Symbol.Ticker) < 10
                                            ).ToList();
                                            foreach (StockBar toAdd in barsToAdd)
                                            {
                                                List<StratStatBundle> bundles = currentBundles.Where(y => y.Ticker == toAdd.Symbol.Ticker).ToList();

                                                int bundlesCountToBuy = 0;
                                                switch (bundles.Count)
                                                {
                                                    case 1: bundlesCountToBuy = 2; break;
                                                    case 3: bundlesCountToBuy = 3; break;
                                                    case 6: bundlesCountToBuy = 4; break;
                                                    default: throw new InvalidOperationException("Should never happen");
                                                }

                                                if (currentBundles.Count + bundlesCountToBuy > maxBundles)
                                                    continue;

                                                ulong shares = (ulong)(bundleSize / toAdd.AdjustedClose);
                                                if (shares == 0)
                                                    continue;

                                                StratStatBundle originalBundle = bundles.OrderBy(y => y.DateTimeOpened).First();
                                                currentBundles.AddRange(Enumerable.Range(0, bundlesCountToBuy).Select(x => new StratStatBundle(toAdd.Symbol.Ticker, Name, toAdd.Date, 0, toAdd.AdjustedClose, shares)
                                                {
                                                    CanExitFunc = originalBundle.CanExitFunc,
                                                    StartRow = originalBundle.StartRow,
                                                    Col = originalBundle.Col
                                                }));

                                                // Update cash
                                                cash -= shares * (ulong)bundlesCountToBuy * toAdd.AdjustedClose;
                                            }
                                        }

                                        // Check cash - just to be sure
                                        if (cash < 0)
                                            throw new InvalidOperationException($"Cash is {cash}");
                                    }

                                    // Exit all
                                    while (currentBundles.Any())
                                    {
                                        // Remove from active
                                        StratStatBundle bundle = currentBundles.First();
                                        currentBundles.RemoveAt(0);

                                        // Find closing bar
                                        StockBar closingBar = table.FindLastBar(bundle.Ticker, End - 1);

                                        // Close the trade
                                        bundle.Exit(closingBar.Date, closingBar.AdjustedClose);

                                        // Move to the processed ones
                                        allBundles.Add(bundle);

                                        // Update cash
                                        cash += bundle.CloseAssetValue;
                                    }

                                    // Analyze results
                                    PerformanceReport report = new PerformanceReport(allBundles.Cast<Bundle>().ToList(), Config.Capital);
                                    if (report.DrawdownPercentage < 30 && report.ProfitFactor > 1.5m && report.NumberOfTrades > 800 && report.AverageYearReturn > 10 && report.SafeRatio > 1)
                                        reports.Add((report, tag));
                                    else
                                        File.AppendAllText(progressFile, tag + "\n");
                                }
                    }

            // Export reports
            int count = 0;
            foreach ((PerformanceReport, string) report in reports.OrderBy(x => x.report.SafeRatio))
            {
                report.Item1.PrintStats();
                report.Item1.ExportToExcel(Config.ResultFile.AddVersion(count++).AddVersion(report.Item2));
            }
        }

        private void CalculateJustIndicators(Table table, ILogger logger)
        {
            // Calculate for every stock
            for (int col = 0; col < table.Bars[0].Length; ++col)
            {
                IIndicator<decimal>[] indicators = new IIndicator<decimal>[]
                {
                    new SimpleMovingAverage(5),
                    new SimpleMovingAverage(15),
                    new SimpleMovingAverage(60),
                    new SimpleMovingAverage(102),
                    new SimpleMovingAverage(200),
                    new WildersRsi(2),
                    new WildersRsi(40),
                    new WildersRsi(80),
                };

                for (int row = Start; row < End; ++row)
                {
                    // Find out statistics for this bar
                    StockBar current = table.Bars[row][col];
                    if (current == null)
                        continue;

                    // Save indicator values
                    current.NameToBarData.Add(Name, new StratStatBarData()
                    {
                        IndicatorValues = indicators.Select(x => x.Next(current.AdjustedClose)).ToArray()
                    });
                }
            }
        }

        public override StrategyStateBase CreateState() => new StrategyStateBase();
        public override void CalculateIndicators(Table table, ILogger logger)
        {
            // Calculate for every stock
            for (int col = 0; col < table.Bars[0].Length; ++col)
            {
                // Create predictors
                List<Predictor> predictors = new List<Predictor>();
                for (int f = 1; f < 2; f++)
                {
                    predictors.Add(new Predictor(PredictorHistoryLength, (t, r, c) =>
                    {
                        StockBar current = t.Bars[r][c];
                        return r > Start && current != null && t.Bars[r - 1][c] != null && current.GetBarData<StratStatBarData>(Name).IndicatorValues[IndicatorIndex1] > current.AdjustedClose && t.Bars[r - 1][c].GetBarData<StratStatBarData>(Name).IndicatorValues[IndicatorIndex1] < t.Bars[r - 1][c].AdjustedClose;
                    }, (t, r, c) =>
                    {
                        return r > Start && t.Bars[r][c] != null && r + DaysLookAhead < t.Bars.Count && t.Bars[r + DaysLookAhead][c] != null && t.Bars[r][c].AdjustedClose * 1.01m < t.Bars[r + DaysLookAhead][c].AdjustedClose;
                    }, (t, r1, c, r2) =>
                    {
                        StockBar current = t.Bars[r2][c];
                        return r1 > Start && (t.Bars[r1][c].AdjustedClose * 1.01m < t.Bars[r2][c].AdjustedClose || (t.Bars[r2][c].Date - t.Bars[r1][c].Date).TotalDays >= DaysLookAhead);
                    }));
                }

                //IIndicator[] indicators = new IIndicator[]
                //{
                //    new SimpleMovingAverage(5),
                //    new SimpleMovingAverage(15),
                //    new SimpleMovingAverage(60),
                //    new SimpleMovingAverage(102),
                //    new SimpleMovingAverage(200),
                //    new WildersRsi(2),
                //    new WildersRsi(40),
                //    new WildersRsi(80),
                //};

                for (int row = Start; row < End; ++row)
                {
                    // Update statitics
                    predictors.ForEach(x => x.UpdateStatistics(table, row - DaysLookAhead, col));

                    // Find out statistics for this bar
                    StockBar current = table.Bars[row][col];
                    if (current == null)
                        continue;

                    // Save indicator values
                    //current.IndicatorValues = indicators.Select(x => 0.0m).ToArray();

                    // Save result
                    current.GetBarData<StratStatBarData>(Name).StoreResults(predictors.Select(x => x.CurrentGuess(table, row, col)).ToArray());
                    //if (row % 2000 == 0)
                    //    Console.WriteLine(predictors.First().ProbabilityOfSuccess);
                }
            }
        }

        public override decimal ExitPositions(State state, List<IPosition> positions, ITradingFactory tradingFactory, IApiClient apiClient, StockBar[] latestBars, Table table, IAccount account, out List<IOrder> closeOrders, out List<Bundle> closedTrades, ILogger _____________________________________________________________________________logger)
        {
            throw new NotImplementedException();
        }

        public override void OpenPositions(State state, List<IPosition> positions, ITradingFactory tradingFactory, IApiClient apiClient, StockBar[] latestBars, Table table, IAccount account, decimal assignedAccountSize, decimal positionsValue, out List<IOrder> openOrders, out List<Bundle> openedTrades, ILogger _____________________________________________________________________________logger)
        {
            throw new NotImplementedException();
        }
    }
}
