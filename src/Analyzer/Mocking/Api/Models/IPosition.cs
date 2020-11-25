using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Mocking.Api.Models
{
    public interface IPosition
    {
        IProduct Product { get; }
        decimal Size { get; }
        decimal UnitCost { get; }
    }
}
