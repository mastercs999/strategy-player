using System;
using System.Collections.Generic;
using SharpML.Recurrent.Models;

namespace SharpML.Recurrent.Networks
{
     [Serializable]
    public class LinearLayer : ILayer
    {
         readonly Matrix _w;
        //no biases

        public LinearLayer(int inputDimension, int outputDimension, Random rng)
        {
            _w = Matrix.Random(outputDimension, inputDimension, 1 / Math.Sqrt(inputDimension), rng);
        }

        public Matrix Activate(Matrix input, Graph g)
        {
            Matrix returnObj = g.Mul(_w, input);

            return returnObj;
        }

        public void ResetState()
        {

        }

        public List<Matrix> GetParameters()
        {
            List<Matrix> result = new List<Matrix>();

            result.Add(_w);

            return result;
        }

        public void SaveWeights()
        {
            throw new NotImplementedException();
        }

        public void RestoreWeights()
        {
            throw new NotImplementedException();
        }
    }
}
