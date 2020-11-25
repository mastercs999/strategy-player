using System;

namespace SharpML.Recurrent.Activations
{
    [Serializable]
    public class SineUnit : INonlinearity
    {
        public SineUnit()
        {

        }

        public double Forward(double x)
        {
            return Math.Sin(x);
        }

        public double Backward(double x)
        {
            return Math.Cos(x);
        }
    }
}
