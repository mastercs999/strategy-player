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
using System.Globalization;

namespace Analyzer.Data.Sources
{
    /// <summary>
    /// Historical:
    /// We download data from free EOD database. The server returns 500 Internal server error from time to time.
    /// - Speed - Slow
    /// - Current day - No
    /// - Start year - 1980
    /// - Fields - All
    /// - Limitations - Some days are missing and the data seems to be a little different than others.
    ///               - Historical data are no longer provided by Quandl - unusuable at the moment.
    /// </summary>
    public class Quandl : IHistoricalSource
    {
        private readonly string ApiKey = "";
        private ILogger _____________________________________________________________________________Logger;




        public Quandl() : this(new SilentLogger())
        {

        }
        public Quandl(ILogger logger)
        {
            _____________________________________________________________________________Logger = logger;
        }




        public void DownloadHistorical(List<Symbol> symbols, string workingDirectory, Func<Symbol, string> targetFileFunc)
        {
            // Download the data
            _____________________________________________________________________________Logger.Info("Quandl is about to download historical data for symbols: " + String.Join(", ", symbols.Select(x => x.Ticker)));
            foreach (Symbol symbol in symbols)
            {
                string resultFile = targetFileFunc(symbol);
                try
                {
                    // Target file
                    string targetFile = Path.Combine(workingDirectory, symbol.Ticker + ".csv");
                    Download(symbol, targetFile);

                    // Proceed only if some data are found
                    if (File.ReadAllLines(targetFile).Length <= 2)
                        continue;

                    // Transform the file the main directory
                    ConvertFile(targetFile, resultFile);
                }
                catch (Exception ex)
                {
                    _____________________________________________________________________________Logger.Error($"Quandl couldn't download data for {symbol.Ticker}", ex);
                    if (File.Exists(resultFile))
                        File.Delete(resultFile);
                }
            }
        }

        private void Download(Symbol symbol, string file)
        {
            // Delete the target file
            if (File.Exists(file))
                File.Delete(file);

            // Prepare url
            string url = "https://www.quandl.com/api/v3/datatables/WIKI/PRICES.csv?ticker=___&api_key=" + ApiKey;
            string targetUrl = url.Replace("___", symbol.Ticker.Replace(" ", "_"));

            // Download
            _____________________________________________________________________________Logger.Info($"Starting downloading Quandl historical data for {symbol.Ticker} from {targetUrl}");
            string cursorId = null;
            do
            {
                using (AdvancedWebClient wc = new AdvancedWebClient())
                {
                    // Temp file
                    string tmpFile = file + ".tmp";

                    // Download as a temp file
                    string targetUrlPaged = targetUrl;
                    if (cursorId != null)
                        targetUrlPaged = targetUrlPaged + "&qopts.cursor_id=" + cursorId;
                    wc.DownloadFile(new Uri(targetUrlPaged), tmpFile);

                    // Rename or append
                    if (!File.Exists(file))
                        File.Move(tmpFile, file);
                    else
                    {
                        File.AppendAllLines(file, File.ReadLines(tmpFile).Skip(1));
                        File.Delete(tmpFile);
                    }

                    // Move to the next
                    cursorId = wc.ResponseHeaders["cursor_id"];
                }
            } while (cursorId != null);
            _____________________________________________________________________________Logger.Info($"Quandl data for {symbol.Ticker} were downloaded");
        }
        private void ConvertFile(string srcFile, string dstFile)
        {
            // Create header
            File.WriteAllText(dstFile, "date,open,high,low,close,volume,adj_open,adj_high,adj_low,adj_close,adj_volume,dividend,split\n");

            // Current day
            string currentDay = DateTimeOffset.UtcNow.UtcDateTime.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            // Append data
            using (StreamWriter sw = new StreamWriter(dstFile, true))
                foreach (string line in File.ReadAllLines(srcFile).Skip(1).OrderBy(x => x))
                {
                    // Get fields
                    string[] fields = line.Split(new char[] { ',' });

                    // Skip current day
                    if (fields[1] == currentDay)
                        continue;

                    // Write them
                    sw.WriteLine(string.Join(",", new string[]
                    {
                        fields[1], // Date
                        fields[2], // Unadjusted
                        fields[3],
                        fields[4],
                        fields[5],
                        fields[6],
                        fields[9], // Adjusted
                        fields[10],
                        fields[11],
                        fields[12],
                        fields[13],
                        fields[7], // Dividend
                        fields[8], // Split
                    }));
                }
        }
    }
}
