using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Indicators
{
    public interface IIndicator<TResult>
    {
        TResult Next(decimal input);
        TResult Next(decimal open, decimal high, decimal low, decimal close);
    }
}
