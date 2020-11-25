using Analyzer;
using Analyzer.Data;
using Analyzer.Indicators;
using Common;
using Analyzer.Mocking;
using Analyzer.Mocking.Api;
using Analyzer.Mocking.Api.Models;
using Analyzer.Mocking.Data;
using Analyzer.Mocking.Time;
using Analyzer.TradingBase;
using CSharpAPISync;
using CSharpAPISync.Models;
using CSharpAPISync.Models.Orders;
using CSharpAPISync.Support;
using CsQuery;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common.Loggers;
using Common.Extensions;
using Analyzer.Mocking.Notify;
using Analyzer.Data.Sources;
using System.Diagnostics;

namespace Analyzer.Strategies.Ninety
{
    public class NinetyTradingSystem : TradingSystem
    {
        private NinetyConfig Config;

        public NinetyTradingSystem(NinetyConfig config) : base(config)
        {
            Config = config;
        }

        public override void Backtest()
        {
            // Load them
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
                    if (closingBar == null)
                        continue;

                    // Check the condition
                    if (closingBar.AdjustedClose > closingBar.GetBarData<NinetyBarData>(Name).ShorterSma)
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

                decimal leverage = Utilities.Gauss(drawdown < 10 ? 10 : drawdown, 1.5m, 10, 10) + 0.5m;

                // Determine bundle size
                int maxBundles = Config.Bundles;
                int availableBundles = maxBundles - currentBundles.Count;
                decimal bundleSize = Math.Max(0, availableBundles == 0 ? 0 : (currentAccountValue * leverage - assetValue - Config.ConfidenceMinimum) / availableBundles);

                // Current opened trades
                HashSet<string> openedTickers = currentBundles.Select(x => x.Ticker).ToHashSet();

                // Open new position if possible
                StockBar barToOpen = stocks.Bars[i].Where(x => x != null && x.GetBarData<NinetyBarData>(Name).IsValid && x.GetBarData<NinetyBarData>(Name).LongerSma < x.AdjustedClose && x.GetBarData<NinetyBarData>(Name).Rsi < 10 && !openedTickers.Contains(x.Symbol.Ticker)).OrderBy(x => x.GetBarData<NinetyBarData>(Name).Rsi).FirstOrDefault();
                if (barToOpen != null && currentBundles.Count + 1 <= maxBundles)
                {
                    ulong shares = (ulong)(bundleSize / barToOpen.AdjustedClose);
                    if (shares > 0)
                    {
                        currentBundles.Add(new Bundle(barToOpen.Symbol.Ticker, Name, barToOpen.Date, currentAccountValue, barToOpen.AdjustedClose, shares));

                        // Update cash
                        cash -= shares * barToOpen.AdjustedClose;
                    }
                }

                // Add to existings positions
                List<StockBar> barsToAdd = stocks.Bars[i].Where((x, k) =>
                    x != null &&
                    openedTickers.Contains(x.Symbol.Ticker) &&
                    currentBundles.Where(y => y.Ticker == x.Symbol.Ticker).OrderByDescending(y => y.DateTimeOpened).First().OpenPrice > x.AdjustedClose &&
                    currentBundles.Count(y => y.Ticker == x.Symbol.Ticker) < 10
                ).OrderBy(x => x.GetBarData<NinetyBarData>(Name).Rsi).ToList();
                foreach (StockBar toAdd in barsToAdd)
                {
                    List<Bundle> bundles = currentBundles.Where(y => y.Ticker == toAdd.Symbol.Ticker).ToList();

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

                    ulong shares = (ulong)(bundleSize * bundlesCountToBuy / toAdd.AdjustedClose);
                    if (shares == 0)
                        continue;

                    // Create bundles
                    ulong sharesLeft = shares;
                    ulong perBundle = shares / (ulong)bundlesCountToBuy;
                    for (int k = 0; k < bundlesCountToBuy; k++)
                    {
                        ulong bundleShares = k + 1 == bundlesCountToBuy ? sharesLeft : perBundle;
                        sharesLeft -= bundleShares;

                        currentBundles.Add(new Bundle(toAdd.Symbol.Ticker, Name, toAdd.Date, currentAccountValue, toAdd.AdjustedClose, bundleShares));
                    }

                    // Update cash
                    cash -= shares * toAdd.AdjustedClose;
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
            int minValuesKnown = Config.ShorterSmaPeriod;

            // Calculate for every stock
            for (int col = 0; col < table.Bars[0].Length; ++col)
            {
                // Create the indicators
                IIndicator<decimal> longerSma = new SimpleMovingAverage(Config.LongerSmaPeriod);
                IIndicator<decimal> shorterSma = new SimpleMovingAverage(Config.ShorterSmaPeriod);
                IIndicator<decimal> rsi = new WildersRsi(Config.RsiPeriod);

                // Counter of valid
                int foundBars = 0;

                for (int row = 0; row < table.Bars.Count; ++row)
                {
                    // Current bar
                    StockBar current = table.Bars[row][col];
                    if (current == null)
                        continue;

                    // Create bar data
                    current.NameToBarData.Add(Name, new NinetyBarData()
                    {
                        // Not enough data -> invalid
                        IsValid = ++foundBars >= minValuesKnown,

                        // Calculate the indicators
                        LongerSma = longerSma.Next(current.AdjustedClose),
                        ShorterSma = shorterSma.Next(current.AdjustedClose),
                        Rsi = rsi.Next(current.AdjustedClose)
                    });
                }
            }

            // Print info
            StockBar[] latestBars = table.Bars.Last();
            logger.Info("We have calculated these values:", String.Join("\n", latestBars.Select(x => $"{x?.Symbol.Ticker}\tClose: {x?.AdjustedClose}\tLonger SMA: {x?.GetBarData<NinetyBarData>(Name).LongerSma}\tShorter SMA: {x?.GetBarData<NinetyBarData>(Name).ShorterSma}\tRSI: {x?.GetBarData<NinetyBarData>(Name).Rsi}")));
        }
        public override decimal ExitPositions(State state, List<IPosition> positions, ITradingFactory tradingFactory, IApiClient apiClient, StockBar[] latestBars, Table table, IAccount account, out List<IOrder> closeOrders, out List<Bundle> closedTrades, ILogger _____________________________________________________________________________logger)
        {
            decimal exitSize = 0;
            closeOrders = new List<IOrder>();
            closedTrades = new List<Bundle>();

            // Get current state
            StrategyStateBase strategyState = state.GetStrategyState<StrategyStateBase>(Config.Name);

            foreach (string ticker in strategyState.Bundles.Select(x => x.Ticker).ToHashSet())
            {
                // Find proper bar
                StockBar closingBar = latestBars.SingleOrDefault(x => x != null && x.Symbol.Ticker == ticker);

                // Should we exit?
                if (closingBar != null && closingBar.AdjustedClose > closingBar.GetBarData<NinetyBarData>(Name).ShorterSma || !table.Symbols.Any(x => x.Ticker == ticker))
                {
                    // Find the position and proper bundles
                    IPosition position = positions.Single(x => x.Product.Symbol == ticker);
                    List<Bundle> bundles = strategyState.Bundles.Where(x => x.Ticker == ticker).ToList();

                    // How many shares we hold?
                    ulong shares = bundles.Sum(x => x.Shares);

                    // Create exit order
                    _____________________________________________________________________________logger.Info($"Creating order for exiting the position on {ticker} with size {shares}");
                    IOrder order = tradingFactory.CreateMarketOrder(position.Product, OrderAction.Sell, shares);

                    // Place the order
                    _____________________________________________________________________________logger.Info($"Placing order for exiting the position on {ticker} with size {shares}");
                    apiClient.PlaceOrder(order, false);
                    closeOrders.Add(order);

                    // Close bundles
                    foreach (Bundle bundle in bundles)
                        bundle.Exit(closingBar.Date, closingBar.AdjustedClose);

                    // Add closed bundles to the output
                    closedTrades.AddRange(bundles);

                    // Update current state
                    strategyState.Bundles.RemoveAll(x => x.Ticker == ticker);

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
            decimal leverage = Utilities.Gauss(strategyState.CurrentDrawdown < 10 ? 10 : strategyState.CurrentDrawdown, 1.5m, 10, 10) + 0.5m;
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
            StockBar barToOpen = latestBars.Where(x => x != null && x.GetBarData<NinetyBarData>(Name).IsValid && x.GetBarData<NinetyBarData>(Name).LongerSma < x.AdjustedClose && x.GetBarData<NinetyBarData>(Name).Rsi < 10 && !openedTickers.Contains(x.Symbol.Ticker)).OrderBy(x => x.GetBarData<NinetyBarData>(Name).Rsi).FirstOrDefault();
            if (barToOpen != null && strategyState.Bundles.Count + 1 <= maxBundles)
            {
                // Determine product and quantity
                _____________________________________________________________________________logger.Info($"We can open a new position for {barToOpen.Symbol.Ticker}", $"Getting product for {barToOpen.Symbol.Ticker}");
                IProduct product = apiClient.FindProduct(barToOpen.Symbol.Ticker, ProductType.Stock);
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

            // Add to existing positions
            _____________________________________________________________________________logger.Info("Checking whether we can add to some positions. It means buy more stocks.");
            List<StockBar> barsToAdd = latestBars.Where((x, k) =>
                x != null &&
                openedTickers.Contains(x.Symbol.Ticker) &&
                strategyState.Bundles.Where(y => y.Ticker == x.Symbol.Ticker).OrderByDescending(y => y.DateTimeOpened).First().OpenPrice > x.AdjustedClose &&
                strategyState.Bundles.Count(y => y.Ticker == x.Symbol.Ticker) < 10
            ).OrderBy(x => x.GetBarData<NinetyBarData>(Name).Rsi).ToList();
            _____________________________________________________________________________logger.Info($"We will add to these symbols: {String.Join(", ", barsToAdd.Select(x => x.Symbol.Ticker))}");
            foreach (StockBar toAdd in barsToAdd)
            {
                List<Bundle> bundles = strategyState.Bundles.Where(y => y.Ticker == toAdd.Symbol.Ticker).ToList();

                int bundlesCountToBuy = 0;
                switch (bundles.Count)
                {
                    case 1: bundlesCountToBuy = 2; break;
                    case 3: bundlesCountToBuy = 3; break;
                    case 6: bundlesCountToBuy = 4; break;
                    default: throw new InvalidOperationException($"Wrong number of bundles opened ({bundles.Count}) for '{toAdd.Symbol.Ticker}'. This should never happen");
                }

                _____________________________________________________________________________logger.Info($"Product {toAdd.Symbol.Ticker} has {bundles.Count} bundles, in total we have {strategyState.Bundles.Count} bundles and we should add {bundlesCountToBuy} bundles");
                if (strategyState.Bundles.Count + bundlesCountToBuy > maxBundles)
                    continue;

                // Determine product and quantity
                _____________________________________________________________________________logger.Info($"Searching for product for {toAdd.Symbol.Ticker}");
                IProduct product = apiClient.FindProduct(toAdd.Symbol.Ticker, ProductType.Stock);
                ulong shares = (ulong)(bundleSize * bundlesCountToBuy / toAdd.AdjustedClose);
                if (shares == 0)
                    continue;

                // Create the order
                _____________________________________________________________________________logger.Info($"Creating order for {toAdd.Symbol.Ticker} with qunatity {shares} in total.");
                IOrder newPositionOrder = tradingFactory.CreateMarketOrder(product, OrderAction.Buy, shares);

                // Place order
                _____________________________________________________________________________logger.Info($"Placing order for {toAdd.Symbol.Ticker} with qunatity {shares} in total.");
                apiClient.PlaceOrder(newPositionOrder, false);
                openOrders.Add(newPositionOrder);

                // Update in current state
                ulong sharesLeft = shares;
                ulong perBundle = shares / (ulong)bundlesCountToBuy;
                for (int k = 0; k < bundlesCountToBuy; k++)
                {
                    ulong bundleShares = k + 1 == bundlesCountToBuy ? sharesLeft : perBundle;
                    sharesLeft -= bundleShares;

                    Bundle bundle = new Bundle(toAdd.Symbol.Ticker, Name, toAdd.Date, assignedAccountSize, toAdd.AdjustedClose, bundleShares);
                    openedTrades.Add(bundle);
                    strategyState.Bundles.Add(bundle);
                }
                _____________________________________________________________________________logger.Info($"The order was placed");
            }
        }
    }
}
