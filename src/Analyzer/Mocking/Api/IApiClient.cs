using Analyzer.Mocking.Api.Models;
using CSharpAPISync.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Mocking.Api
{
    public interface IApiClient
    {
        void Connect();
        void ConnectSafe();
        void Disconnect();

        IProduct FindProduct(string symbol, ProductType productType);
        IProduct FindProduct(string symbol, ProductType productType, string exchange);
        IProduct FindProduct(string symbol, ProductType productType, string exchange, string currency);

        void PlaceOrder(IOrder order);
        void PlaceOrder(IOrder order, bool waitTillFinishes);

        void CancelOrder(IOrder order);
        void CancelAllOrders();

        List<IPosition> GetAllPositions();

        IAccount GetAccountSummary();
    }
}
