using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSharpAPISync.Support;
using CSharpAPISync.Models;

namespace Analyzer.Mocking.Api.Models
{
    public abstract class RealtimeOrder : IOrder
    {
        public virtual decimal Quantity { get; set; }
        public virtual OrderAction Action { get; set; }

        public OrderStatus Status => Order.Status;
        public IProduct Product { get; private set; }
        public decimal AverageFillPrice => Order.AverageFillPrice;
        public decimal Commission => Order.Commission;

        public Order Order { get; private set; }

        protected RealtimeOrder(Order order)
        {
            Order = order;

            Product = new RealtimeProduct(order.Product);
        }




        public void WaitTillFinishes()
        {
            Order.WailTillFinishes();
        }
        public void WaitForExecutionDetails()
        {
            Order.WaitForExecutionDetails();
        }
    }
}
