using Analyzer.Indicators;
using CsQuery;
using SharpML.Recurrent.DataStructs;
using SharpML.Recurrent.Networks;
using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Analyzer.TradingBase;
using Analyzer.Data;
using Common.Loggers;
using Analyzer.Mocking.Api.Models;
using Analyzer.Mocking;
using Analyzer.Mocking.Api;

namespace Analyzer.Strategies.Neural
{
    public class NeuralTradingSystem : TradingSystem
    {
        private Config Config;

        public NeuralTradingSystem(Config config) : base(config)
        {
            Config = config;

            // Create directories
            Utilities.CreateDirectories(config);
        }

        public override void Backtest()
        {
            //// Download the data
            //Downloader.Download(Config.DataDirectory);

            //// Load them
            //Table<NeuralBar> stocks = Utils.LoadOrDo<Table<NeuralBar>>(Config.TableFile, () =>
            //{
            //    Table<NeuralBar> table = new Table<NeuralBar>(Config.DataDirectory);
            //    CreatePredictions(table, Config);

            //    return table;
            //});

            //// Make trading
            //List<NeuralTrade> trades = new List<NeuralTrade>();
            //List<NeuralTrade> currentTrades = new List<NeuralTrade>();

            //double bundleSize = Config.Capital / Config.Bundles;

            //Dictionary<string, List<double>> predictions = new Dictionary<string, List<double>>();
            //foreach (string ticker in stocks.Symbols)
            //    predictions.Add(ticker, new List<double>());


            //// stocks.Bars.Length - stocks.Bars.Where(x => x.First(y => y != null).Date.Year >= 2000).Count()
            //for (int i = stocks.Bars.Length - stocks.Bars.Where(x => x.First(y => y != null).Date.Year >= 2000).Count(); i < stocks.Bars.Length; ++i)
            //{
            //    foreach (NeuralBar nb in stocks.Bars[i].Where(x => x != null))
            //        predictions[nb.Ticker].Add(nb.NextChange);

            //    // Exit all = end of the data
            //    if (i + 1 == stocks.Bars.Length)
            //    {
            //        while (currentTrades.Any())
            //        {
            //            // Remove from active
            //            NeuralTrade trade = currentTrades.First();
            //            currentTrades.RemoveAt(0);

            //            // Find closing bar
            //            NeuralBar closingBar = stocks.Bars[i].SingleOrDefault(x => x != null && x.Ticker == trade.Ticker);
            //            if (closingBar == null)
            //                continue;

            //            // Close the trade
            //            trade.ClosePositions(closingBar.Date, closingBar.Close);

            //            // Move to the processed ones
            //            trades.Add(trade);
            //        }

            //        // Trading ended
            //        break;
            //    }

            //    // Exit all trades that acoomplish the condition
            //    for (int k = currentTrades.Count - 1; k >= 0; --k)
            //    {
            //        // Find trade and bar
            //        NeuralTrade trade = currentTrades[k];
            //        NeuralBar currentBar = stocks.Bars[i].SingleOrDefault(x => x != null && x.Ticker == trade.Ticker);
            //        if (currentBar == null)
            //            continue;

            //        // Check the condition
            //        double avg = predictions[trade.Ticker].Average();
            //        if (!double.IsNaN(currentBar.NextChange) && currentBar.NextChange < avg)
            //        {
            //            // Exit the trade
            //            currentTrades.RemoveAt(k);
            //            trade.ClosePositions(currentBar.Date, currentBar.Close);
            //            trades.Add(trade);
            //        }
            //    }

            //    // Current opened trades
            //    List<string> openedTickers = currentTrades.Select(x => x.Ticker).Distinct().ToList();

            //    // Open new position if possible
            //    foreach (NeuralBar barToOpen in stocks.Bars[i].Where(x => x != null && !double.IsNaN(x.NextChange) && x.NextChange > 0 && !openedTickers.Contains(x.Ticker)).OrderByDescending(x => x.NextChange))
            //    if (barToOpen != null && currentTrades.Sum(x => x.OpenedBundlesCount) + 1 <= Config.Bundles)
            //    {
            //        NeuralTrade trade = new NeuralTrade(barToOpen.Ticker, (int)(bundleSize / barToOpen.Close));
            //        trade.OpenPositions(barToOpen.Date, barToOpen.Close, 1);

            //        currentTrades.Add(trade);
            //    }
            //}

            //// Analyze results
            //AnalyzeTrades(trades);
        }




        public override StrategyStateBase CreateState() => new StrategyStateBase();
        public override void CalculateIndicators(Table table, ILogger logger)
        {
            //// Calculate for every stock
            //for (int col = 0; col < table.Bars[0].Length; ++col)
            //{
            //    List<NeuralBar> values = new List<NeuralBar>();
            //    NeuroNeuralNetwork network = null;
            //    IIndicator sma = new SimpleMovingAverage(200);

            //    for (int row = 0; row < table.Bars.Length; ++row)
            //    {
            //        // Current bar
            //        NeuralBar current = table.Bars[row][col];
            //        if (current == null)
            //            continue;

            //        // We want to work with adjusted data
            //        current.Close = current.AdjustedClose;

            //        // Store next value
            //        values.Add(current);
            //        if (values.Count < Config.BatchSize * 1)
            //            continue;

            //        // Retrain
            //        if (network == null || values.Count % config.BatchSize == 0)
            //            network = Utils.LoadOrDo<NeuroNeuralNetwork>(Config.ModelFile.AddVersion(current.Ticker + "_" + values.Count), () =>
            //            {
            //                // Get learning data
            //                List<NeuroItem> learningData = CreateNeuralData(values).ToList();
            //                //learningData = learningData.Skip(learningData.Count - 1000).ToList();

            //                // Train network
            //                NeuroNeuralNetwork nn = new NeuroNeuralNetwork(config);
            //                nn.Train(learningData);

            //                return nn;
            //            });

            //        // Create input
            //        double[] input = CreateInput(values);

            //        // Make prediction
            //        double[] output = network.Predict(input);

            //        // Store in the bar
            //        current.NextChange = output.First();
            //        current.Sma = sma.Next(current.Close);
            //    }

            //    Console.Write("\rCreating predictions: " + (col + 1) + "/" + table.Bars[0].Length + "                   ");
            //}
            //Console.WriteLine();
        }
        public override decimal ExitPositions(State state, List<IPosition> positions, ITradingFactory tradingFactory, IApiClient apiClient, StockBar[] latestBars, Table table, IAccount account, out List<IOrder> closeOrders, out List<Bundle> closedTrades, ILogger _____________________________________________________________________________logger)
        {
            throw new NotImplementedException();
        }
        public override void OpenPositions(State state, List<IPosition> positions, ITradingFactory tradingFactory, IApiClient apiClient, StockBar[] latestBars, Table table, IAccount account, decimal assignedAccountSize, decimal positionsValue, out List<IOrder> openOrders, out List<Bundle> openedTrades, ILogger _____________________________________________________________________________logger)
        {
            throw new NotImplementedException();
        }




        private IEnumerable<NeuroItem> CreateNeuralData(List<StockBar> bars)
        {
            for (int today = 6; today < bars.Count; ++today)
                yield return new NeuroItem(
                    CreateInput(bars, today - 1),
                    new double[] {
                        (double)Utilities.Return(bars[today - 1].Close, bars[today].Close)
                    });
        }
        private double[] CreateInput(List<StockBar> bars)
        {
            return CreateInput(bars, bars.Count - 1);
        }
        private double[] CreateInput(List<StockBar> bars, int lastKnow)
        {
            return new double[] {
                        (double)Utilities.Return(bars[lastKnow - 1].Close, bars[lastKnow - 0].Close),
                        (double)Utilities.Return(bars[lastKnow - 2].Close, bars[lastKnow - 1].Close),
                        (double)Utilities.Return(bars[lastKnow - 3].Close, bars[lastKnow - 2].Close),
                        (double)Utilities.Return(bars[lastKnow - 4].Close, bars[lastKnow - 3].Close),
                        (double)Utilities.Return(bars[lastKnow - 5].Close, bars[lastKnow - 4].Close),
                    };
        }
    }
}
