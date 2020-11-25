using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Mocking.Api.Models
{
    [Serializable]
    public class BacktestPosition : IPosition
    {
        public IProduct Product { get; private set; }
        public decimal Size { get; set; }
        public decimal UnitCost { get; set; }

        public BacktestPosition(IProduct product, decimal size, decimal cost)
        {
            Product = product;
            Size = size;
            UnitCost = cost;
        }
    }
}
