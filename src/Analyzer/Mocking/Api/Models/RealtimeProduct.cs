using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSharpAPISync.Support;
using CSharpAPISync.Models;

namespace Analyzer.Mocking.Api.Models
{
    public class RealtimeProduct : IProduct
    {
        public int Id => Product.Id;
        public string Symbol => Product.Symbol;
        public string Exchange => Product.Exchange;
        public string Currency => Product.Currency;
        public List<DateRange> TradingHours => Product.TradingHours;

        public Product Product { get; private set; }

        public RealtimeProduct(Product product)
        {
            Product = product;
        }
    }
}
