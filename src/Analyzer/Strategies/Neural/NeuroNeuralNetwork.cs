using SharpML.Recurrent.Activations;
using SharpML.Recurrent.Models;
using SharpML.Recurrent.Networks;
using SharpML.Recurrent.Trainer;
using SharpML.Recurrent.Util;
using Common;
using Common.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Analyzer.TradingBase;

namespace Analyzer.Strategies.Neural
{
    [Serializable]
    public class NeuroNeuralNetwork
    {
        private Config Config;
        private Stats[] InputStats;
        private Stats[] OutputStats;
        private NeuralNetwork Network;

        public NeuroNeuralNetwork( Config config)
        {
            Config = config;
        }

        public void Train(List<NeuroItem> data)
        {
            // Normalize data
            Normalize(data, out InputStats, out OutputStats);

            NeuralDataSet neuralDataSet = new NeuralDataSet(data, (int)Math.Round(data.Count * Config.TrainRatio));

            // Create network
            Network = NetworkBuilder.MakeFeedForward(
                    neuralDataSet.InputDimension,
                    Config.HiddenDimension,
                    1,
                    neuralDataSet.OutputDimension,
                    new SigmoidUnit(),
                    new LinearUnit(),
                    Config.Random
                );

            // Train
            Trainer.Train(Network, Config.LearningRate, Config.Epochs, Config.UpdatePeriod, neuralDataSet, Config.Random, false, out double trainLoss, out double validLoss);
        }

        public double[] Predict(double[] input)
        {
            // Normalize
            for (int i = 0; i < input.Length; ++i)
                input[i] = input[i].Normalize(0, 1, InputStats[i].Mean, InputStats[i].StdDev);

            // Make prediction
            Graph g = new Graph(false);
            Matrix mi = new Matrix(input);
            Matrix mo = Network.Activate(mi, g);

            // Unnormalize
            double[] output = mo.W;
            for (int i = 0; i < output.Length; ++i)
                output[i] = output[i].Normalize(OutputStats[i].Mean, OutputStats[i].StdDev, 0, 1);

            return output;
        }

        private void Normalize(List<NeuroItem> data, out Stats[] inputStats, out Stats[] outputStats)
        {
            inputStats = new Stats[data.First().Input.Length];
            outputStats = new Stats[data.First().Output.Length];

            // Normalize input
            for (int col = 0; col < data.First().Input.Length; ++col)
            {
                // Values
                List<double> values = data.Select(x => x.Input[col]).ToList();

                // Find stats   
                double mean = values.Average();
                double stdDev = values.Sum(d => Math.Pow(d - mean, 2));
                stdDev = Math.Sqrt(stdDev / (values.Count() - 1));

                // Store stats
                inputStats[col] = new Stats(mean, stdDev);

                // Normalize
                for (int row = 0; row < data.Count; ++row)
                    data[row].Input[col] = data[row].Input[col].Normalize(0, 1, inputStats[col].Mean, inputStats[col].StdDev);
            }

            // Normalize output
            for (int col = 0; col < data.First().Output.Length; ++col)
            {
                // Values
                List<double> values = data.Select(x => x.Output[col]).ToList();

                // Find stats   
                double mean = values.Average();
                double stdDev = values.Sum(d => Math.Pow(d - mean, 2));
                stdDev = Math.Sqrt(stdDev / (values.Count() - 1));

                // Store stats
                outputStats[col] = new Stats(mean, stdDev);

                // Normalize
                for (int row = 0; row < data.Count; ++row)
                    data[row].Output[col] = data[row].Output[col].Normalize(0, 1, outputStats[col].Mean, outputStats[col].StdDev);
            }
        }
    }
}
