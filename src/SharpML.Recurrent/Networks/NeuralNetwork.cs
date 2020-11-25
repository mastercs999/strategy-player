using System;
using System.Collections.Generic;
using SharpML.Recurrent.Models;

namespace SharpML.Recurrent.Networks
{
    [Serializable]
    public class NeuralNetwork
    {
        readonly List<ILayer> _layers;
        private Random Rng;

        public NeuralNetwork(List<ILayer> layers, Random rng)
        {
            this._layers = layers;
            this.Rng = rng;
        }

        public Matrix Activate(Matrix input, Graph g)
        {
            Matrix prev = input;

            foreach (ILayer layer in _layers)
                prev = layer.Activate(prev, g);

            return prev;
        }

        public void ResetState()
        {
            foreach (ILayer layer in _layers)
                layer.ResetState();
        }

        public List<Matrix> GetParameters()
        {
            List<Matrix> result = new List<Matrix>(_layers.Count);

            foreach (ILayer layer in _layers)
                result.AddRange(layer.GetParameters());

            return result;
        }

        public void SaveWeights()
        {
            foreach (ILayer layer in _layers)
                layer.SaveWeights();
        }

        public void RestoreWeights()
        {
            foreach (ILayer layer in _layers)
                layer.RestoreWeights();
        }

        public void ShakeWeights()
        {
            foreach (ILayer layer in _layers)
                foreach (Matrix m in layer.GetParameters())
                    for (int i = 0; i < m.W.Length; ++i)
                        m.W[i] = m.W[i] + (Rng.NextDouble() * 2 - 1) * 0.0000001;
        }
    }
}
