using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpML.Recurrent.Util
{
    public class Gaussian
    {
        private double Mean;
        private double StdDev;
        private Random Random;

        public Gaussian(double mean, double stddev, Random random)
        {
            Mean = mean;
            StdDev = stddev;
            Random = random;
        }

        public double Next()
        {
            double u1 = Random.NextDouble();
            double u2 = Random.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2); 

            return Mean + StdDev * randStdNormal;
        }
    }
}
