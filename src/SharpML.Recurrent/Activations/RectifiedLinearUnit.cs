using System;

namespace SharpML.Recurrent.Activations
{
    [Serializable]
    public class RectifiedLinearUnit : INonlinearity
    {
        private readonly double _slope;

        public RectifiedLinearUnit()
        {
            this._slope = 0;
        }

        public RectifiedLinearUnit(double slope)
        {
            this._slope = slope;
        }

        public double Forward(double x)
        {
            if (x >= 0)
                return x;
            else
                return x * _slope;
        }

        public double Backward(double x)
        {
            if (x >= 0)
                return 1.0;
            else
                return _slope;
        }
    }
}
