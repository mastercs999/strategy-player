using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Indicators
{
    public class DirectionalIndicator : IIndicator<DirectionalIndicatorResult>
    {
        private int Period;
        private decimal PreviousDmPlus;
        private decimal PreviousDmMinus;
        private decimal PreviousTr;
        private Bar PreviousBar;




        public DirectionalIndicator(int period)
        {
            Period = period;
        }




        public DirectionalIndicatorResult Next(decimal input)
        {
            throw new NotImplementedException();
        }

        public DirectionalIndicatorResult Next(decimal open, decimal high, decimal low, decimal close)
        {
            Bar currentBar = new Bar(open, high, low, close);
            if (PreviousBar == null)
            {
                PreviousBar = currentBar;
                return new DirectionalIndicatorResult(0, 0);
            }

            decimal val1 = high - low;
            decimal val2 = Math.Abs(high - PreviousBar.Close);
            decimal val3 = Math.Abs(low - PreviousBar.Close);
            decimal tr = Math.Max(val1, Math.Max(val2, val3));

            decimal upMove = high - PreviousBar.High;
            decimal downMove = PreviousBar.Low - low;
            decimal dmPlus = upMove > downMove && upMove > 0 ? upMove : 0;
            decimal dmMinus = downMove > upMove && downMove > 0 ? downMove : 0;

            PreviousDmPlus = dmPlus = PreviousDmPlus - PreviousDmPlus / Period + dmPlus;
            PreviousDmMinus = dmMinus = PreviousDmMinus - PreviousDmMinus / Period + dmMinus;
            PreviousTr = tr = PreviousTr - PreviousTr / Period + tr;

            decimal diPlus = tr == 0 ? 50 : 100 * dmPlus / tr;
            decimal diMinus = tr == 0 ? 50 : 100 * dmMinus / tr;

            PreviousBar = currentBar;

            return new DirectionalIndicatorResult(diPlus, diMinus);
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




    public class DirectionalIndicatorResult
    {

        public decimal DiPlus { get; set; }
        public decimal DiMinus { get; set; }




        public DirectionalIndicatorResult(decimal diPlus, decimal diMinus)
        {
            DiPlus = diPlus;
            DiMinus = diMinus;
        }
    }
}
