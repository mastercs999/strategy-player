using System;

namespace SharpML.Recurrent.Activations
{
    [Serializable]
    public class LinearUnit : INonlinearity
    {
        public LinearUnit()
        {

        }

        public double Forward(double x)
        {
            return x;
        }

        public double Backward(double x)
        {
            return 1.0;
        }
    }
}
