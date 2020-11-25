using CSharpAPISync.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Mocking.Api.Models
{
    [Serializable]
    public class BacktestAccount : IAccount
    {
        public decimal EquityWithLoanValue { get; set; }
        public decimal GrossPositionValue { get; set; }
        public decimal TotalCashValue { get; set; }




        public BacktestAccount(decimal capital)
        {
            EquityWithLoanValue = capital;
            GrossPositionValue = 0;
            TotalCashValue = capital;
        }
    }
}
