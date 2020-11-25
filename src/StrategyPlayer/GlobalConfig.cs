using Analyzer.Strategies.FBLL;
using Analyzer.Strategies.FutureFinder;
using Analyzer.Strategies.Ninety;
using Analyzer.Strategies.Ultimate;
using Analyzer.TradingBase;
using Analyzer.TradingBase.Composition;
using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace StrategyPlayer
{
    public class GlobalConfig
    {
        public CompositeConfig CompositeConfig { get; set; }

        public FbllConfig FbllConfig { get; set; }
        public NinetyConfig NinetyConfig { get; set; }
        public SharedConfig StratStatConfig { get; set; }
        public UltimateConfig UltimateConfig { get; set; }
        public FutureFinderConfig FutureFinderConfig { get; set; }




        public void Fix()
        {
            CompositeConfig.Random = new Random();

            foreach (PropertyInfo property in typeof(SharedConfig).GetProperties().Where(x => x.CanWrite && x.Name != nameof(SharedConfig.Name)))
            {
                object value = property.GetValue(CompositeConfig);

                typeof(FbllConfig).GetProperty(property.Name).SetValue(FbllConfig, value);
                typeof(NinetyConfig).GetProperty(property.Name).SetValue(NinetyConfig, value);
                typeof(SharedConfig).GetProperty(property.Name).SetValue(StratStatConfig, value);
                typeof(UltimateConfig).GetProperty(property.Name).SetValue(UltimateConfig, value);
                typeof(FutureFinderConfig).GetProperty(property.Name).SetValue(FutureFinderConfig, value);
            }
        }
    }
}
