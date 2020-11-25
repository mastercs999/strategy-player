using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSharpAPISync.Models;

namespace Analyzer.Mocking.Api.Models
{
    public class RealtimePosition : IPosition
    {
        public IProduct Product { get; private set; }
        public decimal Size => Position.Size;
        public decimal UnitCost => Position.UnitCost;

        public Position Position { get; private set; }

        public RealtimePosition(Position position)
        {
            Position = position;

            Product = new RealtimeProduct(position.Product);
        }
    }
}
