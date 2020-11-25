using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpML.Recurrent.Activations;
using SharpML.Recurrent.Networks;

namespace SharpML.Recurrent.Util
{
    public static class NetworkBuilder
    {

        public static NeuralNetwork MakeLstm(int inputDimension, int hiddenDimension, int hiddenLayers, int outputDimension, INonlinearity decoderUnit, Random rng)
        {
            List<ILayer> layers = new List<ILayer>();

            for (int h = 0; h < hiddenLayers; h++)
                if (h == 0)
                    layers.Add(new LstmLayer(inputDimension, hiddenDimension, rng));
                else
                    layers.Add(new LstmLayer(hiddenDimension, hiddenDimension, rng));

            layers.Add(new FeedForwardLayer(hiddenDimension, outputDimension, decoderUnit, rng));

            return new NeuralNetwork(layers, rng);
        }

        public static NeuralNetwork MakeLstmWithInputBottleneck(int inputDimension, int bottleneckDimension, int hiddenDimension, int hiddenLayers, int outputDimension, INonlinearity decoderUnit, Random rng)
        {
            List<ILayer> layers = new List<ILayer>();

            layers.Add(new LinearLayer(inputDimension, bottleneckDimension, rng));

            for (int h = 0; h < hiddenLayers; h++)
                if (h == 0)
                    layers.Add(new LstmLayer(bottleneckDimension, hiddenDimension, rng));
                else
                    layers.Add(new LstmLayer(hiddenDimension, hiddenDimension, rng));

            layers.Add(new FeedForwardLayer(hiddenDimension, outputDimension, decoderUnit, rng));

            return new NeuralNetwork(layers, rng);
        }

        public static NeuralNetwork MakeFeedForward(int inputDimension, int hiddenDimension, int hiddenLayers, int outputDimension, INonlinearity hiddenUnit, INonlinearity decoderUnit, Random rng)
        {
            List<ILayer> layers = new List<ILayer>();

            for (int h = 0; h < hiddenLayers; h++)
                if (h == 0)
                    layers.Add(new FeedForwardLayer(inputDimension, hiddenDimension, hiddenUnit, rng));
                else
                    layers.Add(new FeedForwardLayer(hiddenDimension, hiddenDimension, hiddenUnit, rng));

            layers.Add(new FeedForwardLayer(hiddenDimension, outputDimension, decoderUnit, rng));

            return new NeuralNetwork(layers, rng);
        }

        public static NeuralNetwork MakeGru(int inputDimension, int hiddenDimension, int hiddenLayers, int outputDimension, INonlinearity decoderUnit, Random rng)
        {
            List<ILayer> layers = new List<ILayer>();

            for (int h = 0; h < hiddenLayers; h++)
                if (h == 0)
                    layers.Add(new GruLayer(inputDimension, hiddenDimension, rng));
                else
                    layers.Add(new GruLayer(hiddenDimension, hiddenDimension, rng));

            layers.Add(new FeedForwardLayer(hiddenDimension, outputDimension, decoderUnit, rng));

            return new NeuralNetwork(layers, rng);
        }

        public static NeuralNetwork MakeRnn(int inputDimension, int hiddenDimension, int hiddenLayers, int outputDimension, INonlinearity hiddenUnit, INonlinearity decoderUnit, Random rng)
        {
            List<ILayer> layers = new List<ILayer>();

            for (int h = 0; h < hiddenLayers; h++)
                if (h == 0)
                    layers.Add(new RnnLayer(inputDimension, hiddenDimension, hiddenUnit, rng));
                else
                    layers.Add(new RnnLayer(hiddenDimension, hiddenDimension, hiddenUnit, rng));

            layers.Add(new FeedForwardLayer(hiddenDimension, outputDimension, decoderUnit, rng));

            return new NeuralNetwork(layers, rng);
        }
    }
}
