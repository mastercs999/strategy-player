using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Analyzer.TradingBase;
using Common.Loggers;
using System.IO;
using Common;
using CsQuery;

namespace Analyzer.Data.Sources
{
    /// <summary>
    /// Symbols:
    /// This source of S&P100 seems to be OK. One http request is enough.
    /// </summary>
    public class Wikipedia : ISymbolSource
    {
        private ILogger _____________________________________________________________________________Logger;




        public Wikipedia() : this(new SilentLogger())
        {

        }
        public Wikipedia(ILogger logger)
        {
            _____________________________________________________________________________Logger = logger;
        }



        
        public List<Symbol> DownloadSymbols(string workingDirectory, bool useCache)
        {
            // Target file path
            string filepath = Path.Combine(workingDirectory, "Symbols.html");
            Directory.CreateDirectory(workingDirectory);

            // Download page
            if (!useCache || !File.Exists(filepath))
            {
                string url = "https://en.wikipedia.org/wiki/S%26P_100";
                _____________________________________________________________________________Logger.Info($"Downloading Wikipedia symbol source html to {filepath} from {url}");
                Utilities.DownloadFileAkaBrowser(url, filepath);
                _____________________________________________________________________________Logger.Info("Source file downloaded");
            }

            // Parse html
            CQ dom = File.ReadAllText(filepath);

            // Find table
            CQ table = dom.Find("table.wikitable tr th:first-child").Single(x => x.InnerText.Trim() == "Symbol").Cq().Closest("table");

            // Parse symbols
            List<Symbol> parsedSymbols = table.Find("tbody tr td:first-child").Select(x => new Symbol()
            {
                Ticker = x.InnerText.Trim().Replace(".", " ")
            }).ToList();
            _____________________________________________________________________________Logger.Info($"Wikipeda contained following symbols: " + String.Join(", ", parsedSymbols.Select(x => x.Ticker).OrderBy(x => x)));

            return parsedSymbols;
        }
    }
}
