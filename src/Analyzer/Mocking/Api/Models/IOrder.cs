using CSharpAPISync.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Mocking.Api.Models
{
    public interface IOrder
    {
        decimal Quantity { get; set; }
        OrderAction Action { get; set; }

        OrderStatus Status { get; }
        IProduct Product { get; }
        decimal AverageFillPrice { get; }
        decimal Commission { get; }

        void WaitTillFinishes();
        void WaitForExecutionDetails();
    }
}
