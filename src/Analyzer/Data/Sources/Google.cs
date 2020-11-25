using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Analyzer.TradingBase;
using System.IO;
using Common;
using Common.Extensions;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Net;
using Common.Loggers;
using System.Threading;
using CsQuery;

namespace Analyzer.Data.Sources
{
    /// <summary>
    /// Historical:
    /// We have also API for historical data, but Google is throttling number of requests :(
    /// http://finance.google.com/finance/historical?q=QQQQ&startdate=01-01-2000&enddate=06-20-2010&output=csv
    /// 
    /// Realtime:
    /// Using many http request we download realtime data for stock. But we have to guess exchange which is part of the url. Also number of http request is throttled.
    /// </summary>
    public class Google : IRealtimeSource
    {
        private ILogger _____________________________________________________________________________Logger;
        private static readonly CultureInfo EnParsingCulture = new CultureInfo("en-US");
        private static readonly CultureInfo CzParsingCulture = new CultureInfo("cs-CZ");




        public Google() : this(new SilentLogger())
        {

        }
        public Google(ILogger logger)
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

            // Run queries
            ServicePointManager.DefaultConnectionLimit = Math.Max(symbols.Count * 10, ServicePointManager.DefaultConnectionLimit);
            _____________________________________________________________________________Logger.Info("Starting downloading Google online data for following tickers: " + String.Join(", ", symbols.Select(x => x.Ticker).OrderBy(x => x)));
            symbols.ParallelLoop((i, symbol) =>
            {
                // Download
                string[] possibleExchanges = new string[] { "", "NASDAQ", "NYSE" };
                foreach (string exchange in possibleExchanges)
                {
                    try
                    {
                        string filepath = Path.Combine(workingDirectory, "Online.html");
                        decimal? price = Download(symbol, exchange, filepath, possibleExchanges);
                        if (price.HasValue)
                        {
                            // Save the price
                            prices[symbol] = price.Value;
                            _____________________________________________________________________________Logger.Info($"Google found price for {symbol.Ticker}: {price.Value}");

                            // Call callback
                            onPriceFound?.Invoke(symbol, price.Value);
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _____________________________________________________________________________Logger.Error($"Google couldn't download realtime price for {symbol.Ticker} and exchange {exchange}", ex);
                    }
                }
            }, symbols.Count);
            _____________________________________________________________________________Logger.Info("Google has finished downloading realtime prices");

            return prices;
        }




        private decimal? Download(Symbol symbol, string exchange, string filepath, string[] possibleExchanges)
        {
            // Get html with necessary codes
            string filename = filepath.AddVersion(symbol.Ticker).AddVersion(exchange);
            string exchangePrefix = String.IsNullOrEmpty(exchange) ? "" : $"{exchange}:";
            string url = $"https://www.google.com/search?tbm=fin&q={exchangePrefix}" + symbol.Ticker.Replace(" ", "");
            _____________________________________________________________________________Logger.Info($"Downloading Google HTML for {symbol.Ticker} on exchange {exchange}...");
            Utilities.DownloadFileAkaBrowser(url, filename);
            _____________________________________________________________________________Logger.Info($"Google data HTML {symbol.Ticker} and exchange {exchange} downloaded to {filename}");

            // Parse out ei
            CQ dom = File.ReadAllText(filename);
            string ei = dom["input[name=ei]"].Val();

            // Parse the code
            string[] lines = File.ReadAllLines(filename);
            string codeLine = lines.SingleOrDefault(x => x.Contains($"\"entities\""));
            if (codeLine == null)
                return null;
            int startPosition = codeLine.IndexOf("\\\"") + 2;
            int length = codeLine.Substring(startPosition).IndexOf("\\\"");
            string code = codeLine.Substring(startPosition, length);
            
            // Get the json with price
            url = $"https://www.google.com/async/finance_wholepage_price_updates?ei={ei}&safe=off&yv=3&async=mids:{code},currencies:,_fmt:jspb";
            filename = Path.ChangeExtension(filename, "json");
            _____________________________________________________________________________Logger.Info($"Downloading Google JSON for {symbol.Ticker} on exchange {exchange} from {url}...");
            Utilities.DownloadFileAkaBrowser(url, filename);
            _____________________________________________________________________________Logger.Info($"Google JSON for {symbol.Ticker} was downloaded");
            lines = File.ReadAllLines(filename);
            JObject json = JObject.Parse(String.Join("", lines.Skip(1)));

            decimal value = json["PriceUpdate"].First.First.First[17][4].Value<decimal>();

            return value;
        }
    }
}
