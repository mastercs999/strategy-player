using CSharpAPISync.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Mocking.Api.Models
{
    public class RealtimeAccount : IAccount
    {
        public decimal EquityWithLoanValue => Account.EquityWithLoanValue;
        public decimal GrossPositionValue => Account.GrossPositionValue;

        public Account Account { get; private set; }




        public RealtimeAccount(Account account)
        {
            Account = account;
        }
    }
}
