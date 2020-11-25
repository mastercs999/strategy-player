using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSharpAPISync.Support;

namespace Analyzer.Mocking.Api.Models.Orders
{
    public class BacktestMarket : BacktestOrder, IMarket
    {
        public override OrderAction Action { get; set; }
        public override decimal Quantity { get; set; }

        public BacktestMarket(IProduct product, OrderAction action, decimal quantity, OrderStatus status) : base(status)
        {
            Action = action;
            Quantity = quantity;

            Product = product;
        }
    }
}
