using Analyzer.TradingBase;
using Common;
using Common.Loggers;
using CsQuery;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Data.Sources
{
    /// <summary>
    /// Symbols:
    /// It treats BRK.B as BRKB therefore it's unusable.
    /// </summary>
    public class SharesI : ISymbolSource
    {
        private ILogger _____________________________________________________________________________Logger;




        public SharesI() : this(new SilentLogger())
        {

        }
        public SharesI(ILogger logger)
        {
            _____________________________________________________________________________Logger = logger;
        }




        public List<Symbol> DownloadSymbols(string workingDirectory, bool useCache)
        {
            // Prepare directory
            string filepath = Path.Combine(workingDirectory, "Symbols.json");
            Directory.CreateDirectory(workingDirectory);

            // Download page
            if (!useCache || !File.Exists(filepath))
            {
                string url = "https://www.ishares.com/us/products/239723/ishares-sp-100-etf/1467271812596.ajax?tab=all&fileType=json";
                _____________________________________________________________________________Logger.Info($"Downloading iShares symbol source html to {filepath} from {url}");
                Utilities.DownloadFileAkaBrowser(url, filepath);
                _____________________________________________________________________________Logger.Info("Source file downloaded");
            }

            // Parse json
            JObject dom =  JObject.Parse(File.ReadAllText(filepath));

            // Parse symbols
            List<Symbol> parsedSymbols = (dom["aaData"] as JArray).Where(x => (string)x[2] == "Equity").Select(x => new Symbol()
            {
                Ticker = (string)x[0],
            }).ToList();
            _____________________________________________________________________________Logger.Info($"iShares contained following symbols: " + String.Join(", ", parsedSymbols.Select(x => x.Ticker).OrderBy(x => x)));

            return parsedSymbols;
        }
    }
}
