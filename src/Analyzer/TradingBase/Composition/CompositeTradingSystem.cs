using Analyzer.Data;
using Analyzer.Exceptions;
using Analyzer.Mocking;
using Analyzer.Mocking.Api;
using Analyzer.Mocking.Api.Models;
using Analyzer.Mocking.Data;
using Analyzer.Mocking.Notify;
using Analyzer.Mocking.Time;
using Common;
using Common.Extensions;
using Common.Loggers;
using CSharpAPISync.Support;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Analyzer.TradingBase.Composition
{
    public class CompositeTradingSystem
    {
        private readonly CompositeConfig Config;
        private readonly StrategySettings[] Strategies;
        private readonly Dictionary<string, decimal> NameToRatio;




        public CompositeTradingSystem(CompositeConfig config, TradingSystem strategy) : this(config, new StrategySettings(strategy, 1))
        {

        }
        public CompositeTradingSystem(CompositeConfig config, params StrategySettings[] strategies)
        {
            Config = config;
            Strategies = strategies;
            NameToRatio = strategies.ToDictionary(x => x.Strategy.Name, x => x.Ratio);
        }




        public void Run()
        {
            // Create basic structure
            //ITradingFactory tradingFactory = new RealtimeTradingFactory(Config.DataDirectory, Config.AccountName, Config.ClientId, Config.Port, Config.PhoneNumber, Config.Email, Config.LogDirectory);
            ITradingFactory tradingFactory = new BacktestTradingFactory(Config.Random, Config.DataDirectory, Config.Capital, DateTimeOffset.UtcNow.AddYears(-100));

            // Get providers
            IDateTimeProvider dateTimeProvider = tradingFactory.DateTimeProvider;
            IDataProvider dataProvider = tradingFactory.DataProvider;
            INotifyProvider notifyProvider = tradingFactory.NotifyProvider;
            ILogger _____________________________________________________________________________logger = tradingFactory.Logger;

            // For handling 
            try
            {
                // Make sure the directory for trades exists
                Directory.CreateDirectory(Config.TradesDirectory);

                // Create API
                _____________________________________________________________________________logger.Info("Creating API object");
                IApiClient apiClient = tradingFactory.CreateApi();
                bool connected = false;
                _____________________________________________________________________________logger.Info("API object was created");

                while (dataProvider.HasData)
                {
                    try
                    {
                        // Start gateway
                        _____________________________________________________________________________logger.Info("Starting gateway...");
                        tradingFactory.Gateway.Start(Config.GatewayVersion, Config.GatewayLogin, Config.GatewayPassword, Config.LiveTrading);
                        _____________________________________________________________________________logger.Info("Gatway was started");

                        // Connect
                        _____________________________________________________________________________logger.Info("Connecting to IB...");
                        apiClient.ConnectSafe();
                        connected = true;
                        _____________________________________________________________________________logger.Info("Connection to IB was established");

                        // Load current state
                        _____________________________________________________________________________logger.Info("Loading current state or creating a new one...");
                        State currentState = File.Exists(Config.StateFile) ? Serializer.DeserializeJson<State>(Config.StateFile) : new State(Strategies.ToDictionary(x => x.Strategy.Name, x => x.Strategy.CreateState()));
                        _____________________________________________________________________________logger.Info("Current state loaded");

                        // Check if we are in sync
                        _____________________________________________________________________________logger.Info("Checking state sync on the beginning...");
                        CheckStateSync(currentState, apiClient.GetAllPositions(), _____________________________________________________________________________logger);
                        _____________________________________________________________________________logger.Info("State is in sync");

                        // First get symbols
                        _____________________________________________________________________________logger.Info("Getting list of symbols...");
                        List<Symbol> symbols = dataProvider.GetSymbols();
                        _____________________________________________________________________________logger.Info("We have following symbols:", String.Join("\n", symbols.Select(x => $"{x.Ticker}\t=>\t{x.Volume}")));

                        // Find next ending session
                        _____________________________________________________________________________logger.Info($"Now is {dateTimeProvider.Now}. Searching for next ending session...");
                        DateTimeOffset nextEnding = NextEndingSession(symbols, apiClient, dateTimeProvider.Now, _____________________________________________________________________________logger);
                        _____________________________________________________________________________logger.Info($"Next ending session is in {nextEnding} and now is {dateTimeProvider.Now}");

                        // We're going to wait, so disconnection is obvious choice
                        _____________________________________________________________________________logger.Info("Disconnecting from IB till trading session...");
                        apiClient.Disconnect();
                        _____________________________________________________________________________logger.Info("IB was disconnected");

                        // And also we want to stop gateway
                        _____________________________________________________________________________logger.Info("Stopping gateway till trading session...");
                        tradingFactory.Gateway.Stop();
                        _____________________________________________________________________________logger.Info("Gateway was stopped");

                        // Sleep until we should be in the zone where we can download the main data
                        _____________________________________________________________________________logger.Info($"Let's sleep until next ending session: {nextEnding.AddMinutes(-Config.StartBeforeEnd)}");
                        notifyProvider.SendSms($"Next trading session is at {nextEnding.ToLocalTime()}. We're going to sleep till {nextEnding.ToLocalTime().AddMinutes(-Config.StartBeforeEnd)}.");
                        dateTimeProvider.SleepUntil(nextEnding.AddMinutes(-Config.StartBeforeEnd));
                        _____________________________________________________________________________logger.Info($"We woke up at {dateTimeProvider.Now}");

                        // Get symbols again
                        _____________________________________________________________________________logger.Info("Getting symbols during trading preparation...");
                        symbols = dataProvider.GetSymbols();
                        _____________________________________________________________________________logger.Info("Symbols seem to be OK. We got this:", String.Join("\n", symbols.Select(x => $"{x.Ticker}\t=>\t{x.Volume}")));

                        // Try to download online data if it works
                        _____________________________________________________________________________logger.Info("Trying to get online data. Just checking if it works...");
                        dataProvider.DownloadOnlineData(symbols.ToArray());
                        _____________________________________________________________________________logger.Info("We have downloaded all online data");

                        // Start gateway
                        _____________________________________________________________________________logger.Info("Starting gateway before trading...");
                        tradingFactory.Gateway.Start(Config.GatewayVersion, Config.GatewayLogin, Config.GatewayPassword, Config.LiveTrading);
                        _____________________________________________________________________________logger.Info("Gateway was started");

                        // Connect
                        _____________________________________________________________________________logger.Info("Connecting to IB before trading...");
                        apiClient.ConnectSafe();
                        _____________________________________________________________________________logger.Info("Connection to IB before trading was established");

                        // Get account summary - just to be sure
                        _____________________________________________________________________________logger.Info("Getting account summary...just in case.");
                        IAccount account = apiClient.GetAccountSummary();
                        _____________________________________________________________________________logger.Info("We downloaded account summary for the sake of assurance. There is its state:", account.Dump());

                        // Now we can download the main data
                        _____________________________________________________________________________logger.Info("Getting complete history...");
                        Table table = dataProvider.GetHistory();
                        _____________________________________________________________________________logger.Info($"Getting history finished. We have {table.Bars.Count} bars and last bar is from {(table.Bars.Count > 0 ? table.Bars.Last().First(x => x != null).Date.ToString() : "UNKNOWN")}");

                        // Get opened positions now
                        _____________________________________________________________________________logger.Info("Getting all opened positions...");
                        List<IPosition> positions = apiClient.GetAllPositions();
                        _____________________________________________________________________________logger.Info("We currently hold following positions:", String.Join("\n", positions.Select(x => $"{x.Product.Symbol}\t=>\t{x.Size}")));

                        // We must be in sync
                        _____________________________________________________________________________logger.Info("Checking state sync...");
                        CheckStateSync(currentState, positions, _____________________________________________________________________________logger);
                        _____________________________________________________________________________logger.Info("State is in sync");

                        // Wait until the main trading time
                        _____________________________________________________________________________logger.Info($"Let's sleep until we want to trade. It means until: {nextEnding.AddMinutes(-Config.MainAttemptBeforeEnd)}. Now is {dateTimeProvider.Now}");
                        notifyProvider.SendSms("Preparation for trading finished. Let's sleep till main trading session.");
                        dateTimeProvider.SleepUntil(nextEnding.AddMinutes(-Config.MainAttemptBeforeEnd));
                        _____________________________________________________________________________logger.Info($"We woke up at {dateTimeProvider.Now} and we want to trade!");

                        // Get account information
                        _____________________________________________________________________________logger.Info("Getting account summary...");
                        account = apiClient.GetAccountSummary();
                        _____________________________________________________________________________logger.Info("We downloaded account summary. There is its state:", account.Dump());

                        // Append the online data
                        _____________________________________________________________________________logger.Info($"Getting online data...");
                        StockBar[] latestBars = dataProvider.AppendOnlineData();
                        _____________________________________________________________________________logger.Info($"Online data were successfully downloaded. We got this:", String.Join("\n", latestBars.Select(x => $"{x?.Symbol.Ticker}\t{x?.Date}\t{x?.AdjustedClose}")));

                        // Calculate indicators
                        _____________________________________________________________________________logger.Info("Calculating indicators...");
                        dataProvider.CalculateIndicators(Strategies.Select(x => (Action<Table>)(y => x.Strategy.CalculateIndicators(y, _____________________________________________________________________________logger))));
                        _____________________________________________________________________________logger.Info("Indicators calculation finished");

                        // Exit position that accomplish a condition
                        _____________________________________________________________________________logger.Info("We are going to check all positions and close the ones that accomplish the condition.");
                        decimal grossPositionValueBeforeExit = account.GrossPositionValue;
                        decimal exitSize = 0;
                        List<IOrder> closeOrders = new List<IOrder>();
                        List<Bundle> closedTrades = new List<Bundle>();
                        foreach (TradingSystem tradingSystem in Strategies.Select(x => x.Strategy))
                        {
                            _____________________________________________________________________________logger.Info($"Strategy {tradingSystem.Name} may exit positions now.");
                            exitSize += tradingSystem.ExitPositions(currentState, positions, tradingFactory, apiClient, latestBars, table, account, out List<IOrder> tradingSystemCloseOrders, out List<Bundle> tradingSystemClosedTrades, _____________________________________________________________________________logger);
                            _____________________________________________________________________________logger.Info($"Strategy {tradingSystem.Name} finished exiting positions.");

                            closeOrders.AddRange(tradingSystemCloseOrders);
                            closedTrades.AddRange(tradingSystemClosedTrades);
                        }

                        // Wait till exit orders finishes
                        foreach (IOrder order in closeOrders)
                        {
                            _____________________________________________________________________________logger.Info($"Waiting till order for {order.Product.Symbol} finishes...");
                            order.WaitTillFinishes();
                            _____________________________________________________________________________logger.Info($"Order for {order.Product.Symbol} finished with status {order.Status}");
                        }

                        // Wait till account information is updated
                        account = apiClient.GetAccountSummary();
                        _____________________________________________________________________________logger.Info($"Exit site is {exitSize}.");
                        if (exitSize != 0)
                        {
                            _____________________________________________________________________________logger.Info($"Waiting till account information is updated after exiting positions. Exit size is ${exitSize} and before exit we held products for ${grossPositionValueBeforeExit}");
                            account = apiClient.GetAccountSummary();
                            while (Math.Abs(grossPositionValueBeforeExit - account.GrossPositionValue - exitSize) > exitSize / 20)
                            {
                                dateTimeProvider.SleepFor(1000);
                                account = apiClient.GetAccountSummary();
                            }
                        }
                        _____________________________________________________________________________logger.Info("We have actual account information data");

                        // Update positions
                        _____________________________________________________________________________logger.Info("Getting all positions again, after closing positions...");
                        positions = apiClient.GetAllPositions();
                        _____________________________________________________________________________logger.Info("After closing positions we hold following positions:", String.Join("\n", positions.Select(x => $"{x.Product.Symbol} => {x.Size}")));

                        // Calculate current drawdown
                        _____________________________________________________________________________logger.Info("Calculating current drawdown...");
                        CalculateDrawdowns(account.EquityWithLoanValue, ref currentState, _____________________________________________________________________________logger);
                        _____________________________________________________________________________logger.Info("Drawdowns were calculated");

                        // Open trades in every strategy
                        List<IOrder> openOrders = new List<IOrder>();
                        List<Bundle> openedTrades = new List<Bundle>();
                        foreach (TradingSystem tradingSystem in Strategies.Select(x => x.Strategy))
                        {
                            // Determine account size
                            decimal assignedAccountSize = NameToRatio[tradingSystem.Name] * account.EquityWithLoanValue;
                            decimal positionsValue = CalculateStrategyPositiosSize(table, currentState.GetStrategyState<StrategyStateBase>(tradingSystem.Name).Bundles);
                            _____________________________________________________________________________logger.Info($"Strategy {tradingSystem.Name} was assigned account size of {assignedAccountSize}/{account.EquityWithLoanValue} and hold positions with value of {positionsValue}");

                            _____________________________________________________________________________logger.Info($"Strategy {tradingSystem.Name} may open positions now.");
                            tradingSystem.OpenPositions(currentState, positions, tradingFactory, apiClient, latestBars, table, account, assignedAccountSize, positionsValue, out List<IOrder> tradingSystemOpenOrders, out List<Bundle> tradingSystemOpenedTrades, _____________________________________________________________________________logger);
                            _____________________________________________________________________________logger.Info($"Strategy {tradingSystem.Name} has ended opening positions.");

                            openOrders.AddRange(tradingSystemOpenOrders);
                            openedTrades.AddRange(tradingSystemOpenedTrades);
                        }

                        // Wait till all orders finishes
                        foreach (IOrder order in openOrders)
                        {
                            _____________________________________________________________________________logger.Info($"Waiting till order for {order.Product.Symbol} finishes...");
                            order.WaitTillFinishes();
                            _____________________________________________________________________________logger.Info($"Order for {order.Product.Symbol} finished with status {order.Status}");
                        }

                        // Wait till we have all execution details
                        foreach (IOrder order in closeOrders.Concat(openOrders))
                        {
                            _____________________________________________________________________________logger.Info($"Waiting till order for {order.Product.Symbol} got all execution details...");
                            order.WaitForExecutionDetails();
                            _____________________________________________________________________________logger.Info($"Order for {order.Product.Symbol} should have all execution details");
                        }

                        // Update information from orders
                        UpdateExecutionDetails(closeOrders, ref closedTrades, BundleAction.Close, _____________________________________________________________________________logger);
                        UpdateExecutionDetails(openOrders, ref openedTrades, BundleAction.Open, _____________________________________________________________________________logger);

                        // Save historical trades
                        _____________________________________________________________________________logger.Info($"Saving closed trades into {Config.TradeHistoryFile}");
                        closedTrades.AppendToJsonFile(Config.TradeHistoryFile);
                        _____________________________________________________________________________logger.Info($"Closed trades were saved");

                        // Save current state
                        _____________________________________________________________________________logger.Info($"Writing current state into file {Config.StateFile}");
                        currentState.SerializeJson(Config.StateFile);
                        _____________________________________________________________________________logger.Info($"Current state was saved");

                        // Check order's state
                        _____________________________________________________________________________logger.Info("Checking order statuses after trading has finished");
                        List<IOrder> notFilledOrders = closeOrders.Concat(openOrders).Where(x => x.Status != OrderStatus.Filled).ToList();
                        if (notFilledOrders.Any())
                        {
                            _____________________________________________________________________________logger.Info(new string[]
                            {
                                "Following orders were not filled:",
                            }.Concat(notFilledOrders.Select(x => $"{x.Product.Symbol}\t{x.Status.Text()}\t{x.Action.Text()}\t{x.Quantity}")).ToArray());
                            throw new TradingException("Some orders were not filled: " + String.Join(",", notFilledOrders.Select(x => x.Product.Symbol)));
                        }
                        else
                            _____________________________________________________________________________logger.Info("All orders were filled");

                        // We're going to wait, so disconnection is obvious choice
                        _____________________________________________________________________________logger.Info("Disconnecting from IB after trading session...");
                        apiClient.Disconnect();
                        _____________________________________________________________________________logger.Info("IB was disconnected");

                        // We can end gateway now
                        _____________________________________________________________________________logger.Info("Stopping gateway after trading session...");
                        tradingFactory.Gateway.Stop();
                        _____________________________________________________________________________logger.Info("Gateway was stopped");

                        // Sleep till ending session pass
                        _____________________________________________________________________________logger.Info($"Now we are going to sleep until {nextEnding.AddMinutes(Config.MainAttemptBeforeEnd * 5)}");
                        notifyProvider.SendSms($"Trading finished at {dateTimeProvider.Now.ToLocalTime()}. We sold {String.Join(",", closeOrders.Concat(openOrders).Where(x => x.Action == OrderAction.Sell).Select(x => x.Product.Symbol))} and bought {String.Join(",", closeOrders.Concat(openOrders).Where(x => x.Action == OrderAction.Buy).Select(x => x.Product.Symbol))}.");
                        dateTimeProvider.SleepUntil(nextEnding.AddMinutes(Config.MainAttemptBeforeEnd * 5));
                        _____________________________________________________________________________logger.Info($"We woke up at {dateTimeProvider.Now}");
                    }
                    catch (Exception ex) when (ex is ConnectionException ||
                        ex is ThreadInterruptedException && ThreadMessage.ThrownException is IBException ibException &&
                            ((ibException.Id == -1 && ibException.ErrorCode == 1100 && ibException.Message == "Connectivity between IB and Trader Workstation has been lost.") ||
                            (ibException.Id == -1 && ibException.ErrorCode == 2110 && ibException.Message == "Connectivity between Trader Workstation and server is broken. It will be restored automatically.")))
                    {
                        // We should have cought an exception about connection
                        _____________________________________________________________________________logger.Info("We cought and exception which comes from a different thread. Caught exception and ThreadMessage.ThrownException follows");
                        _____________________________________________________________________________logger.Error(ex);
                        _____________________________________________________________________________logger.Error(ThreadMessage.ThrownException);

                        // Switch to ib exception if caught
                        if (ex is ThreadInterruptedException)
                            ex = ThreadMessage.ThrownException;

                        // Send SMS if we had connection, but lost it now
                        _____________________________________________________________________________logger.Info("Connection to IB has been lost: " + ex.Message);
                        if (connected)
                        {
                            connected = false;

                            _____________________________________________________________________________logger.Info("Let's notify about IB outage...");
                            notifyProvider.SendMail("Trader lost connection to IB", ex.Message);
                            notifyProvider.SendSms("Connection to IB was lost");
                            _____________________________________________________________________________logger.Info("We have informed about IB outage");

                            // We will try to stop gateway and client
                            _____________________________________________________________________________logger.Info("Because we have received ib outage, let's try to disconnect and stop gateway.");
                            apiClient.Disconnect();
                            _____________________________________________________________________________logger.Info("Disconnection ended. Now stop the gateway.");
                            tradingFactory.Gateway.Stop();
                            _____________________________________________________________________________logger.Info("Gateway was stopped.");
                        }

                        // Wait and repeat the loop
                        int reconnectDelay = 30 * 1000;
                        _____________________________________________________________________________logger.Info($"Sleeping for {reconnectDelay / 1000} seconds and then we'll try to connect...");
                        dateTimeProvider.SleepFor(reconnectDelay);
                    }
                }

                // Sell all
                foreach (IPosition position in apiClient.GetAllPositions())
                {
                    IOrder order = tradingFactory.CreateMarketOrder(position.Product, OrderAction.Sell, position.Size);

                    apiClient.PlaceOrder(order);

                    // Close bundles
                    State currentState = Serializer.DeserializeJson<State>(Config.StateFile);
                    List <Bundle> bundlesToClose = currentState.AllBundles.Where(x => x.Ticker == position.Product.Symbol).ToList();
                    foreach (Bundle bundle in bundlesToClose)
                        bundle.Exit(dateTimeProvider.Now.UtcDateTime.Date, order.AverageFillPrice);
                    bundlesToClose.AppendToJsonFile(Config.TradeHistoryFile);
                }

                // Write account info
                IAccount acc = apiClient.GetAccountSummary();
                Console.WriteLine(acc.EquityWithLoanValue);
                Console.WriteLine(acc.GrossPositionValue);
            }
            catch (Exception ex)
            {
                // Extract last message from log
                string lastLogMessage = _____________________________________________________________________________logger.LastMessage;

                // Log both exceptions
                _____________________________________________________________________________logger.Info("We cought and exception which comes from a different thread. Caught exception and ThreadMessage.ThrownException follows");
                _____________________________________________________________________________logger.Error(ex);
                _____________________________________________________________________________logger.Error(ThreadMessage.ThrownException);

                // Switch to saved exception
                if (ex is ThreadInterruptedException)
                    ex = ThreadMessage.ThrownException;

                // Notify user
                notifyProvider.SendMail("Trading error", "Last log message: " + lastLogMessage + "\n\n" + String.Join("\n\n", ex.Message, ex.StackTrace, ex));
                notifyProvider.SendSms("Trading error: " + ex.Message + "\n Log: " + lastLogMessage);

                ex.Rethrow();
            }

            // Analyze trades
            List<Bundle> allTrades = Serializer.DeserializeJson<List<Bundle>>(Config.TradeHistoryFile);
            PerformanceReport report = new PerformanceReport(allTrades, Config.Capital);
            report.PrintStats();
            report.ExportToExcel(Config.ResultFile);
        }




        private DateTimeOffset NextEndingSession(List<Symbol> symbols, IApiClient client, DateTimeOffset now, ILogger _____________________________________________________________________________logger)
        {
            // Getting symbols with the highest volume
            symbols = symbols.OrderByDescending(x => x.Volume).Take(10).ToList();
            _____________________________________________________________________________logger.Info("Next ending session will be infered from these symbols: " + String.Join(", ", symbols.Select(x => x.Ticker)));

            // Find next trading dates
            List<DateTimeOffset> endings = new List<DateTimeOffset>();
            foreach (Symbol s in symbols)
            {
                // Find product
                IProduct product = client.FindProduct(s.Ticker, ProductType.CFD);

                // Get next ending
                DateRange nextSession = product.TradingHours.OrderBy(x => x.To).First(x => x.To > now);
                if ((nextSession.To - nextSession.From).TotalHours >= 10)
                    throw new TradingException($"{s.Ticker} has suspiciously long trading hours: {nextSession.From} - {nextSession.To}");

                endings.Add(nextSession.To);

                // Log
                _____________________________________________________________________________logger.Info($"{product.Symbol} has ending session in {nextSession.To}. This was taken from: {String.Join(", ", product.TradingHours.Select(x => x.From + " - " + x.To))}");
            }

            // Check whether all dates are the same
            if (endings.Distinct().Count() > 1)
                throw new TradingException(String.Format("Cannot determine next ending session. These products were used: {0} with these ending sessions found: {1}", String.Join(", ", symbols.Select(x => x.Ticker)), String.Join(", ", endings)));

            return endings.First();
        }
        private void CheckStateSync(State state, List<IPosition> positions, ILogger logger)
        {
            HashSet<string> stateTickers = state.AllBundles.Select(x => x.Ticker).ToHashSet();
            HashSet<string> apiTickers = positions.Select(x => x.Product.Symbol).ToHashSet();

            // We should be in positions with the same stocks
            if (!stateTickers.SetEquals(apiTickers))
                throw new TradingException(String.Format("State and api is out of sync. State contains following tickers: {0}. While api contains these: {1}", String.Join(", ", stateTickers), String.Join(", ", apiTickers)));

            foreach (string ticker in stateTickers)
            {
                // Find proper position
                List<Bundle> bundles = state.AllBundles.Where(x => x.Ticker == ticker).ToList();
                IPosition pos = positions.Single(x => x.Product.Symbol == ticker);

                // Check if number of shares match
                ulong stateShares = bundles.Sum(x => x.Shares);
                if (stateShares != pos.Size)
                    throw new TradingException($"There is mismatch in number of shares for {ticker}. State has {stateShares} shares while api has {pos.Size}");
            }

            // Log
            logger.Info("We have state in sync. We hold following positions:", String.Join("\n", state.AllBundles.GroupBy(x => x.Ticker).Select(x => $"{x.Key}\t=>\t{x.Count()} bundles\t=>\t{x.Sum(y => y.Shares)} shares")));
        }
        private void CalculateDrawdowns(decimal accountValue, ref State state, ILogger _____________________________________________________________________________logger)
        {
            // Calculate drawdown for every strategy
            foreach (KeyValuePair<string, StrategyStateBase> kvp in state.NameToState)
            {
                StrategyStateBase strategyState = kvp.Value;

                // What part of account value the strategy manages?
                decimal strategyAccountSize = NameToRatio[kvp.Key] * accountValue;

                // Calculate max account value
                if (strategyAccountSize > strategyState.MaxAccountValue)
                    strategyState.MaxAccountValue = strategyAccountSize;

                // Calculate current drawdown and max drawdown
                strategyState.CurrentDrawdown = (strategyState.MaxAccountValue - strategyAccountSize) / strategyState.MaxAccountValue * 100;
                if (strategyState.CurrentDrawdown > strategyState.MaxDrawdown)
                    strategyState.MaxDrawdown = strategyState.CurrentDrawdown;

                _____________________________________________________________________________logger.Info($"Current drawdown for {kvp.Key} is {strategyState.CurrentDrawdown}. Max drawdown is {strategyState.MaxDrawdown}, max account value is {strategyState.MaxAccountValue} and current account value is {accountValue}");
            }
        }
        private decimal CalculateStrategyPositiosSize(Table table, List<Bundle> bundles)
        {
            Dictionary<string, decimal> tickerToPrice = bundles.Select(x => x.Ticker).ToHashSet().ToDictionary(x => x, x => table.FindLastBar(x).AdjustedClose);

            return bundles.Sum(x => x.Shares * tickerToPrice[x.Ticker]);
        }
        private void UpdateExecutionDetails(List<IOrder> orders, ref List<Bundle> bundles, BundleAction bundleAction, ILogger _____________________________________________________________________________logger)
        {
            _____________________________________________________________________________logger.Info($"We're going to update execution details of {bundleAction.Text()} for order of symbols: {String.Join(", ", orders.Select(x => x.Product.Symbol))} and bundles: {String.Join(", ", bundles.Select(x => x.Ticker))}");

            // We have to fill execution details from these orders
            List<IOrder> ordersToInspect = orders.ToList();

            // Do every ticker per strategy separately
            foreach (string strategyName in Strategies.Select(x => x.Strategy.Name))
                foreach (IGrouping<string, Bundle> tickerToBundles in bundles.Where(x => x.StrategyName == strategyName).ToLookup(x => x.Ticker))
                {
                    // Find matching order
                    IOrder order = ordersToInspect.First(x => x.Product.Symbol == tickerToBundles.Key && x.Quantity == tickerToBundles.Sum(y => y.Shares));
                    ordersToInspect.Remove(order);

                    // Update execution details
                    if (bundleAction == BundleAction.Open)
                    {
                        _____________________________________________________________________________logger.Info($"Bundles for strategy {strategyName} with ticker {tickerToBundles.Key} had open price {tickerToBundles.First().OpenPrice}, but now has {order.AverageFillPrice}");
                        _____________________________________________________________________________logger.Info($"Bundles for strategy {strategyName} with ticker {tickerToBundles.Key} had open commission {tickerToBundles.First().OpenCommission}, but now has {order.Commission}");
                        foreach (Bundle bundle in tickerToBundles)
                        {
                            bundle.OpenPrice = order.AverageFillPrice;
                            bundle.OpenCommission = order.Commission / tickerToBundles.Count();
                        }
                    }
                    else
                    {
                        _____________________________________________________________________________logger.Info($"Bundles for strategy {strategyName} with ticker {tickerToBundles.Key} had close price {tickerToBundles.First().ClosePrice}, but now has {order.AverageFillPrice}");
                        _____________________________________________________________________________logger.Info($"Bundles for strategy {strategyName} with ticker {tickerToBundles.Key} had close commission {tickerToBundles.First().CloseCommission}, but now has {order.Commission}");
                        foreach (Bundle bundle in tickerToBundles)
                        {
                            bundle.ClosePrice = order.AverageFillPrice;
                            bundle.CloseCommission = order.Commission / tickerToBundles.Count();
                        }
                    }
                }
        }
    }
}
