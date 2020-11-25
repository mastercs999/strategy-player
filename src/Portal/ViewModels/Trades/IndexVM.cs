using Analyzer.Data.Sources;
using Analyzer.Strategies.Ninety;
using Analyzer.TradingBase;
using Common.Loggers;
using Common.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Analyzer.Data;

namespace Portal.ViewModels.Trades
{
    public class IndexVM
    {
        public List<ScaleBundle> OpenedTrades { get; set; }
        public List<ScaleBundle> ClosedTrades { get; set; }

        public IndexVM(string stateFile, string historicTradesFile, string workingDirectory)
        {
            OpenedTrades = !File.Exists(stateFile) ? new List<ScaleBundle>() : ToScaledBundlesOpen(JsonConvert.DeserializeObject<State>(File.ReadAllText(stateFile)).AllBundles).ToList();
            ClosedTrades = !File.Exists(historicTradesFile) ? new List<ScaleBundle>() : ToScaledBundlesClosed(JsonConvert.DeserializeObject<List<Bundle>>(File.ReadAllText(historicTradesFile))).ToList();

            // Download online prices for opened trades
            List<Symbol> symbols = OpenedTrades.Select(x => x.Ticker).Distinct().Select(x => new Symbol() { Ticker = x }).ToList();
            Dictionary<Symbol, decimal?> prices = new DownloadManager(workingDirectory, new FileLogger(Path.Combine(workingDirectory, "Log"))).DownloadRealtime(symbols);

            // Virtually exit positions
            foreach (KeyValuePair<Symbol, decimal?> kvp in prices.Where(x => x.Value.HasValue))
                foreach (ScaleBundle bundle in OpenedTrades.Where(x => x.Ticker == kvp.Key.Ticker))
                    bundle.Exit(DateTimeOffset.UtcNow.Date, kvp.Value.Value);
        }

        private IEnumerable<ScaleBundle> ToScaledBundlesOpen(IEnumerable<Bundle> bundles)
        {
            return bundles
                .GroupBy(x => (
                    ticker: x.Ticker,
                    strategyName: x.StrategyName,
                    dateTimeOpened: x.DateTimeOpened,
                    accountValueOpened: x.AccountValueOpened,
                    openPrice: x.OpenPrice))
                .Select(x => new ScaleBundle(x.Key.ticker, x.Key.strategyName, x.Key.dateTimeOpened, x.Key.accountValueOpened, x.Key.openPrice, x.Sum(y => y.Shares), x.Count(), x.Sum(y => y.OpenCommission), x.Sum(y => y.CloseCommission)));
        }
        private IEnumerable<ScaleBundle> ToScaledBundlesClosed(IEnumerable<Bundle> bundles)
        {
            return bundles
                .GroupBy(x => (
                    ticker: x.Ticker,
                    strategyName: x.StrategyName,
                    dateTimeOpened: x.DateTimeOpened,
                    dateTimeClosed: x.DateTimeClosed,
                    accountValueOpened: x.AccountValueOpened, 
                    openPrice: x.OpenPrice,
                    closePrice: x.ClosePrice))
                .Select(x =>
                {
                    ScaleBundle scaledBundle = new ScaleBundle(x.Key.ticker, x.Key.strategyName, x.Key.dateTimeOpened, x.Key.accountValueOpened, x.Key.openPrice, x.Sum(y => y.Shares), x.Count(), x.Sum(y => y.OpenCommission), x.Sum(y => y.CloseCommission));
                    scaledBundle.Exit(x.Key.dateTimeClosed, x.Key.closePrice);

                    return scaledBundle;
                });
        }
    }
}