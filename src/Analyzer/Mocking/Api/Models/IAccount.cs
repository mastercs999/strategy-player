using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Mocking.Api.Models
{
    public interface IAccount
    {
        decimal GrossPositionValue { get; }
        decimal EquityWithLoanValue { get; }
    }
}
