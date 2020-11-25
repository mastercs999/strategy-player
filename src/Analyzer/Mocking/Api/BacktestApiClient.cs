using Analyzer.Data;
using Analyzer.Exceptions;
using Analyzer.Mocking.Api.Models;
using Analyzer.Mocking.Time;
using Analyzer.TradingBase;
using Common;
using Common.Extensions;
using CSharpAPISync.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Mocking.Api
{
    public class BacktestApiClient : IApiClient
    {
        private bool Connected;
        private Random Random;
        private Dictionary<DateTimeOffset, StockBar[]> DateToBars;
        private HashSet<DateTimeOffset> TradedDays;
        private DateTimeOffset MinDate;
        private DateTimeOffset MaxDate;
        private IDateTimeProvider DateTimeProvider;
        private List<BacktestPosition> OpenedPositions;
        private BacktestAccount Account;

        public BacktestApiClient(Random random, string dataDirectory, IDateTimeProvider dateTimeProvider, BacktestAccount account)
        {
            Table table = new DataManager(dataDirectory).CreateDataTable(true, true);

            Connected = false;
            Random = random;
            DateToBars = table.Bars.ToDictionary(x => x.First(y => y != null).Date, x => x);
            TradedDays = new HashSet<DateTimeOffset>(table.Bars.Select(x => x.First(y => y != null).Date));
            MinDate = TradedDays.Min();
            MaxDate = TradedDays.Max();
            DateTimeProvider = dateTimeProvider;
            OpenedPositions = new List<BacktestPosition>();
            Account = account;
        }




        public void Connect()
        {
            Connected = true;
        }
        public void ConnectSafe()
        {
            Connected = true;
        }
        public void Disconnect()
        {
            Connected = false;
        }

        public IProduct FindProduct(string symbol, ProductType productType)
        {
            return FindProduct(symbol, productType, "SMART");
        }
        public IProduct FindProduct(string symbol, ProductType productType, string exchange)
        {
            return FindProduct(symbol, productType, exchange, "USD");
        }
        public IProduct FindProduct(string symbol, ProductType productType, string exchange, string currency)
        {
            if (!Connected)
                throw new ConnectionException("Backtest api is not connected");

            // Find next trading dates
            List<DateRange> tradedDays = new List<DateRange>();
            DateTimeOffset today = DateTimeProvider.Today;
            while (tradedDays.Count < 2 && today <= MaxDate)
            {
                if (TradedDays.Contains(today))
                    tradedDays.Add(new DateRange()
                    {
                        From = new DateTimeOffset(today.Year, today.Month, today.Day, 14, 30, 0, TimeSpan.Zero),
                        To = new DateTimeOffset(today.Year, today.Month, today.Day, 21, 00, 0, TimeSpan.Zero),
                    });

                today = today.AddDays(1);
            }

            return new BacktestProduct(Random, symbol, exchange, currency, tradedDays);
        }

        public void PlaceOrder(IOrder order)
        {
            // Set status
            (order as BacktestOrder).Status = OrderStatus.Submitted;

            // Get basic trade info
            decimal quantityChange = order.Quantity * (order.Action == OrderAction.Buy ? 1 : -1);
            decimal stockValue = FindProductClose(DateTimeProvider.Today, order.Product.Symbol);
            decimal totalChange = quantityChange * stockValue;

            // Save position into position list
            BacktestPosition openedPosition = OpenedPositions.SingleOrDefault(x => x.Product.Symbol == order.Product.Symbol);
            if (openedPosition == null)
            {
                openedPosition = new BacktestPosition(order.Product, 0, 0);
                OpenedPositions.Add(openedPosition);
            }

            // Update position's size and value
            openedPosition.Size += quantityChange;
            openedPosition.UnitCost = stockValue;

            // Update account details
            Account.TotalCashValue -= totalChange;

            // If size is 0, delete the position
            if (openedPosition.Size == 0)
                OpenedPositions.Remove(openedPosition);

            // Order is completed
            (order as BacktestOrder).Status = OrderStatus.Filled;
            (order as BacktestOrder).AverageFillPrice = stockValue;
            (order as BacktestOrder).Commission = 0;
        }
        public void PlaceOrder(IOrder order, bool waitTillFinishes)
        {
            PlaceOrder(order);
        }

        public void CancelOrder(IOrder order)
        {
            ;
        }
        public void CancelAllOrders()
        {
            ;
        }

        public List<IPosition> GetAllPositions()
        {
            // Update unit size
            foreach (BacktestPosition position in OpenedPositions)
                position.UnitCost = FindProductClose(DateTimeProvider.Today, position.Product.Symbol);

            return OpenedPositions.Select(x => x.Clone()).Cast<IPosition>().ToList();
        }

        public IAccount GetAccountSummary()
        {
            // Count all necessary data
            Account.GrossPositionValue = OpenedPositions.Sum(x => x.Size * FindProductClose(DateTimeProvider.Today, x.Product.Symbol));
            Account.EquityWithLoanValue = Account.TotalCashValue + Account.GrossPositionValue;

            return Account.Clone();
        }




        private decimal FindProductClose(DateTimeOffset date, string symbol)
        {
            while (date >= MinDate)
            {
                if (DateToBars.TryGetValue(date, out StockBar[] bars))
                {
                    decimal? value = bars.SingleOrDefault(x => x != null && x.Symbol.Ticker == symbol)?.AdjustedClose;
                    if (value.HasValue)
                        return value.Value;
                }

                date = date.AddDays(-1);
            }

            throw new DataException($"No close found for {symbol}");
        }
    }
}
