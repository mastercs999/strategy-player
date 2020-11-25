using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Analyzer.TradingBase;
using Common.Loggers;
using Common.Extensions;
using System.IO;
using Common;
using System.Net;
using System.Globalization;
using System.Threading;

namespace Analyzer.Data.Sources
{
    /// <summary>
    /// Historical:
    /// Reliable source for historical data which should be sourced from iextrading.com. 
    /// - Speed - Slow
    /// - Current day - Yes
    /// - Start year - 2000
    /// - Fields - All
    /// - Limitations - Extra slow
    /// 
    /// Realtime:
    /// Delayed max dozen of seconds - that's fine. Quite fast.
    /// </summary>
    public class AlphaVantage : IHistoricalSource, IRealtimeSource
    {
        private readonly string ApiKey = "";
        private ILogger _____________________________________________________________________________Logger;




        public AlphaVantage() : this(new SilentLogger())
        {

        }
        public AlphaVantage(ILogger logger)
        {
            _____________________________________________________________________________Logger = logger;
        }




        public void DownloadHistorical(List<Symbol> symbols, string workingDirectory, Func<Symbol, string> targetFileFunc)
        {
            // Download the data
            _____________________________________________________________________________Logger.Info("AlphaVantage is about to download historical data for symbols: " + String.Join(", ", symbols.Select(x => x.Ticker)));
            foreach (Symbol symbol in symbols)
            {
                string resultFile = targetFileFunc(symbol);
                try
                {
                    // Download the file
                    string targetFile = Path.Combine(workingDirectory, symbol.Ticker + ".csv");
                    DownloadHistoricalData(symbol, targetFile);

                    // Proceed only if some data are found
                    string content = File.ReadAllText(targetFile);
                    if (content.Contains("Error Message") && content.Contains("Invalid API call"))
                    {
                        _____________________________________________________________________________Logger.Warning($"AlphaVantage wasn't successfull in downloading data for {symbol.Ticker}. It seems to be unknown ticker for this service.");
                        continue;
                    }

                    // Transform the file the main directory
                    ConvertFile(targetFile, resultFile);
                }
                catch (Exception ex)
                {
                    _____________________________________________________________________________Logger.Error($"AlphaVantage couldn't download data for {symbol.Ticker}", ex);
                    if (File.Exists(resultFile))
                        File.Delete(resultFile);
                }
            }
            _____________________________________________________________________________Logger.Info("AlphaVantage downloaded all historical data");
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
            _____________________________________________________________________________Logger.Info("Starting downloading AlphaVantage online data for following tickers: " + String.Join(", ", symbols.Select(x => x.Ticker).OrderBy(x => x)));
            symbols.Split(100).ParallelLoop((i, batchSymbols) =>
            {
                try
                {
                    // Download
                    string targetFile = Path.Combine(workingDirectory, batchSymbols.First().Ticker + ".csv");
                    DownloadOnlineData(batchSymbols, targetFile);

                    // Parse found prices
                    Dictionary<string, decimal> foundPrices = File.ReadAllLines(targetFile).Skip(1).Select(x =>
                    {
                        string[] parts = x.Split(',');
                        string ticker = parts[0].Replace('.', ' ');
                        decimal price = decimal.Parse(parts[1], CultureInfo.InvariantCulture);

                        return (ticker: ticker, price: price);
                    }).ToDictionary(x => x.ticker, x => x.price);

                    // Save them into dictionary
                    foreach (Symbol symbol in batchSymbols)
                        if (foundPrices.TryGetValue(symbol.Ticker, out decimal price))
                        {
                            // Save a price
                            prices[symbol] = price;
                            _____________________________________________________________________________Logger.Info($"AlphaVantage found price for {symbol.Ticker}: {price}");

                            // Call callback
                            onPriceFound?.Invoke(symbol, price);
                        }
                        else
                            _____________________________________________________________________________Logger.Warning($"AlphaVantage couldn't find price for {symbol.Ticker}");
                }
                catch (Exception ex)
                {
                    _____________________________________________________________________________Logger.Error("AlphaVantage couldn't download realtime prices for: " + String.Join(", ", batchSymbols.Select(x => x.Ticker)), ex);
                }
            }, 3);
            _____________________________________________________________________________Logger.Info("AlphaVantage finished downloading realtime data");

            return prices;
        }




        private void DownloadHistoricalData(Symbol symbol, string file)
        {
            // Prepare url
            string url = "https://www.alphavantage.co/query?function=TIME_SERIES_DAILY_ADJUSTED&datatype=csv&symbol=___&outputsize=full&apikey=" + ApiKey;
            string targetUrl = url.Replace("___", symbol.Ticker.Replace(" ", "."));

            // Download
            _____________________________________________________________________________Logger.Info($"AlphaVantage is going to download historical data for {symbol.Ticker} from {targetUrl} to {file}");
            Utilities.DownloadFile(targetUrl, file);
            _____________________________________________________________________________Logger.Info($"Historical data were downloaded to {file}");
        }
        private void ConvertFile(string srcFile, string dstFile)
        {
            // Create header
            File.WriteAllText(dstFile, "date,open,high,low,close,volume,adj_open,adj_high,adj_low,adj_close,adj_volume,dividend,split\n");

            // Load data
            string[] lines = File.ReadAllLines(srcFile);

            // Current day
            string currentDay = DateTimeOffset.UtcNow.UtcDateTime.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            // Append data
            File.AppendAllLines(dstFile, lines.Skip(1).Reverse().Where(x => !x.StartsWith(currentDay, StringComparison.Ordinal)).Select(line =>
            {
                // Get fields
                string[] fields = line.Split(new char[] { ',' });

                // Find ratio for price adjustment
                decimal multiplier = decimal.Parse(fields[5], CultureInfo.InvariantCulture) / decimal.Parse(fields[4], CultureInfo.InvariantCulture);
                decimal adjustedOpen = decimal.Parse(fields[1], CultureInfo.InvariantCulture) * multiplier;
                decimal adjustedHigh = decimal.Parse(fields[2], CultureInfo.InvariantCulture) * multiplier;
                decimal adjustedLow = decimal.Parse(fields[3], CultureInfo.InvariantCulture) * multiplier;
                decimal adjustedVolume = decimal.Parse(fields[6], CultureInfo.InvariantCulture) / multiplier;

                // Write them
                return string.Join(",", new string[]
                {
                    fields[0], // Date
                    fields[1], // Unadjusted
                    fields[2],
                    fields[3],
                    fields[4],
                    fields[6],
                    adjustedOpen.ToString(CultureInfo.InvariantCulture),       // Adjusted
                    adjustedHigh.ToString(CultureInfo.InvariantCulture),
                    adjustedLow.ToString(CultureInfo.InvariantCulture),
                    fields[5],
                    adjustedVolume.ToString(CultureInfo.InvariantCulture),
                    fields[7], // Dividend
                    fields[8], // Split
                });
            }).OrderBy(x => x));
        }

        private void DownloadOnlineData(List<Symbol> symbols, string file)
        {
            // Create url
            string url = "https://www.alphavantage.co/query?function=BATCH_STOCK_QUOTES&datatype=csv&symbols=___&apikey=" + ApiKey;
            string targetUrl = url.Replace("___", String.Join(",", symbols.Select(x => x.Ticker.Replace(" ", "."))));

            // Download
            _____________________________________________________________________________Logger.Info($"AlphaVantage is going to download online data from {targetUrl} to {file}");
            Utilities.DownloadFile(targetUrl, file);
            _____________________________________________________________________________Logger.Info($"Online data were downloaded to {file}");
        }
    }
}
