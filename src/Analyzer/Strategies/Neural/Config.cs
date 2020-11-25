using Analyzer.TradingBase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Strategies.Neural
{
    [Serializable]
    public class Config : SharedConfig
    {
        public int Bundles { get; set; }
        public int BatchSize { get; set; }
        public double TrainRatio { get; set; }
        public double LearningRate { get; set; }
        public int Epochs { get; set; }
        public int UpdatePeriod { get; set; }
        public int HiddenDimension { get; set; }
        public string TableFile => Path.Combine(BaseDirectory, "Table", "Table.bin");
        public string ModelFile => Path.Combine(BaseDirectory, "Models", "Model.bin");
    }
}
