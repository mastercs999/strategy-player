using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Indicators
{
    public class UltimateOscillator : IIndicator<decimal>
    {
        private int P1, P2, P3;
        private int MaxPeriod;
        private List<Bar> History;
        private List<decimal> Bps, Trs;

        public UltimateOscillator(int p1, int p2, int p3)
        {
            P1 = p1;
            P2 = p2;
            P3 = p3;
            MaxPeriod = new int[] { P1, P2, P3 }.Max();
            History = new List<Bar>();
            Bps = new List<decimal>();
            Trs = new List<decimal>();
        }

        public decimal Next(decimal input)
        {
            throw new InvalidOperationException();
        }

        public decimal Next(decimal open, decimal high, decimal low, decimal close)
        {
            // Append bar
            Bar current = new Bar(open, high, low, close);
            Add(History, current, MaxPeriod);

            // Get help bar
            if (History.Count < 2)
                return 50;
            Bar previous = History[1];

            // Count buying presure and true range
            decimal bp = current.Close - Math.Min(current.Low, previous.Close);
            decimal tr = Math.Max(current.High, previous.Close) - Math.Min(current.Low, previous.Close);

            // Apend help variables
            Add(Bps, bp, MaxPeriod);
            Add(Trs, tr, MaxPeriod);

            // Count new value
            decimal sum1 = Trs.Take(P1).Sum();
            decimal sum2 = Trs.Take(P2).Sum();
            decimal sum3 = Trs.Take(P3).Sum();
            decimal average1 = sum1 == 0 ? 0 : Bps.Take(P1).Sum() / sum1;
            decimal average2 = sum2 == 0 ? 0 : Bps.Take(P2).Sum() / sum2;
            decimal average3 = sum3 == 0 ? 0 : Bps.Take(P3).Sum() / sum3;

            return 100 * (4 * average1 + 2 * average2 + average3) / (4 + 2 + 1);
        }

        private void Add<T>(List<T> list, T item, int maxCount)
        {
            list.Insert(0, item);
            if (list.Count > maxCount)
                list.RemoveAt(maxCount);
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
