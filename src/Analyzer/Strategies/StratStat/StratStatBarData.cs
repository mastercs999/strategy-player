using Analyzer.Indicators;
using Analyzer.TradingBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Strategies.StratStat
{
    public class StratStatBarData
    {
        public decimal[] IndicatorValues { get; set; }
        public PredictorResult[] Results { get; private set; }
        public bool HasMatch { get; private set; }
        public PredictorResult BestPredictorResult { get; private set; }

        public void StoreResults(PredictorResult[] results)
        {
            List<PredictorResult> withMatch = results.Where(x => x.PatternMatch).ToList();

            Results = results;
            HasMatch = withMatch.Any();
            BestPredictorResult = !withMatch.Any() ? null : withMatch.Aggregate((max, next) => next.ProbabilityOfSuccess > max.ProbabilityOfSuccess ? next : max);
        }
    }
}
