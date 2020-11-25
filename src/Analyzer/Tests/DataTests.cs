using Analyzer.Data;
using Analyzer.Data.Sources;
using Analyzer.TradingBase;
using Common.Extensions;
using Common.Loggers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Tests
{
    public static class DataTests
    {
        public static readonly string TestPath = @"C:\Users\MASTER\Desktop\Stocks\Tests";





        public static void Symbols()
        {
            DownloadManager downloadManager = new DownloadManager(TestPath);
            
            // Download
            Dictionary<string, string[]> sourceToTickers = new Dictionary<string, string[]>();
            foreach (ISymbolSource source in new ISymbolSource[] { new Wikipedia(), new Barchart(), new SharesI(), new Freestockcharts() })
            {
                List<Symbol> symbols = downloadManager.DownloadSymbols(false);

                sourceToTickers.Add(source.GetType().Name, symbols.Select(x => x.Ticker).ToArray());
            }

            // Remove the same parts
            HashSet<string> common = sourceToTickers.Values.Aggregate((x, y) => x.Intersect(y).ToArray()).ToHashSet();

            // Print differencies
            foreach (KeyValuePair<string, string[]> kvp in sourceToTickers)
            {
                Console.WriteLine(kvp.Key);
                Console.WriteLine("--------------------");
                Console.WriteLine(string.Join(",", kvp.Value.Except(common).OrderBy(x => x)));
                Console.WriteLine();
            }
        }
        public static void RealtimeSingle()
        {
            // Create providers
            IRealtimeSource[] sources = new IRealtimeSource[]
            {
                new Xignite(new ConsoleLogger())
            };
            //sources = typeof(Barchart).Assembly.GetTypes().Where(x => x.GetInterface(nameof(IRealtimeSource)) != null).Select(x => Activator.CreateInstance(x) as IRealtimeSource).ToArray();

            // Display prices
            string symbol = "MSFT";
            while (true)
            {
                foreach (IRealtimeSource src in sources)
                    Console.WriteLine(symbol + "\t" + src.GetType().Name + "\t\t" + FetchPrice(symbol, src, new ConsoleLogger()));
                Console.WriteLine();

                Console.ReadLine();
            }
        }
        public static void RealtimeAll()
        {
            DownloadManager downloadManager = new DownloadManager(TestPath);

            // Create providers
            IRealtimeSource[] sources = new IRealtimeSource[]
            {
                new Xignite(),
                new Freestockcharts(),
                new Google(),
            };
            sources = typeof(Barchart).Assembly.GetTypes().Where(x => x.GetInterface(nameof(IRealtimeSource)) != null).Select(x => Activator.CreateInstance(x) as IRealtimeSource).ToArray();

            // Download symbols
            List<Symbol> symbols = downloadManager.DownloadSymbols(true);

            foreach (IRealtimeSource provider in sources)
            {
                // Download prices
                Stopwatch sw = new Stopwatch(); sw.Start();
                Dictionary<Symbol, decimal?> prices = downloadManager.DownloadRealtime(symbols, provider);

                // Print how many prices we got
                sw.Stop();
                Console.WriteLine(provider.GetType().Name + "\t" + sw.ElapsedMilliseconds + "\t" + prices.Count(x => x.Value.HasValue) + "/" + prices.Count);
                Console.WriteLine(
                    "AAPL: " + prices.Single(x => x.Key.Ticker == "AAPL").Value +
                    "\tMSFT: " + prices.Single(x => x.Key.Ticker == "MSFT").Value +
                    "\tGILD: " + prices.Single(x => x.Key.Ticker == "GILD").Value);
            }
        }
        public static void RealtimeParallel()
        {
            DownloadManager downloadManager = new DownloadManager(TestPath, new FileLogger(TestPath));
            
            // Download symbols
            List<Symbol> symbols = downloadManager.DownloadSymbols(true).ToList();

            Console.WriteLine(DateTime.Now);

            Dictionary<Symbol, decimal?> prices = downloadManager.DownloadRealtimeParallel(symbols);

            Console.WriteLine(DateTime.Now);
            Console.WriteLine(prices.Count(x => x.Value.HasValue) + "/" + prices.Count);
        }
        public static void HistoricalAll()
        {
            DownloadManager downloadManager = new DownloadManager(TestPath);

            // Download symbols
            List<Symbol> symbols = downloadManager.DownloadSymbols(true).Where(x => x.Ticker == "AAPL").ToList();

            // Download historical data
            Stopwatch sw = new Stopwatch(); sw.Start();
            downloadManager.DownloadHistorical(symbols, true);

            sw.Stop();
            Console.WriteLine(sw.ElapsedMilliseconds);
        }




        private static decimal FetchPrice(string symbol, IRealtimeSource source, ILogger logger)
        {
            // Create symbol list
            List<Symbol> symbols = new List<Symbol>()
            {
                new Symbol()
                {
                    Ticker = symbol
                }
            };

            return new DownloadManager(TestPath, logger).DownloadRealtime(symbols, source).First().Value.Value;
        }
    }
}
