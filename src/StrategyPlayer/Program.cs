using Analyzer.Data;
using Analyzer.Data.Sources;
using Analyzer.Strategies.FBLL;
using Analyzer.Strategies.FutureFinder;
using Analyzer.Strategies.Ninety;
using Analyzer.Strategies.Ultimate;
using Analyzer.Tests;
using Analyzer.TradingBase;
using Analyzer.TradingBase.Composition;
using Common;
using Common.Loggers;
using GatewayController;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StrategyPlayer
{
    class Program
    {
        static void Main(string[] args)
        {
            // Write version
            Console.WriteLine("Strategy Player v" + ((Func<Version, string>)((v) => $"{v.Major}.{v.Minor}.{v.Build}"))(Assembly.GetEntryAssembly().GetName().Version));

            // Deserialize config
            GlobalConfig globalConfig = Serializer.DeserializeXml<GlobalConfig>("GlobalConfig.xml");
            globalConfig.Fix();

            // Create strategies
            FbllTradingSystem fbll = new FbllTradingSystem(globalConfig.FbllConfig);
            NinetyTradingSystem ninety = new NinetyTradingSystem(globalConfig.NinetyConfig);
            UltimateTradingSystem ultimate = new UltimateTradingSystem(globalConfig.UltimateConfig);
            FutureFinderSystem futureFinder = new FutureFinderSystem(globalConfig.FutureFinderConfig);
            CompositeTradingSystem compositeTradingSystem = new CompositeTradingSystem(globalConfig.CompositeConfig, new StrategySettings(ninety, 1.0m));

            // Run the strategy
            //fbll.Backtest();
            //ninety.Backtest();
            //ultimate.Backtest();
            futureFinder.Backtest();
            //compositeTradingSystem.Run();

            // Play finish sound
            new SoundPlayer("nautical008.wav").PlaySync();
        }
    }
}
