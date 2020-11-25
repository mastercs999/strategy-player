using System;
using Analyzer.Mocking.Api;
using Analyzer.Mocking.Api.Models;
using Analyzer.Mocking.Api.Models.Orders;
using Analyzer.Mocking.Data;
using Analyzer.Mocking.Time;
using Analyzer.TradingBase;
using CSharpAPISync.Models.Orders;
using CSharpAPISync.Support;
using Analyzer.Mocking.Notify;
using Common.Loggers;
using Analyzer.Mocking.Gateway;

namespace Analyzer.Mocking
{
    public class RealtimeTradingFactory : ITradingFactory
    {
        public ILogger Logger { get; private set; }
        public IDataProvider DataProvider { get; private set; }
        public IDateTimeProvider DateTimeProvider { get; private set; }
        public INotifyProvider NotifyProvider { get; private set; }
        public IGateway Gateway { get; private set; }

        private string AccountName;
        private int ClientId;
        private int Port;

        public RealtimeTradingFactory(string dataDirectory, string accountName, int clientId, int port, string phoneNumber, string email, string logDirectory)
        {
            Logger = new ThreadLogger(new FileLogger(logDirectory));
            DateTimeProvider = new RealtimeDateTimeProvider();
            DataProvider = new RealtimeDataProvider(dataDirectory, DateTimeProvider, Logger);
            NotifyProvider = new RealtimeNotifyProvider(phoneNumber, email, Logger);
            Gateway = new RealtimeGateway(Logger);
            AccountName = accountName;
            ClientId = clientId;
            Port = port;
        }

        public IApiClient CreateApi()
        {
            return new RealtimeApiClient(AccountName, ClientId, Port, Logger);
        }

        public IMarket CreateMarketOrder(IProduct product, OrderAction action, decimal quantity)
        {
            return new RealtimeMarket(new Market((product as RealtimeProduct).Product, action, quantity));
        }
    }
}
