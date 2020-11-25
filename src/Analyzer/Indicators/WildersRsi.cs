using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Indicators
{
    public class WildersRsi : IIndicator<decimal>
    {
        private int Period;

        private bool Ready;
        private List<decimal> History;
        private decimal AvgUp;
        private decimal AvgDown;

        public WildersRsi(int period)
        {
            Period = period;

            Ready = false;
            History = new List<decimal>();
        }

        public decimal Next(decimal input)
        {
            // Not ready yet
            if (!Ready)
                return PreInit(input);

            // Calculate the difference
            History.Add(input);
            History.RemoveAt(0);
            decimal move = History[History.Count - 1] - History[History.Count - 2];
            decimal lastUp = move > 0 ? Math.Abs(move) : 0;
            decimal lastDown = move < 0 ? Math.Abs(move) : 0;

            // Update averages
            AvgUp = ((AvgUp * (Period - 1)) + lastUp) / Period;
            AvgDown = ((AvgDown * (Period - 1)) + lastDown) / Period;
            if (AvgDown == 0)
                return 100;

            return 100 - (100.0m / (1.0m + AvgUp / AvgDown));
        }

        private decimal PreInit(decimal input)
        {
            // Add to input
            History.Add(input);

            // Not enough data
            if (History.Count <= Period)
                return 50;

            // Enough data, we are ready now
            Ready = true;

            // Enough data, calculate first and set it as ready
            List<decimal> up = new List<decimal>();
            List<decimal> down = new List<decimal>();
            for (int i = 1; i < Period + 1; ++i)
                if (History[i] > History[i - 1])
                {
                    up.Add(Math.Abs(History[i] - History[i - 1]));
                    down.Add(0);
                }
                else
                {
                    up.Add(0);
                    down.Add(Math.Abs(History[i] - History[i - 1]));
                }

            AvgUp = up.Sum() / Period;
            AvgDown = down.Sum() / Period;
            if (AvgDown == 0)
                return 100;

            return 100 - (100.0m / (1.0m + AvgUp / AvgDown));
        }

        public decimal Next(decimal open, decimal high, decimal low, decimal close)
        {
            throw new InvalidOperationException();
        }
    }
}
