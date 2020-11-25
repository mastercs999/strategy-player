using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Analyzer.TradingBase;
using Common.Loggers;
using Common.Extensions;
using CsQuery;
using System.Globalization;
using Common;
using System.Threading;

namespace Analyzer.Data.Sources
{
    /// <summary>
    /// Historical:
    /// I found the link somewhere on the internet.
    /// - Speed - Fast
    /// - Current day - No
    /// - Start year - 2002
    /// - Fields - Adjusted
    /// - Limitations - None
    /// 
    /// Realtime:
    /// Thanks to reverse engineering of http://quotes.wsj.com/AAPL. We have to use many http requests at the same time.
    /// Yet it takes about 10 seconds to get all the data.
    /// </summary>
    public class Wsj : IHistoricalSource, IRealtimeSource
    {
        private ILogger _____________________________________________________________________________Logger;




        public Wsj() : this(new SilentLogger())
        {

        }
        public Wsj(ILogger logger)
        {
            _____________________________________________________________________________Logger = logger;
        }




        public void DownloadHistorical(List<Symbol> symbols, string workingDirectory, Func<Symbol, string> targetFileFunc)
        {
            // Download the data for symbols
            _____________________________________________________________________________Logger.Info("Wsj is about to download historical data for symbols: " + String.Join(", ", symbols.Select(x => x.Ticker)));
            ServicePointManager.DefaultConnectionLimit = Math.Max(symbols.Count * 10, ServicePointManager.DefaultConnectionLimit);
            symbols.ParallelLoop((i, symbol) =>
            {
                string resultFile = targetFileFunc(symbol);
                try
                {
                    // Download the file
                    string targetFile = Path.Combine(workingDirectory, symbol.Ticker + ".csv");
                    DownloadHistoricalData(symbol, targetFile);

                    // Check that all needed information are present
                    string[] lines = File.ReadAllLines(targetFile);
                    if (lines.Length < 3)
                    {
                        _____________________________________________________________________________Logger.Warning($"Wjs couldn't find historical data for {symbol.Ticker}");
                        return;
                    }

                    // Create header
                    File.WriteAllText(resultFile, "date,open,high,low,close,volume,adj_open,adj_high,adj_low,adj_close,adj_volume,dividend,split\n");

                    // Current day
                    string currentDay = DateTimeOffset.UtcNow.UtcDateTime.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

                    // Append data
                    File.AppendAllLines(resultFile, lines.Skip(1).Reverse().Select(line =>
                    {
                        // 06/26/08, 24.8671, 24.9777, 24.0014, 24.0371, 217402080
                        string[] parts = line.Split(',').Select(x => x.Trim()).ToArray();
                        DateTimeOffset date = DateTimeOffset.ParseExact(parts[0], "MM/dd/yy", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);

                        return string.Join(",", new string[]
                        {
                            date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),    // Date
                            "-1",            // Unadjusted
                            "-1",
                            "-1",
                            "-1",
                            "-1",
                            parts[1],        // Adjusted
                            parts[2],
                            parts[3],
                            parts[4],
                            parts[5],
                            "-1",            // Dividend
                            "-1",            // Split
                        });
                    }).Where(x => !x.StartsWith(currentDay, StringComparison.OrdinalIgnoreCase)).OrderBy(x => x));
                    _____________________________________________________________________________Logger.Info($"Wsj successfully processed data for {symbol.Ticker}");
                }
                catch (Exception ex)
                {
                    _____________________________________________________________________________Logger.Error($"Wsj couldn't download data for {symbol.Ticker}", ex);
                    if (File.Exists(resultFile))
                        File.Delete(resultFile);
                }
            }, 5);

            _____________________________________________________________________________Logger.Info("All symbols from Wsj were processed");
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
            _____________________________________________________________________________Logger.Info("Wsj is about to download online data for symbols: " + String.Join(", ", symbols.Select(x => x.Ticker)));
            symbols.ParallelLoop((i, symbol) =>
            {
                try
                {
                    // Download
                    string symbolFilePath = Path.Combine(workingDirectory, "Online.html").AddVersion(symbol.Ticker);
                    try
                    {
                        DownloadOnline(symbolFilePath, symbol);
                    }
                    catch (WebException ex) when ((ex.Response as HttpWebResponse).StatusCode == HttpStatusCode.GatewayTimeout)
                    {
                        _____________________________________________________________________________Logger.Warning($"Wsj couldn't download realtime data for {symbol.Ticker} - gateway timeout");
                        return;
                    }

                    // Parse html
                    CQ dom = File.ReadAllText(symbolFilePath);

                    // Find first row
                    string rawPrice = dom.Find("#quote_val").SingleOrDefault()?.InnerText?.Trim();
                    if (rawPrice == null)
                    {
                        _____________________________________________________________________________Logger.Warning($"Wsj couldn't find price for {symbol.Ticker}");
                        return;
                    }

                    // Parse price
                    decimal price = decimal.Parse(rawPrice, CultureInfo.InvariantCulture);

                    // Add price
                    prices[symbol] = price;
                    _____________________________________________________________________________Logger.Info($"Wsj found realtime price {price} for {symbol.Ticker}");

                    // Call callback
                    onPriceFound?.Invoke(symbol, price);
                }
                catch (Exception ex)
                {
                    _____________________________________________________________________________Logger.Error($"Wsj couldn't download realtime price for {symbol.Ticker}", ex);
                }
            }, symbols.Count);
            _____________________________________________________________________________Logger.Info("Wsj download of online data ended");

            return prices;
        }




        private void DownloadHistoricalData(Symbol symbol, string file)
        {
            // Prepare url
            string url = "https://www.wsj.com/market-data/quotes/___/historical-prices/download?MOD_VIEW=page&num_rows=99999999&range_days=1&startDate=08/20/1990&endDate=01/01/2100";
            string targetUrl = url.Replace("___", symbol.Ticker.Replace(" ", "."));

            // Download
            _____________________________________________________________________________Logger.Info($"Wsj is going to download historical data from {targetUrl} to {file}");
            Utilities.DownloadFile(targetUrl, file);
            _____________________________________________________________________________Logger.Info($"Historical data were downloaded to {file}");
        }
        private void DownloadOnline(string filePath, Symbol symbol)
        {
            string url = "http://quotes.wsj.com/" + symbol.Ticker.Replace(' ', '.');
            _____________________________________________________________________________Logger.Info($"Downloading Wsj online data for {symbol.Ticker} from {url} to {filePath}");
            using (WebClient wc = new WebClient())
            {
                wc.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64; rv:52.0) Gecko/20100101 Firefox/52.0");
                wc.Headers.Add("Accept", "text/html");
                wc.Headers.Add(HttpRequestHeader.Host, "quotes.wsj.com");
                wc.Headers.Add(HttpRequestHeader.AcceptLanguage, "cs,en-US;q=0.7,en;q=0.3");

                wc.DownloadFile(new Uri(url), filePath);
            }
            _____________________________________________________________________________Logger.Info($"Wsj online data for {symbol.Ticker} were downloaded");
        }
    }
}
