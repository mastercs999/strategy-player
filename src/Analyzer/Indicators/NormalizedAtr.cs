using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Indicators
{
    public class NormalizedAtr : IIndicator<decimal>
    {
        private readonly IIndicator<decimal> SimpleMovingAverage;
        private Bar PreviousBar;




        public NormalizedAtr(int period)
        {
            SimpleMovingAverage = new SimpleMovingAverage(period);
        }

        public decimal Next(decimal input)
        {
            throw new NotImplementedException();
        }

        public decimal Next(decimal open, decimal high, decimal low, decimal close)
        {
            Bar currentBar = new Bar(open, high, low, close);
            if (PreviousBar == null)
            {
                PreviousBar = currentBar;
                return high - low;
            }

            decimal val1 = high - low;
            decimal val2 = Math.Abs(high - PreviousBar.Close);
            decimal val3 = Math.Abs(low - PreviousBar.Close);
            decimal tr = Math.Max(val1, Math.Max(val2, val3)) / close;

            PreviousBar = currentBar;

            return SimpleMovingAverage.Next(tr);
        }




        class Bar
        {
            public decimal Open { get; set; }
            public decimal High { get; set; }
            public decimal Low { get; set; }
            public decimal Close { get; set; }

            public Bar(decimal open, decimal high, decimal low, decimal close)
            {
                Open = open;
                High = high;
                Low = low;
                Close = close;
            }
        }
    }
}
