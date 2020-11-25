using Common.Loggers;
using CSharpAPISync;
using CSharpAPISync.Models;
using CSharpAPISync.Models.Orders;
using CSharpAPISync.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StrategyPlayer
{
    public static class IBTester
    {
        public static void Test()
        {
            // Connect
            ApiClient apiClient = new ApiClient("", 0, 4002, new FileLogger(@"c:\Users\MASTER\Desktop\Stocks\IBLog"));
            apiClient.Connect();

            Account account = apiClient.GetAccountSummary();

            // Get product
            Product product = apiClient.FindProduct("AAPL", ProductType.Stock);

            // Place order
            Order order = new Market(product, OrderAction.Buy, 1);
            apiClient.PlaceOrder(order);
            order.WaitForExecutionDetails();

            // Cancel the order or all orders
            // apiClient.CancelOrder(order);
            // apiClient.CancelAllOrders();

            // Get all positions
            //var positionsTask = apiClient.GetAllPositions();
            //positionsTask.Wait();
            //List<Position> positions = positionsTask.Result;

            //Position p = positions.First();
            //Order o = new Market(p.Product, OrderAction.Buy, 30);
            //apiClient.PlaceOrder(o).Wait();

            //positionsTask = apiClient.GetAllPositions();
            //positionsTask.Wait();
            //positions = positionsTask.Result;

            Console.WriteLine("FINISHED");
            Thread.Sleep(5000000);
        }
    }
}
