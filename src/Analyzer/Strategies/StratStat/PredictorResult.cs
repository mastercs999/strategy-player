using Analyzer.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Strategies.StratStat
{
    public class PredictorResult
    {
        public bool PatternMatch { get; set; }
        public double ProbabilityOfSuccess { get; set; }
        public Func<Table, int, int, int, bool> CanExitFunc { get; set; }
    }
}
