using Analyzer.TradingBase;
using Common.Loggers;
using Common.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using System.Net;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json.Linq;
using System.Globalization;
using Analyzer.Exceptions;
using System.Threading;

namespace Analyzer.Data.Sources
{
    /// <summary>
    /// https://www.barchartondemand.com/free
    /// https://www.barchart.com/solutions/press-releases/1168328/free-market-data-apis-introduced-by-barchart-ondemand
    /// 
    /// Historical:
    /// - Speed - Fast
    /// - Current day - No
    /// - Start year - 2017 (just last 6 months)
    /// - Fields - Unadjusted, Adjusted
    /// - Limitations - None
    /// 
    /// Realtime:
    /// The provided data are delayed by 15 minutes - unusable.
    /// </summary>
    public class BarchartApi : IHistoricalSource, IRealtimeSource
    {
        private readonly string ApiKey = "";
        private readonly int RealtimeBatchSize = 90;
        private ILogger _____________________________________________________________________________Logger;




        public BarchartApi() : this(new SilentLogger())
        {

        }
        public BarchartApi(ILogger logger)
        {
            _____________________________________________________________________________Logger = logger;
        }




        public void DownloadHistorical(List<Symbol> symbols, string workingDirectory, Func<Symbol, string> targetFileFunc)
        {
            // Download the data
            ServicePointManager.DefaultConnectionLimit = Math.Max(symbols.Count * 10, ServicePointManager.DefaultConnectionLimit);
            _____________________________________________________________________________Logger.Info("BarchartApi is about to download historical data for symbols: " + String.Join(", ", symbols.Select(x => x.Ticker)));
            symbols.ParallelLoop((i, symbol) =>
            {
                string resultFile = targetFileFunc(symbol);
                try
                {
                    // Target file
                    string targetPureFile = Path.Combine(workingDirectory, symbol.Ticker + "_Pure.csv");
                    string targetAdjustedFile = Path.Combine(workingDirectory, symbol.Ticker + "_Adjusted.csv");
                    DownloadHistorical(symbol, targetPureFile, false);
                    DownloadHistorical(symbol, targetAdjustedFile, true);

                    // Proceed only if some data are found
                    if (File.ReadAllLines(targetPureFile).Length <= 2 || File.ReadAllLines(targetAdjustedFile).Length <= 2)
                    {
                        _____________________________________________________________________________Logger.Warning($"BarchartApi couldn't find historical data for {symbol.Ticker}");
                        return;
                    }

                    // Transform the file the main directory
                    ConvertFile(targetPureFile, targetAdjustedFile, resultFile);
                    _____________________________________________________________________________Logger.Info($"BarchartApi successfully processed data for {symbol.Ticker}");
                }
                catch (Exception ex)
                {
                    _____________________________________________________________________________Logger.Error($"BarchartApi couldn't download data for {symbol.Ticker}", ex);
                    if (File.Exists(resultFile))
                        File.Delete(resultFile);
                }
            }, symbols.Count);
            _____________________________________________________________________________Logger.Info("BarchartApi downloaded all historical data");
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

            // Download online data - we can download data for 100 symbols at once
            _____________________________________________________________________________Logger.Info("BarchartApi starts downloading realtime prices for following symbols: " + String.Join(", ", symbols.Select(x => x.Ticker).OrderBy(x => x)));
            symbols.Split(RealtimeBatchSize).ParallelLoop((i, symbolsToDownload) =>
            {
                try
                {
                    // Download data for these symbols
                    string batchFilePath = Path.Combine(workingDirectory, "Online.json").AddVersion(i);
                    DownloadOnline(symbolsToDownload, batchFilePath);

                    // Parse file and save to dictionary
                    JObject data = JObject.Parse(File.ReadAllText(batchFilePath));
                    foreach (JObject item in data["results"] as JArray)
                    {
                        // Parse value for ticker
                        string ticker = ((string)item["symbol"]).Replace(".", " ");
                        decimal price = (decimal)item["lastPrice"];

                        // Set price
                        prices[tickerToSymbol[ticker]] = price;
                        _____________________________________________________________________________Logger.Info($"BarchartApi found realtime price for {ticker}: {price}");

                        // Call callback
                        onPriceFound?.Invoke(tickerToSymbol[ticker], price);
                    }
                }
                catch (Exception ex)
                {
                    _____________________________________________________________________________Logger.Error("BarchartApi couldn't download realtime prices for: " + String.Join(", ", symbolsToDownload.Select(x => x.Ticker)), ex);
                }
            }, symbols.Count);
            _____________________________________________________________________________Logger.Info("BarchartApi has finished downloading realtime prices");

            return prices;
        }




        private void DownloadHistorical(Symbol symbol, string file, bool adjusted)
        {
            // Prepare url
            string url = "http://marketdata.websol.barchart.com/getHistory.csv?symbol=___&type=daily&startDate=19000101&interval=1&order=asc&splits=|||&dividends=false&volume=sum&key=" + ApiKey;
            string targetUrl = url.Replace("___", symbol.Ticker.Replace(" ", ".")).Replace("|||", adjusted.ToString().ToLowerInvariant());

            // Download
            _____________________________________________________________________________Logger.Info($"BarchartApi is downloading historical data from {targetUrl} to {file}");
            Utilities.DownloadFile(targetUrl, file);
            _____________________________________________________________________________Logger.Info($"File {file} was downloaded");
        }
        private void ConvertFile(string srcPureFile, string srcAdjustedFile, string dstFile)
        {
            // Parse both files
            Dictionary<string, string[]> dateToLinePure = ParseHistoricalFile(srcPureFile);
            Dictionary<string, string[]> dateToLineAdjusted = ParseHistoricalFile(srcAdjustedFile);

            // Create header
            File.WriteAllText(dstFile, "date,open,high,low,close,volume,adj_open,adj_high,adj_low,adj_close,adj_volume,dividend,split\n");

            // Check if both files contain same data
            if (dateToLinePure.Keys.Except(dateToLineAdjusted.Keys).Any() || dateToLineAdjusted.Keys.Except(dateToLinePure.Keys).Any())
                throw new DataException($"Files {srcPureFile} and {srcAdjustedFile} contain data for different days");

            // Current day
            string currentDay = DateTimeOffset.UtcNow.UtcDateTime.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            // Append data
            using (StreamWriter sw = new StreamWriter(dstFile, true))
                foreach (string date in dateToLinePure.Keys.Concat(dateToLineAdjusted.Keys).Distinct().OrderBy(x => x))
                {
                    // Get lines in both files
                    string[] partsPure = dateToLinePure[date];
                    string[] partsAdjusted = dateToLineAdjusted[date];

                    // Skip current day
                    if (partsPure[2] == currentDay)
                        continue;

                    // Append data
                    sw.WriteLine(string.Join(",", new string[]
                    {
                        partsPure[2],       // Date
                        partsPure[3],       // Unadjusted
                        partsPure[4],
                        partsPure[5],
                        partsPure[6],
                        partsPure[7],
                        partsAdjusted[3],   // Adjusted
                        partsAdjusted[4],
                        partsAdjusted[5],
                        partsAdjusted[6],
                        partsAdjusted[7],
                        "-1",               // Dividend
                        "-1",               // Split
                    }));
                }
        }
        private Dictionary<string, string[]> ParseHistoricalFile(string filePath)
        {
            Dictionary<string, string[]> dateToLine = new Dictionary<string, string[]>();

            using (TextFieldParser parser = new TextFieldParser(filePath))
            {
                // Set file properties
                parser.HasFieldsEnclosedInQuotes = true;
                parser.SetDelimiters(",");

                // Skip first line
                parser.ReadFields();

                // Go through lines
                while (!parser.EndOfData)
                {
                    // Read line
                    string[] parts = parser.ReadFields();

                    // Append line
                    dateToLine.Add(parts[2], parts);
                }
            }

            return dateToLine;
        }

        private void DownloadOnline(List<Symbol> symbols, string file)
        {
            string basicUrl = "http://marketdata.websol.barchart.com/getQuote.json?symbols=___&mode=R&key=" + ApiKey;
            string url = basicUrl.Replace("___", String.Join(",", symbols.Select(x => x.Ticker.Replace(" ", "."))));

            _____________________________________________________________________________Logger.Info($"BarchartApi is going to download online data to {file} from {url}");
            Utilities.DownloadFile(url, file);
            _____________________________________________________________________________Logger.Info($"File {file} was download");
        }
    }
}
