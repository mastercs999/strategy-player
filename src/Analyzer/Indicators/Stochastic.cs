using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Indicators
{
    public class Stochastic : IIndicator<StochasticResult>
    {
        private int Period;
        private int KSmooth;
        private int DSmooth;
        private decimal Max = -1;
        private decimal Min = decimal.MaxValue;
        private Queue<decimal> HighsHistory;
        private Queue<decimal> LowsHistory;
        private SimpleMovingAverage KMovingAverage;
        private SimpleMovingAverage DMovingAverage;

        public Stochastic(int period, int kSmooth, int dSmooth)
        {
            Period = period;
            KSmooth = kSmooth;
            DSmooth = dSmooth;
            HighsHistory = new Queue<decimal>(period + 1);
            LowsHistory = new Queue<decimal>(period + 1);
            KMovingAverage = new SimpleMovingAverage(KSmooth);
            DMovingAverage = new SimpleMovingAverage(DSmooth);
        }




        public StochasticResult Next(decimal input)
        {
            throw new NotImplementedException();
        }

        public StochasticResult Next(decimal open, decimal high, decimal low, decimal close)
        {
            HighsHistory.Enqueue(high);
            LowsHistory.Enqueue(low);

            if (high > Max)
                Max = high;
            if (low < Min)
                Min = low;

            if (HighsHistory.Count > Period)
            {
                decimal output = HighsHistory.Dequeue();
                if (output == Max)
                    Max = HighsHistory.Max();

                output = LowsHistory.Dequeue();
                if (output == Min)
                    Min = LowsHistory.Min();
            }

            decimal result = Max == Min ? 50 : (close - Min) / (Max - Min) * 100;

            decimal k = KSmooth == 1 ? result : KMovingAverage.Next(result);
            decimal d = DSmooth == 1 ? k : DMovingAverage.Next(k);

            return new StochasticResult()
            {
                K = k,
                D = d
            };
        }
    }




    public class StochasticResult
    {
        public decimal K { get; set; }
        public decimal D { get; set; }
    }
}
