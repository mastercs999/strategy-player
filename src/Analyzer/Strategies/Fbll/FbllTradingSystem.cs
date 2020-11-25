using Analyzer.Data;
using Analyzer.Indicators;
using Common;
using Analyzer.TradingBase;
using CsQuery;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Loggers;
using Analyzer.Mocking.Api.Models;
using Analyzer.Mocking;
using Analyzer.Mocking.Api;
using Common.Extensions;
using CSharpAPISync.Support;

namespace Analyzer.Strategies.FBLL
{
    public class FbllTradingSystem : TradingSystem
    {
        private FbllConfig Config;

        public FbllTradingSystem(FbllConfig config) : base(config)
        {
            Config = config;
        }

        public override void Backtest()
        {
            // Load the data
            Table stocks = new DataManager(Config.DataDirectory).CreateDataTable(true, true);

            // Calculate indicators
            CalculateIndicators(stocks, new SilentLogger());

            // Make trading
            List<Bundle> allBundles = new List<Bundle>();
            List<Bundle> currentBundles = new List<Bundle>();

            // We have to track account value
            decimal cash = Config.Capital;
            decimal maxAccountValue = cash;
            decimal maxDrawdown = 0;
            decimal drawdown = 0;

            for (int i = 1; i < stocks.Bars.Count; ++i)
            {
                // Exit all trades that acoomplish the condition
                for (int k = currentBundles.Count - 1; k >= 0; --k)
                {
                    // Find trade and bar
                    Bundle bundle = currentBundles[k];
                    StockBar closingBar = stocks.Bars[i].SingleOrDefault(x => x != null && x.Symbol.Ticker == bundle.Ticker);
                    if (closingBar == null || closingBar.GetBarData<FbllBarData>(Name).PreviousBar1 == null)
                        continue;
                    StockBar previousBar = closingBar.GetBarData<FbllBarData>(Name).PreviousBar1;

                    // Check the condition
                    if (closingBar.AdjustedClose > closingBar.AdjustedOpen && Math.Abs(closingBar.AdjustedClose - closingBar.AdjustedOpen) > Math.Abs(previousBar.AdjustedClose - previousBar.AdjustedOpen))
                    {
                        // Exit the trade
                        currentBundles.RemoveAt(k);
                        bundle.Exit(closingBar.Date, closingBar.AdjustedClose);
                        allBundles.Add(bundle);

                        // Update cash
                        cash += bundle.CloseAssetValue;
                    }
                }

                // Find out current drawdown
                decimal assetValue = currentBundles.Sum(x => x.Shares * stocks.FindLastBar(x.Ticker, i).AdjustedClose);
                decimal currentAccountValue = cash + assetValue;

                if (currentAccountValue > maxAccountValue)
                    maxAccountValue = currentAccountValue;

                drawdown = (maxAccountValue - currentAccountValue) / maxAccountValue * 100;
                if (drawdown > maxDrawdown)
                    maxDrawdown = drawdown;

                decimal leverage = Utilities.Gauss(drawdown < 10 ? 10 : drawdown, 1, 10, 10) + 0.5m;

                // Determine bundle size
                int maxBundles = Config.Bundles;
                int availableBundles = maxBundles - currentBundles.Count;
                decimal bundleSize = Math.Max(0, availableBundles == 0 ? 0 : (currentAccountValue * leverage - assetValue - Config.ConfidenceMinimum) / availableBundles);

                // Current opened trades
                List<string> openedTickers = currentBundles.Select(x => x.Ticker).Distinct().ToList();

                // Open new position if possible
                StockBar barToOpen = stocks.Bars[i].Where(x => {
                    FbllBarData fbllBarData = x?.GetBarData<FbllBarData>(Name);
                    return x != null && !openedTickers.Contains(x.Symbol.Ticker) && fbllBarData.PreviousBar1 != null && fbllBarData.PreviousBar2 != null && fbllBarData.PreviousBar3 != null &&
                    x.AdjustedLow < fbllBarData.PreviousBar1.AdjustedLow &&
                    fbllBarData.PreviousBar1.AdjustedLow < fbllBarData.PreviousBar2.AdjustedLow &&
                    fbllBarData.PreviousBar2.AdjustedLow < fbllBarData.PreviousBar3.AdjustedLow &&
                    x.AdjustedClose > fbllBarData.Sma &&
                    !(x.AdjustedClose > x.AdjustedOpen && Math.Abs(x.AdjustedClose - x.AdjustedOpen) > Math.Abs(fbllBarData.PreviousBar1.AdjustedClose - fbllBarData.PreviousBar1.AdjustedOpen));
                }).OrderByDescending(x =>
                {
                    FbllBarData fbllBarData = x.GetBarData<FbllBarData>(Name);
                    return 100 *
                    (Utilities.Max(x.AdjustedHigh, fbllBarData.PreviousBar1.AdjustedHigh, fbllBarData.PreviousBar2.AdjustedHigh, fbllBarData.PreviousBar3.AdjustedHigh) - x.AdjustedClose) /
                    (Utilities.Max(x.AdjustedHigh, fbllBarData.PreviousBar1.AdjustedHigh, fbllBarData.PreviousBar2.AdjustedHigh, fbllBarData.PreviousBar3.AdjustedHigh) / Utilities.Min(x.AdjustedLow, fbllBarData.PreviousBar1.AdjustedLow, fbllBarData.PreviousBar2.AdjustedLow, x.AdjustedLow));
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




        public override StrategyStateBase CreateState() => new StrategyStateBase();
        public override void CalculateIndicators(Table table, ILogger logger)
        {
            // Calculate for every stock
            for (int col = 0; col < table.Bars[0].Length; ++col)
            {
                // Create the indicators
                IIndicator<decimal> sma = new SimpleMovingAverage(Config.SmaPeriod);

                for (int row = 0; row < table.Bars.Count; ++row)
                {
                    // Current bar
                    StockBar current = table.Bars[row][col];
                    if (current == null)
                        continue;

                    // Calculate the indicators
                    current.NameToBarData.Add(Name, new FbllBarData()
                    {
                        Sma = sma.Next(current.AdjustedClose),
                        PreviousBar1 = FindPreviousBar(table, 1, row, col),
                        PreviousBar2 = FindPreviousBar(table, 2, row, col),
                        PreviousBar3 = FindPreviousBar(table, 3, row, col)
                    });
                }
            }
        }
        public override decimal ExitPositions(State state, List<IPosition> positions, ITradingFactory tradingFactory, IApiClient apiClient, StockBar[] latestBars, Table table, IAccount account, out List<IOrder> closeOrders, out List<Bundle> closedTrades, ILogger _____________________________________________________________________________logger)
        {
            decimal exitSize = 0;
            closeOrders = new List<IOrder>();
            closedTrades = new List<Bundle>();

            // Get current state
            StrategyStateBase strategyState = state.GetStrategyState<StrategyStateBase>(Config.Name);

            // Exit all trades that acoomplish the condition
            foreach (IGrouping<string, Bundle> tickerToBundles in strategyState.Bundles.ToLookup(x => x.Ticker))
            {
                // Find current bar
                StockBar closingBar = latestBars.SingleOrDefault(x => x != null && x.Symbol.Ticker == tickerToBundles.Key);
                if (closingBar == null || closingBar.GetBarData<FbllBarData>(Name).PreviousBar1 == null)
                    continue;
                StockBar previousBar = closingBar.GetBarData<FbllBarData>(Name).PreviousBar1;

                // Can we exit?
                if (closingBar.AdjustedClose > closingBar.AdjustedOpen && Math.Abs(closingBar.AdjustedClose - closingBar.AdjustedOpen) > Math.Abs(previousBar.AdjustedClose - previousBar.AdjustedOpen))
                {
                    // Find the position
                    IPosition position = positions.Single(x => x.Product.Symbol == tickerToBundles.Key);

                    // How many shares we hold?
                    ulong shares = tickerToBundles.Sum(x => x.Shares);

                    // Create exit order
                    _____________________________________________________________________________logger.Info($"Creating order for exiting the position on {tickerToBundles.Key} with size {shares}");
                    IOrder order = tradingFactory.CreateMarketOrder(position.Product, OrderAction.Sell, shares);

                    // Place the order
                    _____________________________________________________________________________logger.Info($"Placing order for exiting the position on {tickerToBundles.Key} with size {shares}");
                    apiClient.PlaceOrder(order, false);
                    closeOrders.Add(order);

                    // Close bundles
                    foreach (Bundle bundle in tickerToBundles)
                        bundle.Exit(closingBar.Date, closingBar.AdjustedClose);

                    // Add closed bundles to the output
                    closedTrades.AddRange(tickerToBundles);

                    // Update current state
                    strategyState.Bundles.RemoveAll(x => x.Ticker == tickerToBundles.Key);

                    // Update exit size
                    exitSize += closingBar.AdjustedClose * shares;
                }
            }

            return exitSize;
        }
        public override void OpenPositions(State state, List<IPosition> positions, ITradingFactory tradingFactory, IApiClient apiClient, StockBar[] latestBars, Table table, IAccount account, decimal assignedAccountSize, decimal positionsValue, out List<IOrder> openOrders, out List<Bundle> openedTrades, ILogger _____________________________________________________________________________logger)
        {
            openOrders = new List<IOrder>();
            openedTrades = new List<Bundle>();

            // Get current state
            StrategyStateBase strategyState = state.GetStrategyState<StrategyStateBase>(Config.Name);

            // Calculate leverage
            decimal leverage = Utilities.Gauss(strategyState.CurrentDrawdown < 10 ? 10 : strategyState.CurrentDrawdown, 1, 10, 10) + 0.5m;
            _____________________________________________________________________________logger.Info($"We have calculated leverage {leverage}");

            // Determine account size and bundle size
            int maxBundles = Config.Bundles;
            int availableBundles = maxBundles - strategyState.Bundles.Count;
            decimal bundleSize = Math.Max(0, availableBundles == 0 ? 0 : (assignedAccountSize * leverage - positionsValue - Config.ConfidenceMinimum) / availableBundles);
            _____________________________________________________________________________logger.Info($"Max bundle count is {maxBundles}, available bundles are {availableBundles}, assigned account size is {assignedAccountSize} and we hold securities for {positionsValue}, therefore bundle size is {bundleSize}.");

            // Current opened trades
            HashSet<string> openedTickers = strategyState.Bundles.Select(x => x.Ticker).ToHashSet();
            _____________________________________________________________________________logger.Info($"We should have opened following tickers:", openedTickers.Dump());

            // Open new position if possible
            _____________________________________________________________________________logger.Info("Checking whether we can open a new positions...");
            StockBar barToOpen = latestBars.Where(x => {
                FbllBarData fbllBarData = x?.GetBarData<FbllBarData>(Name);
                return x != null && !openedTickers.Contains(x.Symbol.Ticker) && fbllBarData.PreviousBar1 != null && fbllBarData.PreviousBar2 != null && fbllBarData.PreviousBar3 != null &&
                x.AdjustedLow < fbllBarData.PreviousBar1.AdjustedLow &&
                fbllBarData.PreviousBar1.AdjustedLow < fbllBarData.PreviousBar2.AdjustedLow &&
                fbllBarData.PreviousBar2.AdjustedLow < fbllBarData.PreviousBar3.AdjustedLow &&
                x.AdjustedClose > fbllBarData.Sma &&
                !(x.AdjustedClose > x.AdjustedOpen && Math.Abs(x.AdjustedClose - x.AdjustedOpen) > Math.Abs(fbllBarData.PreviousBar1.AdjustedClose - fbllBarData.PreviousBar1.AdjustedOpen));
            }).OrderByDescending(x =>
            {
                FbllBarData fbllBarData = x.GetBarData<FbllBarData>(Name);
                return 100 *
                (Utilities.Max(x.AdjustedHigh, fbllBarData.PreviousBar1.AdjustedHigh, fbllBarData.PreviousBar2.AdjustedHigh, fbllBarData.PreviousBar3.AdjustedHigh) - x.AdjustedClose) /
                (Utilities.Max(x.AdjustedHigh, fbllBarData.PreviousBar1.AdjustedHigh, fbllBarData.PreviousBar2.AdjustedHigh, fbllBarData.PreviousBar3.AdjustedHigh) / Utilities.Min(x.AdjustedLow, fbllBarData.PreviousBar1.AdjustedLow, fbllBarData.PreviousBar2.AdjustedLow, x.AdjustedLow));
            }).FirstOrDefault();
            if (barToOpen != null && strategyState.Bundles.Count + 1 <= Config.Bundles)
            {
                // Determine product and quantity
                _____________________________________________________________________________logger.Info($"We can open a new position for {barToOpen.Symbol.Ticker}", $"Getting product for {barToOpen.Symbol.Ticker}");
                IProduct product = apiClient.FindProduct(barToOpen.Symbol.Ticker, ProductType.CFD);
                ulong quantity = (ulong)(bundleSize / barToOpen.AdjustedClose);
                if (quantity > 0)
                {
                    // Create order
                    _____________________________________________________________________________logger.Info($"We are about to place the order with quantity {quantity}");
                    IOrder newPositionOrder = tradingFactory.CreateMarketOrder(product, OrderAction.Buy, quantity);

                    // Place order
                    _____________________________________________________________________________logger.Info($"Placing order for {barToOpen.Symbol.Ticker} with quantity {quantity}");
                    apiClient.PlaceOrder(newPositionOrder, false);
                    openOrders.Add(newPositionOrder);

                    // Add to current state
                    Bundle bundle = new Bundle(barToOpen.Symbol.Ticker, Name, barToOpen.Date, assignedAccountSize, barToOpen.AdjustedClose, quantity);
                    openedTrades.Add(bundle);
                    strategyState.Bundles.Add(bundle);
                    _____________________________________________________________________________logger.Info($"The order for {barToOpen.Symbol.Ticker} was placed");
                }
                else
                    _____________________________________________________________________________logger.Info("We can't open the new position because share price is higher than bundle size");
            }
        }




        private StockBar FindPreviousBar(Table table, int nBarsBack, int row, int col)
        {
            for (int i = 0; i < nBarsBack; ++i)
            {
                row = row - 1;
                if (row == -1)
                    return null;

                while (table.Bars[row][col] == null)
                {
                    --row;

                    if (row == -1)
                        return null;
                }
            }

            return table.Bars[row][col];
        }
    }
}
