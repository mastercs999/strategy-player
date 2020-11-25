using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Indicators
{
    public class Ema : IIndicator<decimal>
    {
        private int Period;
        private decimal PreviousSma;
        private decimal Multiplier;

        private int Processed;
        private SimpleMovingAverage SimpleMovingAverage;

        public Ema(int period)
        {
            Period = period;
            Multiplier = 2m / (Period + 1);

            Processed = 0;
            SimpleMovingAverage = new SimpleMovingAverage(period);
        }




        public decimal Next(decimal input)
        {
            if (Processed < Period)
            {
                ++Processed;
                return PreviousSma = SimpleMovingAverage.Next(input);
            }

            return PreviousSma = (input - PreviousSma) * Multiplier + PreviousSma;
        }

        public decimal Next(decimal open, decimal high, decimal low, decimal close)
        {
            throw new NotImplementedException();
        }
    }
}
