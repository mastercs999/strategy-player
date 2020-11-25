using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.TradingBase
{
    public class SharedConfig
    {
        public string Name { get; set; }
        public string BaseDirectory { get; set; }
        public decimal Capital { get; set; }
        public int ConfidenceMinimum { get; set; }
        public Random Random { get; set; }
        public string DataDirectory => Path.Combine(BaseDirectory, "Data");
        public string ResultFile => Path.Combine(BaseDirectory, "Results.csv");
        public string CustomFile => Path.Combine(BaseDirectory, "Custom.csv");
    }
}
