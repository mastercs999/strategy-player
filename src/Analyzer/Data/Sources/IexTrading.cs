using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Analyzer.TradingBase;
using Common;
using Common.Loggers;
using Common.Extensions;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace Analyzer.Data.Sources
{
    /// <summary>
    /// Obvious IEX stock exchange moved to cloud: https://iexcloud.io/pricing/
    /// The message limit for free plan is 500 000 messages per month. Which is waaaay too low.
    /// 
    /// Realtime:
    /// Data are delayed by dozens of seconds, but fine.
    /// </summary>
    public class IexTrading : IRealtimeSource
    {
        private ILogger _____________________________________________________________________________Logger;




        public IexTrading() : this(new SilentLogger())
        {

        }
        public IexTrading(ILogger logger)
        {
            _____________________________________________________________________________Logger = logger;
        }




        public Dictionary<Symbol, decimal?> DownloadRealtime(List<Symbol> symbols, string workingDirectory)
        {
            return DownloadRealtime(symbols, workingDirectory, null);
        }
        public Dictionary<Symbol, decimal?> DownloadRealtime(List<Symbol> symbols, string workingDirectory, Action<Symbol, decimal> onPriceFound)
        {
            // Create the dictionary
            Dictionary<Symbol, decimal?> prices = symbols.ToDictionary(x => x, x => (decimal?)null);

            // Download online data
            ServicePointManager.DefaultConnectionLimit = Math.Max(symbols.Count * 10, ServicePointManager.DefaultConnectionLimit);
            _____________________________________________________________________________Logger.Info("Starting downloading IexTrading online data for following tickers: " + String.Join(", ", symbols.Select(x => x.Ticker).OrderBy(x => x)));
            symbols.Split(90).ParallelLoop((i, batchSymbols) =>
            {
                try
                {
                    // Download
                    string targetFile = Path.Combine(workingDirectory, batchSymbols.First().Ticker + ".json");
                    DownloadOnlineData(batchSymbols, targetFile);

                    // Parse found prices
                    Dictionary<string, decimal> foundPrices = JObject.Parse(File.ReadAllText(targetFile)).Properties().Select(x =>
                    {
                        JObject quoteData = x.Value["quote"] as JObject;

                        string ticker = ((string)quoteData["symbol"]).Replace('.', ' ');
                        decimal price = (decimal)quoteData["latestPrice"];

                        return (ticker: ticker, price: price);
                    }).ToDictionary(x => x.ticker, x => x.price);

                    // Save them into dictionary
                    foreach (Symbol symbol in batchSymbols)
                    {
                        if (foundPrices.TryGetValue(symbol.Ticker, out decimal price))
                        {
                            // Save a price
                            prices[symbol] = price;
                            _____________________________________________________________________________Logger.Info($"IexTrading found price for {symbol.Ticker}: {price}");

                            // Call callback
                            onPriceFound?.Invoke(symbol, price);
                        }
                        else
                            _____________________________________________________________________________Logger.Warning($"IexTrading couldn't find price for {symbol.Ticker}");
                    }
                }
                catch (Exception ex)
                {
                    _____________________________________________________________________________Logger.Error("IexTrading couldn't download realtime prices for: " + String.Join(", ", batchSymbols.Select(x => x.Ticker)), ex);
                }
            }, 3);
            _____________________________________________________________________________Logger.Info("IexTrading finished downloading realtime data");

            return prices;
        }




        private void DownloadOnlineData(List<Symbol> symbols, string file)
        {
            // Create url
            string url = "https://api.iextrading.com/1.0/stock/market/batch?symbols=___&types=quote";
            string targetUrl = url.Replace("___", String.Join(",", symbols.Select(x => x.Ticker.Replace(" ", "."))));

            // Download
            _____________________________________________________________________________Logger.Info($"IexTrading is going to download online data from {targetUrl} to {file}");
            Utilities.DownloadFile(targetUrl, file);
            _____________________________________________________________________________Logger.Info($"Online data were downloaded to {file}");
        }
    }
}
