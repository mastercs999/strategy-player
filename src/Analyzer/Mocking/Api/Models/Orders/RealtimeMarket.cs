using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSharpAPISync.Support;
using CSharpAPISync.Models.Orders;

namespace Analyzer.Mocking.Api.Models.Orders
{
    public class RealtimeMarket : RealtimeOrder, IMarket
    {
        public override OrderAction Action { get => Market.Action; set => Market.Action = value; }
        public override decimal Quantity { get => Market.Quantity; set => Market.Quantity = value; }

        public Market Market { get; private set; }

        public RealtimeMarket(Market market) : base(market)
        {
            Market = market;
        }
    }
}
