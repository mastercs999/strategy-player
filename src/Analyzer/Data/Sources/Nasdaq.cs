using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Analyzer.TradingBase;
using Common.Loggers;
using System.IO;
using System.Net;
using Common;
using Common.Extensions;
using CsQuery;
using System.Web;
using System.Globalization;
using System.Collections.Specialized;
using Newtonsoft.Json;
using System.Threading;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Analyzer.Data.Sources
{
    /// <summary>
    /// However there is a throthling on the server which works something like this: You can send roughly 70 requests in a few minutes and then I'll block you.
    /// It means we can use this data source for limited number of symbols only.
    /// 
    /// Historical:
    /// - Speed - Slow
    /// - Current day - Yes
    /// - Start year - 2008
    /// - Fields - Adjusted
    /// - Limitations - Possible block due to high number of http requests
    /// 
    /// Realtime:
    /// We download realtime data directly from nasdaq.com symbol by symbol.
    /// </summary>
    public class Nasdaq : IHistoricalSource, IRealtimeSource
    {
        private ILogger _____________________________________________________________________________Logger;




        public Nasdaq() : this(new SilentLogger())
        {

        }
        public Nasdaq(ILogger logger)
        {
            _____________________________________________________________________________Logger = logger;
        }




        public void DownloadHistorical(List<Symbol> symbols, string workingDirectory, Func<Symbol, string> targetFileFunc)
        {
            // Download the data
            _____________________________________________________________________________Logger.Info("Nasdaq is about to download historical data for symbols: " + String.Join(", ", symbols.Select(x => x.Ticker)));
            foreach (Symbol symbol in symbols)
            {
                string resultFile = targetFileFunc(symbol);
                try
                {
                    // Target file
                    string targetFile = Path.Combine(workingDirectory, symbol.Ticker + ".html");
                    DownloadHistorical(symbol, targetFile);

                    // Parse file 
                    List<StockBar> bars = ParseHistorical(targetFile);
                    if (!bars.Any())
                    {
                        _____________________________________________________________________________Logger.Warning($"Nasdaq couldn't download data for {symbol.Ticker}");
                        continue;
                    }

                    // Create header
                    File.WriteAllText(resultFile, "date,open,high,low,close,volume,adj_open,adj_high,adj_low,adj_close,adj_volume,dividend,split\n");

                    // Append data
                    File.AppendAllLines(resultFile, (bars as IEnumerable<StockBar>).Reverse().Select(x =>
                    {
                        return string.Join(",", new string[]
                        {
                            x.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),    // Date
                            "-1",            // Unadjusted
                            "-1",
                            "-1",
                            "-1",
                            "-1",
                            x.AdjustedOpen.ToString(CultureInfo.InvariantCulture),        // Adjusted
                            x.AdjustedHigh.ToString(CultureInfo.InvariantCulture),
                            x.AdjustedLow.ToString(CultureInfo.InvariantCulture),
                            x.AdjustedClose.ToString(CultureInfo.InvariantCulture),
                            x.Volume.ToString(CultureInfo.InvariantCulture),
                            "-1",            // Dividend
                            "-1",            // Split
                        });
                    }).OrderBy(x => x));
                    _____________________________________________________________________________Logger.Info($"Nasdaq successfully processed data for {symbol.Ticker}");
                }
                catch (Exception ex)
                {
                    _____________________________________________________________________________Logger.Error($"Nasdaq couldn't download data for {symbol.Ticker}", ex);
                    if (File.Exists(resultFile))
                        File.Delete(resultFile);
                }
            }

            _____________________________________________________________________________Logger.Info("All symbols from Nasdaq were processed");
        }
        public Dictionary<Symbol, decimal?> DownloadRealtime(List<Symbol> symbols, string workingDirectory)
        {
            return DownloadRealtime(symbols, workingDirectory, null);
        }
        public Dictionary<Symbol, decimal?> DownloadRealtime(List<Symbol> symbols, string workingDirectory, Action<Symbol, decimal> onPriceFound)
        {
            // Create the dictionary
            Dictionary<Symbol, decimal?> prices = symbols.ToDictionary(x => x, x => (decimal?)null);

            // Download the data
            ServicePointManager.DefaultConnectionLimit = Math.Max(symbols.Count * 10, ServicePointManager.DefaultConnectionLimit);
            _____________________________________________________________________________Logger.Info("Nasdaq is about to download online data for symbols: " + String.Join(", ", symbols.Select(x => x.Ticker)));
            symbols.ParallelLoop((i, symbol) =>
            {
                try
                {
                    // Name of the file
                    string symbolFilePath = Path.Combine(workingDirectory, "Online.html").AddVersion(symbol.Ticker);

                    // Download time and sales
                    try
                    {
                        DownloadTimeAndSales(symbolFilePath, symbol);
                    }
                    catch (WebException ex) when (ex.Response is HttpWebResponse response && response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        _____________________________________________________________________________Logger.Warning($"Nasdaq couldn't download prices for {symbol.Ticker}. Limit of downloads reached.");
                        return;
                    }

                    // Parse html
                    CQ dom = File.ReadAllText(symbolFilePath);

                    // Find first row
                    string rawPrice = dom.Find("#quotes_content_left__panelTradeData table tbody tr:first-child td")[1]?.InnerText?.Trim();
                    if (rawPrice == null)
                    {
                        _____________________________________________________________________________Logger.Warning($"Nasdaq couldn't find price for {symbol.Ticker}");
                        return;
                    }

                    // Parse price
                    rawPrice = HttpUtility.HtmlDecode(rawPrice);
                    rawPrice = rawPrice.Substring(rawPrice.IndexOf('$') + 1).Trim();
                    decimal price = decimal.Parse(rawPrice, CultureInfo.InvariantCulture);

                    // Add price
                    prices[symbol] = price;
                    _____________________________________________________________________________Logger.Info($"Nasdaq found realtime price {price} for {symbol.Ticker}");

                    // Call callback
                    onPriceFound?.Invoke(symbol, price);
                }
                catch (Exception ex)
                {
                    _____________________________________________________________________________Logger.Error($"Nasdaq couldn't download realtime price for {symbol.Ticker}", ex);
                }
            }, 50);
            _____________________________________________________________________________Logger.Info("Nasdaq download of realtime data ended");

            return prices;
        }




        private void DownloadHistorical(Symbol symbol, string filePath)
        {
            string ticker = symbol.Ticker.Replace(' ', '-').ToLower();

            // Create URL
            string url = $"https://www.nasdaq.com/api/v1/historical/___/stocks/1980-01-01/2100-01-01";
            url = url.Replace("___", ticker);

            _____________________________________________________________________________Logger.Info($"Downloading Nasdaq historical data for {symbol.Ticker} from {url} to {filePath}");
            using (WebClient wc = new WebClient())
            {

                wc.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64; rv:52.0) Gecko/20100101 Firefox/52.0");
                wc.Headers.Add("Accept", "*/*");
                wc.Headers.Add(HttpRequestHeader.Host, "www.nasdaq.com");
                wc.Headers.Add(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.5");
                wc.Headers.Add(HttpRequestHeader.AcceptEncoding, "identity");
                wc.Headers.Add("X-Requested-With", "XMLHttpRequest");
                wc.Headers.Add(HttpRequestHeader.ContentType, "application/json");

                wc.DownloadFile(url, filePath);
            }
            _____________________________________________________________________________Logger.Info($"Nasdaq historical data for {symbol.Ticker} were downloaded");
        }
        private List<StockBar> ParseHistorical(string file)
        {
            return File.ReadLines(file).Skip(1).Select(line =>
            {
                string[] parts = line.Replace("$", "").Split(',').Select(x => x.Trim()).Where(x => !String.IsNullOrEmpty(x)).ToArray();

                return new StockBar()
                {
                    Date = DateTimeOffset.ParseExact(parts[0], "MM/dd/yyyy", CultureInfo.InvariantCulture),

                    AdjustedOpen = decimal.Parse(parts[3], CultureInfo.InvariantCulture),
                    AdjustedHigh = decimal.Parse(parts[4], CultureInfo.InvariantCulture),
                    AdjustedLow = decimal.Parse(parts[5], CultureInfo.InvariantCulture),
                    AdjustedClose = decimal.Parse(parts[1], CultureInfo.InvariantCulture),
                    AdjustedVolume = decimal.Parse(parts[2], CultureInfo.InvariantCulture),
                };
            }).ToList();
        }
        private void DownloadTimeAndSales(string filePath, Symbol symbol)
        {
            string url = $"https://www.nasdaq.com/symbol/{symbol.Ticker.Replace(" ", "-").ToLower()}/time-sales";
            _____________________________________________________________________________Logger.Info($"Downloading Nasdaq online data for {symbol.Ticker} from {url} to {filePath}");
            NameValueCollection headers = new NameValueCollection()
            {
                { "Host", "www.nasdaq.com" },
                { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:62.0) Gecko/20100101 Firefox/62.0" },
                { "Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8" },
                { "Accept-Language", "en-US,en;q=0.5" },
                { "Accept-Encoding", "identity" },
                { "DNT", "1" },
                { "Connection", "close" },
                { "Upgrade-Insecure-Requests", "1" }
            };
            new RawHttpRequest().DownloadFile(url, headers, filePath);
            _____________________________________________________________________________Logger.Info($"Nasdaq online data for {symbol.Ticker} were downloaded");
        }
    }
}
