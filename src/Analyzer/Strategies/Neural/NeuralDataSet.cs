using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpML.Recurrent;
using SharpML.Recurrent.DataStructs;
using SharpML.Recurrent.Loss;

namespace Analyzer.Strategies.Neural
{
    [Serializable]
    internal class NeuralDataSet : DataSet
    {
        public NeuralDataSet(List<NeuroItem> data, int trainCount)
        {
            Training = CreateSequences(data.Take(trainCount).ToList());
            Validation = CreateSequences(data.Skip(trainCount).ToList());
            InputDimension = Training[0].Steps[0].Input.Rows;
            OutputDimension = Training[0].Steps[0].TargetOutput.Rows;
            LossTraining = new LossSumOfSquares();
            LossReporting = new LossSumOfSquares();
        }

        private List<DataSequence> CreateSequences(List<NeuroItem> data)
        {
            // Create sequences
            List<DataSequence> sequences = new List<DataSequence>();
            DataSequence ds = new DataSequence();
            sequences.Add(ds);
            ds.Steps = new List<DataStep>();

            foreach (NeuroItem item in data)
                ds.Steps.Add(new DataStep(item.Input, item.Output));

            return sequences;
        }
    }
}
