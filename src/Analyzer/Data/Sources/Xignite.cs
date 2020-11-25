using Analyzer.TradingBase;
using Common;
using Common.Loggers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Data.Sources
{
    /// <summary>
    /// Realtime:
    /// Originates from reverse engineering of https://www.investopedia.com/markets/stocks/aapl/. Two http requests are enough to get all the data.
    /// </summary>
    public class Xignite : IRealtimeSource
    {
        private ILogger _____________________________________________________________________________Logger;




        public Xignite() : this(new SilentLogger())
        {

        }
        public Xignite(ILogger logger)
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
            Dictionary<string, Symbol> tickerToSymbol = symbols.ToDictionary(x => x.Ticker);

            // Parse xignite credentials
            Symbol majorSymbol = prices.Keys.OrderByDescending(x => x.Volume).First();
            XigniteCredentials credentials = DownloadXigniteCredentials(workingDirectory, majorSymbol.Ticker);
            if (credentials == null)
            {
                _____________________________________________________________________________Logger.Error("Couldn't download Xignite credentials");
                return prices;
            }

            // Prepare url
            string url = $"http://superquotes.xignite.com/xSuperQuotes.json/GetQuotes?IdentifierType=Symbol&Identifiers=___&_token={credentials.Key}&_token_userid={credentials.UserId}";
            url = url.Replace("___", String.Join(",", symbols.Select(x => x.Ticker.Replace(" ", "."))));

            // Download
            string filepath = Path.Combine(workingDirectory, "Online.json");
            _____________________________________________________________________________Logger.Info($"Download Xignite online data from {url} to {filepath}");
            Utilities.DownloadFile(url, filepath);
            _____________________________________________________________________________Logger.Info($"File {filepath} was downloaded");

            // Parse file
            JArray data = JArray.Parse(File.ReadAllText(filepath));
            foreach (JObject obj in data)
            {
                if ((string)obj["Outcome"] != "Success")
                    continue;

                string ticker = ((string)obj["Identifier"]).Replace(".", " ");
                decimal price = (decimal)obj["Last"];

                // Fill in the symbol
                if (tickerToSymbol.TryGetValue(ticker, out Symbol symbol))
                {
                    // Store price
                    prices[symbol] = price;
                    _____________________________________________________________________________Logger.Info($"Xignite found online data for {symbol.Ticker}: {price}");

                    // Call callback
                    onPriceFound?.Invoke(symbol, price);
                }
                else
                    _____________________________________________________________________________Logger.Warning($"Unknown symbol '{ticker}' from Xignite");
            }
            _____________________________________________________________________________Logger.Info("Xignite has finished downloading realtime prices");

            return prices;
        }




        private XigniteCredentials DownloadXigniteCredentials(string directory, string ticker)
        {
            // Download html file
            string url = "https://www.investopedia.com/markets/stocks/" + ticker;
            string filePath = Path.Combine(directory, "Investopedia.html");
            _____________________________________________________________________________Logger.Info($"Downloading investopedia page to get xignite data from {url} to {filePath}");
            Utilities.DownloadFileAkaBrowser(url, filePath);
            _____________________________________________________________________________Logger.Info("Investopedia file was downloaded");

            // Parse xignite credentials
            List<string> content = File.ReadAllLines(filePath).ToList();
            int initializationLine = content.FindIndex(x => x.Contains("new Xignite("));
            if (initializationLine == -1)
                return null;
            string key = content[initializationLine + 1].Trim();
            string userId = content[initializationLine + 2].Trim();

            // Remove 
            key = BetweenApostrophes(key);
            userId = BetweenApostrophes(userId);

            return new XigniteCredentials()
            {
                Key = key,
                UserId = userId
            };
        }
        private string BetweenApostrophes(string str)
        {
            // 0'234'
            int start = str.IndexOf('\'');
            int end = str.LastIndexOf('\'');

            return str.Substring(start + 1, end - start - 1);
        }

        private class XigniteCredentials
        {
            public string Key { get; set; }
            public string UserId { get; set; }
        }
    }
}
