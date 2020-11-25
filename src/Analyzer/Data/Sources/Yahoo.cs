using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Analyzer.TradingBase;
using Common;
using Common.Loggers;
using System.IO;
using System.Globalization;
using Microsoft.VisualBasic.FileIO;
using System.Net;
using Common.Extensions;
using Newtonsoft.Json.Linq;

namespace Analyzer.Data.Sources
{
    /// <summary>
    /// Historical:
    /// - Speed - Fast
    /// - Current day - Yes
    /// - Start year - 1980
    /// - Fields - Adjusted
    /// - Limitations - Some sources say that data have bad quality
    /// </summary>
    public class Yahoo : IHistoricalSource
    {
        private ILogger _____________________________________________________________________________Logger;




        public Yahoo() : this(new SilentLogger())
        {

        }
        public Yahoo(ILogger logger)
        {
            _____________________________________________________________________________Logger = logger;
        }



        public void DownloadHistorical(List<Symbol> symbols, string workingDirectory, Func<Symbol, string> targetFileFunc)
        {
            // Download the data
            ServicePointManager.DefaultConnectionLimit = Math.Max(symbols.Count * 10, ServicePointManager.DefaultConnectionLimit);
            _____________________________________________________________________________Logger.Info("Yahoo is about to download historical data for symbols: " + String.Join(", ", symbols.Select(x => x.Ticker)));
            symbols.ParallelLoop((i, symbol) =>
            {
                string resultFile = targetFileFunc(symbol);
                try
                {
                    // Download the file
                    string targetFile = Path.Combine(workingDirectory, symbol.Ticker + ".json");
                    try
                    {
                        DownloadHistoricalData(symbol, targetFile);
                    }
                    catch (WebException ex) when ((ex.Response as HttpWebResponse).StatusCode == HttpStatusCode.NotFound)
                    {
                        _____________________________________________________________________________Logger.Warning($"Yahoo couldn't find historical data for {symbol.Ticker}");
                        _____________________________________________________________________________Logger.Error(ex);
                        return;
                    }

                    // Parse file
                    List<StockBar> symbolData = ParseHistoricalFile(targetFile);

                    // Create header
                    File.WriteAllText(resultFile, "date,open,high,low,close,volume,adj_open,adj_high,adj_low,adj_close,adj_volume,dividend,split\n");

                    // Current day
                    DateTimeOffset currentDay = DateTimeOffset.UtcNow.UtcDateTime.Date;

                    // Append data
                    File.AppendAllLines(resultFile, symbolData.Where(x => x.Date.Date != currentDay.Date).Select(x =>
                    {
                        return string.Join(",", new string[]
                        {
                            x.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),    // Date
                            "-1",            // Unadjusted
                            "-1",
                            "-1",
                            "-1",
                            "-1",
                            x.AdjustedOpen.ToString(CultureInfo.InvariantCulture),           // Adjusted
                            x.AdjustedHigh.ToString(CultureInfo.InvariantCulture),
                            x.AdjustedLow.ToString(CultureInfo.InvariantCulture),
                            x.AdjustedClose.ToString(CultureInfo.InvariantCulture),
                            x.AdjustedVolume.ToString(CultureInfo.InvariantCulture),
                            "-1",                                                            // Dividend
                            "-1",                                                            // Split
                        });
                    }).OrderBy(x => x));
                    _____________________________________________________________________________Logger.Info($"Yahoo successfully processed data for {symbol.Ticker}");
                }
                catch (Exception ex)
                {
                    _____________________________________________________________________________Logger.Error($"Yahoo couldn't download data for {symbol.Ticker}", ex);
                    if (File.Exists(resultFile))
                        File.Delete(resultFile);
                }
            }, 5);
            _____________________________________________________________________________Logger.Info("Yahoo downloaded all historical data");
        }




        private void DownloadHistoricalData(Symbol symbol, string file)
        {
            // Prepare url
            string url = "https://query2.finance.yahoo.com/v8/finance/chart/___?period1=3661&period2=4102448461&interval=1d";
            string targetUrl = url.Replace("___", symbol.Ticker.Replace(" ", "-"));

            // Download
            _____________________________________________________________________________Logger.Info($"Yahoo is going to download historical data from {targetUrl} to {file}");
            Utilities.DownloadFile(targetUrl, file);
            _____________________________________________________________________________Logger.Info($"Historical data were downloaded to {file}");
        }
        private List<StockBar> ParseHistoricalFile(string path)
        {
            JObject symbolData = JObject.Parse(File.ReadAllText(path));
            JObject info = symbolData["chart"]["result"][0] as JObject;
            JArray dates = info["timestamp"] as JArray;
            JObject quotes = info["indicators"]["quote"][0] as JObject;

            List<StockBar> bars = new List<StockBar>(dates.Count);
            for (int i = 0; i < dates.Count; ++i)
            {
                DateTimeOffset date = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds((int)dates[i]);

                JValue open = quotes["open"][i] as JValue;
                JValue high = quotes["high"][i] as JValue;
                JValue low = quotes["low"][i] as JValue;
                JValue close = quotes["close"][i] as JValue;
                JValue volume = quotes["volume"][i] as JValue;
                if (open.Value == null || high.Value == null || low.Value == null || close.Value == null || volume.Value == null)
                    continue;

                bars.Add(new StockBar()
                {
                    Date = date,

                    AdjustedOpen = (decimal)open,
                    AdjustedHigh = (decimal)high,
                    AdjustedLow = (decimal)low,
                    AdjustedClose = (decimal)close,
                    AdjustedVolume = (decimal)volume,
                });
            }

            return bars;
        }
    }
}
