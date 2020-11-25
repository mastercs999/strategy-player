using Analyzer.Data;
using Analyzer.Mocking;
using Analyzer.Mocking.Api;
using Analyzer.Mocking.Api.Models;
using Analyzer.TradingBase;
using Common;
using Common.Extensions;
using Common.Loggers;
using Humanizer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Strategies.FutureFinder
{
    public class FutureFinderSystem : TradingSystem
    {
        private FutureFinderConfig Config;

        public FutureFinderSystem(FutureFinderConfig config) : base(config)
        {
            Config = config;
        }


        public override void Backtest()
        {
            // Load the data
            Table stocks = new DataManager(Config.DataDirectory).CreateDataTable(true, true);

            // Calculate indicators
            CalculateIndicators(stocks, new SilentLogger());

            // Normalize
            Normalize(stocks.Bars);

            // Make trading
            List<Bundle> allBundles = new List<Bundle>();
            List<Bundle> currentBundles = new List<Bundle>();
            int featuresLength = stocks.Bars.SelectMany(x => x).First(x => x != null && x.NameToBarData.ContainsKey(Name)).GetBarData<FutureFinderBarData>(Name).Features.Length;
            List<double?[]> history = stocks.Bars.Select(x => x.SelectMany(y => y == null || !y.NameToBarData.ContainsKey(Name) ? new double?[featuresLength] : y.GetBarData<FutureFinderBarData>(Name).Features.Cast<double?>()).ToArray()).ToList();
            Dictionary<string, int> tickerToIndex = stocks.Symbols.Select((x, i) => (x, i)).ToDictionary(x => x.x.Ticker, x => x.i);

            // Calculate minimums
            Dictionary<int, Dictionary<int, double>> allIndexToDistance = CalculateHistory(history);

            // We have to track account value
            decimal cash = Config.Capital;
            decimal maxAccountValue = cash;
            decimal maxDrawdown = 0;
            decimal drawdown = 0;

            for (int i = 0; i < stocks.Bars.Count; ++i)
            {
                // Exit all trades that acoomplish the condition
                for (int k = currentBundles.Count - 1; k >= 0; --k)
                {
                    // Find trade and bar
                    Bundle bundle = currentBundles[k];
                    StockBar closingBar = stocks.Bars[i].SingleOrDefault(x => x != null && x.Symbol.Ticker == bundle.Ticker);
                    if (closingBar == null)
                        continue;

                    // Exit the trade
                    currentBundles.RemoveAt(k);
                    bundle.Exit(closingBar.Date, closingBar.AdjustedClose);
                    allBundles.Add(bundle);

                    // Update cash
                    cash += bundle.CloseAssetValue;
                }

                // Find out current drawdown
                decimal assetValue = currentBundles.Sum(x => x.Shares * stocks.FindLastBar(x.Ticker, i).AdjustedClose);
                decimal currentAccountValue = cash + assetValue;
                if (currentAccountValue > maxAccountValue)
                    maxAccountValue = currentAccountValue;
                drawdown = (maxAccountValue - currentAccountValue) / maxAccountValue * 100;
                if (drawdown > maxDrawdown)
                    maxDrawdown = drawdown;

                decimal leverage = 1; // Utilities.Gauss(drawdown < 10 ? 10 : drawdown, 1, 10, 10) + 0.5m;

                // Determine bundle size
                int maxBundles = 1;
                int availableBundles = maxBundles - currentBundles.Count;
                decimal bundleSize = Math.Max(0, availableBundles == 0 ? 0 : (currentAccountValue * leverage - assetValue - Config.ConfidenceMinimum) / availableBundles);

                // Current opened trades
                List<string> openedTickers = currentBundles.Select(x => x.Ticker).Distinct().ToList();

                // Open new position if possible
                if (i > 100)
                {
                    int minIndex = allIndexToDistance[i].Where(x => x.Key > 90).OrderBy(x => Math.Abs(x.Value)).First().Key;

                    StockBar barToOpen = stocks.Bars[i].Where(x =>
                    {
                        if (x == null)
                            return false;
                        StockBar nextBar = stocks.Bars[minIndex + 1][tickerToIndex[x.Symbol.Ticker]];
                        return nextBar != null && (nextBar.AdjustedClose - nextBar.AdjustedOpen) > 0;
                    }).OrderByDescending(x =>
                    {
                        StockBar nextBar = stocks.Bars[minIndex + 1][tickerToIndex[x.Symbol.Ticker]];
                        return nextBar.AdjustedClose - nextBar.AdjustedOpen;
                    }).FirstOrDefault();
                    if (barToOpen != null && currentBundles.Count + 1 <= Config.Bundles)
                    {
                        ulong shares = (ulong)(bundleSize / barToOpen.AdjustedClose);
                        if (shares > 0)
                        {
                            Bundle trade = new Bundle(barToOpen.Symbol.Ticker, Name, barToOpen.Date, currentAccountValue, barToOpen.AdjustedClose, shares);

                            currentBundles.Add(trade);

                            // Update cash
                            cash -= shares * barToOpen.AdjustedClose;
                        }
                    }
                }
            }

            // Exit all
            while (currentBundles.Any())
            {
                // Remove from active
                Bundle bundle = currentBundles.First();
                currentBundles.RemoveAt(0);

                // Find closing bar
                StockBar closingBar = stocks.FindLastBar(bundle.Ticker, stocks.Bars.Count - 1);

                // Close the trade
                bundle.Exit(closingBar.Date, closingBar.AdjustedClose);

                // Move to the processed ones
                allBundles.Add(bundle);

                // Update cash
                cash += bundle.CloseAssetValue;
            }

            // Analyze results
            AnalyzeTrades(allBundles);
        }

        public override void CalculateIndicators(Table table, ILogger logger)
        {
            // Calculate for every stock
            for (int col = 0; col < table.Bars[0].Length; ++col)
            {
                for (int row = 1; row < table.Bars.Count; ++row)
                {
                    // Current bar
                    StockBar current = table.Bars[row][col];
                    if (current == null)
                        continue;
                    StockBar previous = table.Bars[row - 1][col];
                    if (previous == null)
                        continue;

                    current.NameToBarData.Add(Name, new FutureFinderBarData()
                    {
                        Features = new decimal[]
                        {
                            Utilities.Return(previous.AdjustedClose, current.AdjustedClose),
                            Utilities.Return(previous.AdjustedClose, current.AdjustedOpen),
                            Utilities.Return(previous.AdjustedClose, current.AdjustedHigh),
                            Utilities.Return(previous.AdjustedClose, current.AdjustedLow),
                        }.Select(x => (double)x).ToArray()
                    });
                }
            }
        }

        private void Normalize(List<StockBar[]> bars)
        {
            // Get all
            List<FutureFinderBarData> ffBars = bars.SelectMany(x => x).Where(x => x != null && x.NameToBarData.ContainsKey(Name)).Select(x => x.GetBarData<FutureFinderBarData>(Name)).ToList();

            // Normalize every feature
            for (int i = 0; i < ffBars.First().Features.Length; ++i)
            {
                double[] values = ffBars.Select(x => x.Features[i]).ToArray();
                double[] normalized = values.Normalize().ToArray();

                for (int j = 0; j < values.Length; ++j)
                    ffBars[j].Features[i] = normalized[j];
            }

        }
        private Dictionary<int, Dictionary<int, double>> CalculateHistory(List<double?[]> history)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Config.MinDistanceFile));

            // Time measuring
            int timeUnitsTotal = Enumerable.Range(1, history.Count).Sum();
            Stopwatch sw = new Stopwatch();
            sw.Start();

            return Utilities.LoadOrDo(Config.MinDistanceFile, () =>
            {
                Dictionary<int, Dictionary<int, double>> allIndexToDistance = new Dictionary<int, Dictionary<int, double>>();

                for (int currentIndex = 1; currentIndex < history.Count; ++currentIndex)
                {
                    // Time diagnose
                    Console.CursorTop = Math.Max(0, Console.CursorTop - 2);
                    Console.WriteLine(currentIndex + "/" + (history.Count - 1) + "\t\t\t");
                    double timeUnitsRemaining = timeUnitsTotal - Enumerable.Range(1, currentIndex).Sum();
                    double timeUnitsElapsed = timeUnitsTotal - timeUnitsRemaining;
                    double milisecondsRemaining = sw.ElapsedMilliseconds / timeUnitsElapsed * timeUnitsRemaining;
                    TimeSpan remaining = TimeSpan.FromMilliseconds(milisecondsRemaining);
                    Console.WriteLine("Time remaining: " + remaining.Humanize(2, true) + "\t\t\t");


                    Dictionary<int, double> indexToDistance = new Dictionary<int, double>();

                    for (int historyIndex = 0; historyIndex < currentIndex; ++historyIndex)
                    {
                        double distance = 0;
                        int c = 0;
                        for (int featureIndex = 0; featureIndex < history[0].Length; ++featureIndex)
                            if (history[historyIndex][featureIndex] != null && history[currentIndex][featureIndex] != null)
                            {
                                distance += (double)history[historyIndex][featureIndex] - (double)history[currentIndex][featureIndex];
                                ++c;
                            }

                        distance /= c;

                        indexToDistance[historyIndex] = distance;
                    }

                    allIndexToDistance[currentIndex] = indexToDistance;
                }

                return allIndexToDistance;
            });
        }

        public override StrategyStateBase CreateState()
        {
            throw new NotImplementedException();
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
