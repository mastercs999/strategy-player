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
using Common.Extensions;
using Common.Loggers;

namespace Analyzer.Data.Sources
{
    /// <summary>
    /// Link found somewhere on the internet.
    /// 
    /// Historical:
    /// - Speed - Fast
    /// - Current day - No
    /// - Start year - 1984
    /// - Fields - Adjusted
    /// - Limitations - Few hundreds requests per day
    /// </summary>
    public class Stooq : IHistoricalSource
    {
        private ILogger _____________________________________________________________________________Logger;




        public Stooq() : this(new SilentLogger())
        {

        }
        public Stooq(ILogger logger)
        {
            _____________________________________________________________________________Logger = logger;
        }




        public void DownloadHistorical(List<Symbol> symbols, string workingDirectory, Func<Symbol, string> targetFileFunc)
        {
            // Download the data for symbols
            _____________________________________________________________________________Logger.Info("Stooq is about to download historical data for symbols: " + String.Join(", ", symbols.Select(x => x.Ticker)));
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
                    if (lines.Length < 1 || lines[0].Contains("No data") || lines[0].Contains("Exceeded the daily hits limit"))
                    {
                        _____________________________________________________________________________Logger.Warning($"Stooq couldn't find historical data for {symbol.Ticker}");
                        return;
                    }

                    // Create header
                    File.WriteAllText(targetFileFunc(symbol), "date,open,high,low,close,volume,adj_open,adj_high,adj_low,adj_close,adj_volume,dividend,split\n");

                    // Current day
                    string currentDay = DateTimeOffset.UtcNow.UtcDateTime.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

                    // Append data
                    File.AppendAllLines(targetFileFunc(symbol), lines.Skip(1).Where(x => !x.StartsWith(currentDay, StringComparison.Ordinal)).Select(item =>
                    {
                        // 1984-12-31,0.46487,0.46742,0.46487,0.46487,57940741
                        string[] parts = item.Split(',');

                        return string.Join(",", new string[]
                        {
                            parts[0],    // Date
                            "-1",            // Unadjusted
                            "-1",
                            "-1",
                            "-1",
                            "-1",
                            parts[1],        // Adjusted
                            parts[2],
                            parts[3],
                            parts[4],
                            parts.Length > 5 ? parts[5] : "-1",
                            "-1",            // Dividend
                            "-1",            // Split
                        });
                    }).OrderBy(x => x));
                    _____________________________________________________________________________Logger.Info($"Stooq successfully processed data for {symbol.Ticker}");
                }
                catch (Exception ex)
                {
                    _____________________________________________________________________________Logger.Error($"Stooq couldn't download data for {symbol.Ticker}", ex);
                    if (File.Exists(resultFile))
                        File.Delete(resultFile);
                }
            }, symbols.Count);

            _____________________________________________________________________________Logger.Info("All symbols from Stooq were processed");
        }




        private void DownloadHistoricalData(Symbol symbol, string file)
        {
            // Prepare url
            string url = "https://stooq.com/q/d/l/?s=___.us&i=d";
            string targetUrl = url.Replace("___", symbol.Ticker.Replace(' ', '-'));

            // Download
            _____________________________________________________________________________Logger.Info($"Stooq is going to download historical data from {targetUrl} to {file}");
            Utilities.DownloadFile(targetUrl, file);
            _____________________________________________________________________________Logger.Info($"Historical data were downloaded to {file}");
        }
    }
}
