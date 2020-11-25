using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Indicators
{
    public class SimpleMovingAverage : IIndicator<decimal>
    {
        private int Period;
        private Queue<decimal> History;
        private decimal Sum;

        public SimpleMovingAverage(int period)
        {
            Period = period;
            History = new Queue<decimal>();
            Sum = 0;
        }

        public decimal Next(decimal input)
        {
            decimal output = 0;

            // Update history
            History.Enqueue(input);
            if (History.Count > Period)
                output = History.Dequeue();

            // Update sum
            Sum = Sum - output + input;

            return Sum / History.Count;
        }

        public decimal Next(decimal open, decimal high, decimal low, decimal close)
        {
            throw new InvalidOperationException();
        }
    }
}
