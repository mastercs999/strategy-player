using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSharpAPISync.Support;

namespace Analyzer.Mocking.Api.Models
{
    [Serializable]
    public class BacktestProduct : IProduct
    {
        public int Id { get; private set; }
        public string Symbol { get; private set; }
        public string Exchange { get; private set; }
        public string Currency { get; private set; }
        public List<DateRange> TradingHours { get; private set; }

        public BacktestProduct(Random random, string symbol, string exchange, string currency, List<DateRange> tradingHours)
        {
            Id = random.Next();
            Symbol = symbol;
            Exchange = exchange;
            Currency = currency;
            TradingHours = tradingHours;
        }
    }
}
