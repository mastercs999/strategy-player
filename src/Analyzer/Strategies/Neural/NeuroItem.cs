using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Strategies.Neural
{
    public class NeuroItem
    {
        public double[] Input { get; set; }
        public double[] Output { get; set; }

        public NeuroItem(double[] input, double[] output)
        {
            Input = input;
            Output = output;
        }
    }
}
