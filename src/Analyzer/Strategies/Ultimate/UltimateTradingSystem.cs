using Analyzer.Data;
using Analyzer.Indicators;
using Analyzer.Mocking;
using Analyzer.Mocking.Api;
using Analyzer.Mocking.Api.Models;
using Analyzer.TradingBase;
using Common;
using Common.Extensions;
using Common.Loggers;
using CSharpAPISync.Support;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Analyzer.Strategies.Ultimate
{
    public class UltimateTradingSystem : TradingSystem
    {
        private UltimateConfig Config;

        public UltimateTradingSystem(UltimateConfig config) : base(config)
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
                foreach (IGrouping<string, Bundle> tickerToBundles in currentBundles.ToLookup(x => x.Ticker))
                {
                    // Find current bar
                    StockBar closingBar = stocks.Bars[i].SingleOrDefault(x => x != null && x.Symbol.Ticker == tickerToBundles.Key);
                    if (closingBar == null)
                        continue;

                    // Was PF hit?
                    decimal profit = tickerToBundles.Sum(x => x.Shares * (closingBar.AdjustedClose - x.OpenPrice));
                    if (closingBar.GetBarData<UltimateBarData>(Name).UltimateOscillator > 35 || profit / tickerToBundles.Sum(x => x.OpenAssetValue) > 0.01m)
                        for (int k = currentBundles.Count - 1; k >= 0; --k)
                        {
                            Bundle bundle = currentBundles[k];
                            if (bundle.Ticker != tickerToBundles.Key)
                                continue;

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
                HashSet<string> openedTickers = currentBundles.Select(x => x.Ticker).ToHashSet();

                // Open new position if possible
                int positionsOpened = 0;
                foreach (StockBar barToOpen in stocks.Bars[i].Where(x => x != null && x.GetBarData<UltimateBarData>(Name).UltimateOscillator < 30).OrderBy(x => x.GetBarData<UltimateBarData>(Name).UltimateOscillator))
                    if (currentBundles.Count < Config.Bundles && positionsOpened < 5)
                    {
                        // Can we open a position
                        bool open;
                        if (!openedTickers.Contains(barToOpen.Symbol.Ticker))
                            open = true;
                        else
                        {
                            decimal previousOpen = currentBundles.Where(x => x.Ticker == barToOpen.Symbol.Ticker).OrderByDescending(y => y.DateTimeOpened).First().OpenPrice;
                            decimal increase = (barToOpen.AdjustedClose - previousOpen) / previousOpen;

                            open = increase < -0.01m;
                        }

                        // Open new position
                        if (open)
                        {
                            ulong shares = (ulong)(bundleSize / barToOpen.AdjustedClose);
                            if (shares > 0)
                            {
                                Bundle trade = new Bundle(barToOpen.Symbol.Ticker, Name, barToOpen.Date, currentAccountValue, barToOpen.AdjustedClose, shares);

                                currentBundles.Add(trade);

                                ++positionsOpened;

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


        public override StrategyStateBase CreateState() => new StrategyStateBase();
        public override void CalculateIndicators(Table table, ILogger logger)
        {
            // Calculate for every stock
            for (int col = 0; col < table.Bars[0].Length; ++col)
            {
                // Create the indicators
                IIndicator<decimal> uo = new UltimateOscillator(Config.UltimateOscillatorPeriod1, Config.UltimateOscillatorPeriod2, Config.UltimateOscillatorPeriod3);

                for (int row = 0; row < table.Bars.Count; ++row)
                {
                    // Current bar
                    StockBar current = table.Bars[row][col];
                    if (current == null)
                        continue;

                    // Calculate the indicators
                    current.NameToBarData.Add(Name, new UltimateBarData()
                    {
                        UltimateOscillator = uo.Next(current.AdjustedOpen, current.AdjustedHigh, current.AdjustedLow, current.AdjustedClose)
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
                if (closingBar == null)
                    continue;

                // Was PF hit?
                decimal profit = tickerToBundles.Sum(x => x.Shares * (closingBar.AdjustedClose - x.OpenPrice));
                if (closingBar.GetBarData<UltimateBarData>(Name).UltimateOscillator > 35 || profit / tickerToBundles.Sum(x => x.OpenAssetValue) > 0.01m || !table.Symbols.Any(x => x.Ticker == tickerToBundles.Key))
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
            int positionsOpened = 0;
            foreach (StockBar barToOpen in latestBars.Where(x => x != null && x.GetBarData<UltimateBarData>(Name).UltimateOscillator < 30).OrderBy(x => x.GetBarData<UltimateBarData>(Name).UltimateOscillator))
                if (strategyState.Bundles.Count < Config.Bundles && positionsOpened < 5)
                {
                    // Can we open a position
                    bool open;
                    if (!openedTickers.Contains(barToOpen.Symbol.Ticker))
                        open = true;
                    else
                    {
                        decimal previousOpen = strategyState.Bundles.Where(x => x.Ticker == barToOpen.Symbol.Ticker).OrderByDescending(y => y.DateTimeOpened).First().OpenPrice;
                        decimal increase = (barToOpen.AdjustedClose - previousOpen) / previousOpen;

                        open = increase < -0.01m;
                        _____________________________________________________________________________logger.Info($"Increase for {barToOpen.Symbol.Ticker} is {increase}");
                    }

                    // Open new position
                    if (open)
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
                            ++positionsOpened;
                            _____________________________________________________________________________logger.Info($"The order for {barToOpen.Symbol.Ticker} was placed");
                        }
                        else
                            _____________________________________________________________________________logger.Info("We can't open the new position because share price is higher than bundle size");
                    }
                }
        }
    }
}
