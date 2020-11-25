using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpML.Recurrent.Models;

namespace SharpML.Recurrent.Loss
{
    [Serializable]
    public class LossSumOfSquares : ILoss
    {
        public void Backward(Matrix actualOutput, Matrix targetOutput)
        {
            for (int i = 0; i < targetOutput.W.Length; i++)
            {
                double errDelta = actualOutput.W[i] - targetOutput.W[i];
                actualOutput.Dw[i] += errDelta;
            }
        }

        public double Measure(Matrix actualOutput, Matrix targetOutput)
        {
            double sum = 0;

            for (int i = 0; i < targetOutput.W.Length; i++)
            {
                double errDelta = actualOutput.W[i] - targetOutput.W[i];
                sum += Math.Abs(errDelta);
            }

            return sum / targetOutput.W.Length;
        }
    }
}
