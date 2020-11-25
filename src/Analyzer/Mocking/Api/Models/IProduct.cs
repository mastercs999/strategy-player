using CSharpAPISync.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Mocking.Api.Models
{
    public interface IProduct
    {
        int Id { get; }
        string Symbol { get;}
        string Exchange { get; }
        string Currency { get; }
        List<DateRange> TradingHours { get; }
    }
}
