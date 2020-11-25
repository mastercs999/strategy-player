using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Analyzer.Mocking.Api;
using Analyzer.Mocking.Api.Models;
using Analyzer.Mocking.Api.Models.Orders;
using CSharpAPISync.Support;
using Analyzer.Mocking.Time;
using Analyzer.TradingBase;
using Analyzer.Mocking.Data;
using Analyzer.Mocking.Notify;
using Common.Loggers;
using Analyzer.Mocking.Gateway;

namespace Analyzer.Mocking
{
    public class BacktestTradingFactory : ITradingFactory
    {
        public ILogger Logger { get; private set; }
        public IDataProvider DataProvider { get; private set; }
        public IDateTimeProvider DateTimeProvider { get; private set; }
        public INotifyProvider NotifyProvider { get; private set; }
        public IGateway Gateway { get; private set; }

        private Random Random;
        private string DataDirectory;
        private BacktestAccount Account;




        public BacktestTradingFactory(Random random, string dataDirectory, decimal capital, DateTimeOffset backtestStart)
        {
            Logger = new SilentLogger();
            DateTimeProvider = new BacktestDateTimeProvider(backtestStart);
            DataProvider = new BacktestDataProvider(dataDirectory, DateTimeProvider);
            NotifyProvider = new BacktestNotifyProvider(Logger);
            Gateway = new BacktestGateway();
            Random = random;
            DataDirectory = dataDirectory;
            Account = new BacktestAccount(capital);
        }




        public IApiClient CreateApi()
        {
            return new BacktestApiClient(Random, DataDirectory, DateTimeProvider, Account);
        }

        public IMarket CreateMarketOrder(IProduct product, OrderAction action, decimal quantity)
        {
            return new BacktestMarket(product, action, quantity, OrderStatus.NotPlaced);
        }
    }
}
