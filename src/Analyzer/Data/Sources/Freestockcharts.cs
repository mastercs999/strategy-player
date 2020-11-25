using Analyzer.TradingBase;
using Common;
using Common.Loggers;
using Common.Extensions;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Data.Sources
{
    /// <summary>
    /// I created a widget with S&P100 on http://widgets.freestockcharts.com/
    /// 
    /// Symbols:
    /// Unusable - symbol list haphazardly omits a symbol from time to time. In addition the list seems to be little out of date.
    /// 
    /// Realtime:
    /// Works nice, but we need two http requests.
    /// </summary>
    public class Freestockcharts : ISymbolSource, IRealtimeSource
    {
        private ILogger _____________________________________________________________________________Logger;




        public Freestockcharts() : this(new SilentLogger())
        {

        }
        public Freestockcharts(ILogger logger)
        {
            _____________________________________________________________________________Logger = logger;
        }





        public List<Symbol> DownloadSymbols(string workingDirectory, bool useCache)
        {
            // Get target file
            string filepath = Path.Combine(workingDirectory, "Online.html");
            Directory.CreateDirectory(workingDirectory);

            // Download symbols from freestockcharts
            _____________________________________________________________________________Logger.Info("Getting symbol list from Freestockcharts...");
            List<FreestockchartsSymbol> freestockchartsSymbols = DownloadData(filepath, useCache);
            _____________________________________________________________________________Logger.Info("We have received following symbols from Freestockcharts: " + String.Join(", ", freestockchartsSymbols.Select(x => x.Ticker).OrderBy(x => x)));

            return freestockchartsSymbols.Cast<Symbol>().ToList();
        }
        public Dictionary<Symbol, decimal?> DownloadRealtime(List<Symbol> symbols, string workingDirectory)
        {
            return DownloadRealtime(symbols, workingDirectory, null);
        }
        public Dictionary<Symbol, decimal?> DownloadRealtime(List<Symbol> symbols, string workingDirectory, Action<Symbol, decimal> onPriceFound)
        {
            // Create the directory
            string filepath = Path.Combine(workingDirectory, "Online.html");

            // Create the dictionary
            Dictionary<Symbol, decimal?> prices = symbols.ToDictionary(x => x, x => (decimal?)null);

            // Download prices
            List<FreestockchartsSymbol> freestockchartsSymbols = DownloadData(filepath, false);

            // Fill the data
            _____________________________________________________________________________Logger.Info("Freestockcharts downloading online data for following tickers: " + String.Join(", ", symbols.Select(x => x.Ticker).OrderBy(x => x)));
            foreach (Symbol symbol in symbols)
            {
                decimal? price = freestockchartsSymbols.SingleOrDefault(x => x.Ticker == symbol.Ticker)?.Price;
                if (price.HasValue)
                {
                    prices[symbol] = price.Value;
                    _____________________________________________________________________________Logger.Info($"Freestockcharts found realtime data for {symbol.Ticker}: {prices[symbol]}");

                    // Call callback
                    onPriceFound?.Invoke(symbol, price.Value);
                }
                else
                    _____________________________________________________________________________Logger.Warning($"Freestockcharts couldn't find realtime data for {symbol.Ticker}");
            }
            _____________________________________________________________________________Logger.Info("Freestockcharts has finished realtime prices download");

            return prices;
        }




        private List<FreestockchartsSymbol> DownloadData(string file, bool useCache)
        {
            // Download source file
            string srcFile = file.AddVersion(1);
            string url = "https://widgets.tc2000.com/WidgetServer.ashx?id=98371";
            if (!useCache || !File.Exists(srcFile))
            {
                _____________________________________________________________________________Logger.Info($"Downloading Freestockcharts source html to {srcFile} from {url}");
                Utilities.DownloadFileAkaBrowser(url, srcFile);
                _____________________________________________________________________________Logger.Info("Source file downloaded");
            }

            // Get symbols ids
            string symsLine = File.ReadAllLines(srcFile).Single(x => x.Trim().StartsWith("var syms = [", StringComparison.OrdinalIgnoreCase));
            symsLine = symsLine.Substring(symsLine.IndexOf('[') + 1);
            symsLine = symsLine.Substring(0, symsLine.LastIndexOf(']'));
            int[] ids = symsLine.Split(new char[] { ',' }).Select(x => int.Parse(x, CultureInfo.InvariantCulture)).ToArray();

            // Get prices for given ids
            url = "https://widgets.tc2000.com/Widgetserver.ashx?svc=LISTCONTENTS&setid=4805940&sort=price&sortDir=desc&apptoken=cd0c052d-06cb-453c-b632-0cc5245b8c05&host=https://widgets.tc2000.com/GridWidget.aspx?id=98371";
            string priceFile = file.AddVersion(2);
            if (!useCache || !File.Exists(priceFile))
            {
                _____________________________________________________________________________Logger.Info($"Downloading Freestockcharts data to {priceFile} from {url}");
                using (WebClient webClient = Utilities.PrepareBrowserWebClient())
                {
                    // Set client
                    webClient.Headers[HttpRequestHeader.Accept] = "*/*";
                    webClient.Headers[HttpRequestHeader.AcceptLanguage] = "cs,en-US;q=0.7,en;q=0.3";
                    webClient.Headers[HttpRequestHeader.Referer] = "https://widgets.tc2000.com/GridWidget.aspx?id=68773";
                    webClient.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                    webClient.Headers["X-Requested-With"] = "XMLHttpRequest";
                    webClient.Headers[HttpRequestHeader.Host] = "widgets.tc2000.com";

                    // Download
                    File.WriteAllText(priceFile, webClient.UploadString(url, string.Join(",", ids)));
                }
                _____________________________________________________________________________Logger.Info("Data downloaded");
            }

            // Parse symbols
            return (JObject.Parse(File.ReadAllText(priceFile))["Data"] as JObject).Properties().Select(x => new FreestockchartsSymbol()
            {
                Ticker = x.Name,
                Price = decimal.Parse((string)x.Value[1]["v"][0], CultureInfo.InvariantCulture)
            }).ToList();
        }

        


        private class FreestockchartsSymbol : Symbol
        {
            public decimal Price { get; set; }
        }
    }
}
