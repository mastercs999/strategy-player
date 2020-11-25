using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSharpAPISync.Support;

namespace Analyzer.Mocking.Api.Models
{
    [Serializable]
    public abstract class BacktestOrder : IOrder
    {
        public virtual decimal Quantity { get; set; }
        public virtual OrderAction Action { get; set; }

        public OrderStatus Status { get; set; }
        public IProduct Product { get; protected set; }
        public decimal AverageFillPrice { get; set; }
        public decimal Commission { get; set; }

        protected BacktestOrder(OrderStatus status)
        {
            Status = status;
        }

        public void WaitTillFinishes()
        {

        }

        public void WaitForExecutionDetails()
        {

        }
    }
}
