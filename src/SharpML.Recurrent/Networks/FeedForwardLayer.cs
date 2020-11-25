using System;
using System.Collections.Generic;
using SharpML.Recurrent.Activations;
using SharpML.Recurrent.Models;

namespace SharpML.Recurrent.Networks
{
    [Serializable]
    public class FeedForwardLayer : ILayer
    {
        private Matrix _w;
        private Matrix _b;
        readonly INonlinearity _f;

        private Matrix _wB;
        private Matrix _bB;

        public FeedForwardLayer(int inputDimension, int outputDimension, INonlinearity f, Random rng)
        {
            _w = Matrix.Random(outputDimension, inputDimension, 1 / Math.Sqrt(inputDimension), rng);
            _b = new Matrix(outputDimension);
            this._f = f;

            SaveWeights();
        }

        public Matrix Activate(Matrix input, Graph g)
        {
            return g.Nonlin(_f, g.Add(g.Mul(_w, input), _b));
        }

        public void ResetState()
        {

        }

        public List<Matrix> GetParameters()
        {
            List<Matrix> result = new List<Matrix>(2);

            result.Add(_w);
            result.Add(_b);

            return result;
        }

        public void SaveWeights()
        {
            _wB = _w.Clone();
            _bB = _b.Clone();
        }

        public void RestoreWeights()
        {
            _w = _wB.Clone();
            _b = _bB.Clone();
        }
    }
}
