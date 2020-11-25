using System;

namespace SharpML.Recurrent.Activations
{
    [Serializable]
    public class SigmoidUnit : INonlinearity
    {
        public SigmoidUnit()
        {

        }

        public double Forward(double x)
        {
            return 1 / (1 + Math.Exp(-x));
        }

        public double Backward(double x)
        {
            double act = Forward(x);

            return act * (1 - act);
        }
    }
}
