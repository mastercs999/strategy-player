using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SharpML.Recurrent;
using SharpML.Recurrent.DataStructs;
using SharpML.Recurrent.Loss;
using SharpML.Recurrent.Models;
using SharpML.Recurrent.Networks;
using SharpML.Recurrent.Util;
using System.Collections;

namespace SharpML.Recurrent.Trainer
{
    public class Trainer
    {
        public static readonly double DecayRate = 0.999;
        public static readonly double SmoothEpsilon = 1e-8;
        public static readonly double GradientClipValue = 5;
        public static readonly double Regularization = 0.000001; // L2 regularization strength

        public static void Train(NeuralNetwork network, double learningRate, int epochs, int updatePeriod, DataSet data, Random rng, bool shuffle, out double trainLoss, out double validLoss)
        {
            trainLoss = double.PositiveInfinity;
            validLoss = double.PositiveInfinity;

            int pokus = 0;
            for (int epoch = 0; epoch < epochs; epoch++)
            {
                if (shuffle)
                {
                    data.Training.First().Steps.Shuffle(rng);
                    data.Validation.First().Steps.Shuffle(rng);
                }

                // Training
                double reportedLossTrain = Pass(learningRate, network, data.Training, true, updatePeriod, data.LossTraining, data.LossReporting);
                if (Double.IsNaN(reportedLossTrain) || Double.IsInfinity(reportedLossTrain))
                    throw new Exception("WARNING: invalid value for training loss. Try lowering learning rate.");

                // Validation
                double reportedLossValidation = Pass(learningRate, network, data.Validation, false, updatePeriod, data.LossTraining, data.LossReporting);
                if (Double.IsNaN(reportedLossValidation) || Double.IsInfinity(reportedLossValidation))
                    throw new Exception("WARNING: invalid value for training loss. Try lowering learning rate.");

                // Save best error and adjust learning rate
                if (reportedLossValidation < validLoss)
                {
                    trainLoss = reportedLossTrain;
                    validLoss = reportedLossValidation;
                    pokus = 0;

                    network.SaveWeights();
                }
                else
                {
                    ++pokus;

                    if (pokus == 3)
                    {
                        learningRate /= 2;

                        network.RestoreWeights();
                        pokus = 0;
                    }
                }

                // End of training
                if (epoch + 1 == epochs)
                {
                    network.RestoreWeights();
                    break;
                }
            }
        }
        public static double OnePass(NeuralNetwork network, double learningRate, int updatePeriod, DataSet data, Random rng)
        {
            double trainLoss = Pass(learningRate, network, data.Training, true, updatePeriod, data.LossTraining, data.LossReporting);
            if (Double.IsNaN(trainLoss) || Double.IsInfinity(trainLoss))
                throw new Exception("WARNING: invalid value for training loss. Try lowering learning rate.");

            return trainLoss;
        }

        public static double Pass(double learningRate, NeuralNetwork network, List<DataSequence> sequences, bool applyTraining, int updatePeriod, ILoss lossTraining, ILoss lossReporting)
        {
            double numerLoss = 0;
            int denomLoss = 0;

            foreach (DataSequence seq in sequences)
            {
                network.ResetState();
                Graph g = new Graph(applyTraining);

                for (int i = 0; i < seq.Steps.Count; ++i)
                {
                    DataStep step = seq.Steps[i];

                    Matrix output = network.Activate(step.Input, g);

                    double loss = lossReporting.Measure(output, step.TargetOutput);
                    if (Double.IsNaN(loss) || Double.IsInfinity(loss))
                        return loss;

                    numerLoss += loss;
                    denomLoss++;

                    if (applyTraining)
                        lossTraining.Backward(output, step.TargetOutput);

                    if (applyTraining && (i % updatePeriod == 0 || i + 1 == seq.Steps.Count))
                    {
                        g.Backward();
                        UpdateModelParams(network, learningRate);

                        g = new Graph(applyTraining);
                    }
                }
            }

            return numerLoss / denomLoss;
        }

        public static void UpdateModelParams(NeuralNetwork network, double stepSize)
        {
            foreach (Matrix m in network.GetParameters())
            {
                for (int i = 0; i < m.W.Length; i++)
                {

                    // rmsprop adaptive learning rate
                    double mdwi = m.Dw[i];
                    m.StepCache[i] = m.StepCache[i] * DecayRate + (1 - DecayRate) * mdwi * mdwi;

                    // gradient clip
                    if (mdwi > GradientClipValue)
                    {
                        mdwi = GradientClipValue;
                    }
                    if (mdwi < -GradientClipValue)
                    {
                        mdwi = -GradientClipValue;
                    }

                    // update (and regularize)
                    m.W[i] += -stepSize * mdwi / Math.Sqrt(m.StepCache[i] + SmoothEpsilon) - Regularization * m.W[i];
                    m.Dw[i] = 0;
                }
            }
        }
    }
}
