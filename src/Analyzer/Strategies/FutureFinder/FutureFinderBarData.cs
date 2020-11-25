using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Strategies.FutureFinder
{
    public class FutureFinderBarData
    {
        public double[] Features { get; set; }

        public override string ToString()
        {
            return (base.ToString() + Utilities.CsvSeparator + String.Join(Utilities.CsvSeparator, Features));
        }
    }
}
