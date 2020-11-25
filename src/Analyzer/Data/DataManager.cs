using Common;
using Analyzer.TradingBase;
using CsQuery;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Common.Loggers;
using Analyzer.Data.Sources;
using Analyzer.Exceptions;

namespace Analyzer.Data
{
    public class DataManager
    {
        private DownloadManager DownloadManager;
        public string BaseDirectory { get; set; }

        public DataManager(string directory) : this(directory, new SilentLogger())
        {
        }
        public DataManager(string directory, ILogger logger)
        {
            DownloadManager = new DownloadManager(directory, logger);
            BaseDirectory = directory;
        }

        public List<Symbol> FetchSymbols()
        {
            return FetchSymbols(false);
        }
        public List<Symbol> FetchSymbols(bool useCache)
        {
            return DownloadManager.DownloadSymbols(useCache);
        }

        public Table CreateDataTable()
        {
            return CreateDataTable(false);
        }
        public Table CreateDataTable(bool useCache)
        {
            return CreateDataTable(useCache, false);
        }
        public Table CreateDataTable(bool useCache, bool fixSplits)
        {
            // Get symbols and data
            List<Symbol> symbols = FetchSymbols(useCache);
            DownloadManager.DownloadHistorical(symbols, useCache);

            // Build the table
            Table table = new Table(this, symbols, DownloadManager.HistoricalDataPath);

            // Fix splits
            if (fixSplits)
                table.FixSplits();

            return table;
        }

        public decimal[] FetchOnlineData(Symbol[] symbols)
        {
            // Download data
            Dictionary<Symbol, decimal?> prices = DownloadManager.DownloadRealtime(symbols.ToList());

            // Check whether we have all the data
            if (prices.Any(x => x.Value == null))
                throw new DataException("Unable to download online data for following tickers: " + String.Join(",", prices.Where(x => x.Value == null).Select(x => x.Key.Ticker)));

            return symbols.Select(x => prices[x].Value).ToArray();
        }
        public decimal[] FetchOnlineDataParallel(Symbol[] symbols)
        {
            // Download data
            Dictionary<Symbol, decimal?> prices = DownloadManager.DownloadRealtimeParallel(symbols.ToList());

            // Check whether we have all the data
            if (prices.Any(x => x.Value == null))
                throw new DataException("Unable to download online data for following tickers: " + String.Join(",", prices.Where(x => x.Value == null).Select(x => x.Key.Ticker)));

            return symbols.Select(x => prices[x].Value).ToArray();
        }
    }
}
