using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Analyzer.Mocking.Api.Models;
using CSharpAPISync;
using CSharpAPISync.Support;
using Common.Loggers;

namespace Analyzer.Mocking.Api
{
    public class RealtimeApiClient : IApiClient
    {
        private ApiClient ApiClient;

        public RealtimeApiClient(string accountName, int clientId, int port, ILogger logger)
        {
            ApiClient = new ApiClient(accountName, clientId, port, logger);
        }




        public void Connect()
        {
            ApiClient.Connect();
        }
        public void ConnectSafe()
        {
            ApiClient.ConnectSafe();
        }
        public void Disconnect()
        {
            ApiClient.Disconnect();
        }

        public IProduct FindProduct(string symbol, ProductType productType)
        {
            return new RealtimeProduct(ApiClient.FindProduct(symbol, productType));
        }
        public IProduct FindProduct(string symbol, ProductType productType, string exchange)
        {
            return new RealtimeProduct(ApiClient.FindProduct(symbol, productType, exchange));
        }
        public IProduct FindProduct(string symbol, ProductType productType, string exchange, string currency)
        {
            return new RealtimeProduct(ApiClient.FindProduct(symbol, productType, currency));
        }

        public void PlaceOrder(IOrder order)
        {
            ApiClient.PlaceOrder((order as RealtimeOrder).Order);
        }
        public void PlaceOrder(IOrder order, bool waitTillFinishes)
        {
            ApiClient.PlaceOrder((order as RealtimeOrder).Order, waitTillFinishes);
        }

        public void CancelOrder(IOrder order)
        {
            ApiClient.CancelOrder((order as RealtimeOrder).Order);
        }
        public void CancelAllOrders()
        {
            ApiClient.CancelAllOrders();
        }

        public List<IPosition> GetAllPositions()
        {
            return ApiClient.GetAllPositions().Select(x => new RealtimePosition(x)).Cast<IPosition>().ToList();
        }

        public IAccount GetAccountSummary()
        {
            return new RealtimeAccount(ApiClient.GetAccountSummary());
        }
    }
}
