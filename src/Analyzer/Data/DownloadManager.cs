using Analyzer.Data.Sources;
using Analyzer.Exceptions;
using Analyzer.TradingBase;
using Common;
using Common.Extensions;
using Common.Loggers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Analyzer.Data
{
    public class DownloadManager
    {
        private readonly string SymbolListDataPath;
        public string HistoricalDataPath { get; private set; }
        private readonly string RealtimeDataPath;
        private readonly ILogger _____________________________________________________________________________Logger;
        private readonly ISymbolSource[] DefaultSymbolSources;
        private readonly IHistoricalSource[] DefaultHistoricalSources;
        private readonly IRealtimeSource[] DefaultRealtimeSources;

        public DownloadManager(string dataDirectory) : this(dataDirectory, new SilentLogger())
        {
        }
        public DownloadManager(string dataDirectory, ILogger logger)
        {
            SymbolListDataPath = Path.Combine(dataDirectory, "Symbols");
            HistoricalDataPath = Path.Combine(dataDirectory, "Historical");
            RealtimeDataPath = Path.Combine(dataDirectory, "Realtime");
            _____________________________________________________________________________Logger = logger;

            // Create default sources
            DefaultSymbolSources = new ISymbolSource[]
            {
                new Barchart(logger),
                new Wikipedia(logger)
            };
            DefaultHistoricalSources = new IHistoricalSource[]
            {
                new Wsj(logger),
                new Barchart(logger),
                new Yahoo(logger),
                new Stooq(logger),
                new AlphaVantage(logger),
                new Nasdaq(logger),
                new BarchartApi(logger)
            };
            DefaultRealtimeSources = new IRealtimeSource[] 
            {
                new Xignite(logger),
                new Freestockcharts(logger),
                new Google(logger),
                new Nasdaq(logger),
                new Barchart(logger),
                new Wsj(logger),
                new IexTrading(logger),
                new AlphaVantage(logger),
            };

            // Support new TLS
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
        }




        public List<Symbol> DownloadSymbols(bool useCache, params ISymbolSource[] symbolSources)
        {
            // Create symbol sources if not given
            if (!symbolSources.Any())
                symbolSources = DefaultSymbolSources;

            // Delete directory with all the data
            if (!useCache && Directory.Exists(SymbolListDataPath))
                Directory.Delete(SymbolListDataPath, true);

            // Download symbols
            List<Symbol> allSymbols = new List<Symbol>(90);
            foreach (ISymbolSource symbolSource in symbolSources)
            {
                try
                {
                    // Create its own working directory
                    string symbolSourceDirectory = Path.Combine(SymbolListDataPath, symbolSource.GetType().Name);
                    Directory.CreateDirectory(symbolSourceDirectory);

                    // Download the symbols
                    List<Symbol> symbols = symbolSource.DownloadSymbols(symbolSourceDirectory, useCache);

                    // Remove symbols which were not download by this source - we're doing intersection
                    if (!allSymbols.Any())
                        allSymbols = symbols;
                    else
                    {
                        HashSet<string> currentSymbols = symbols.Select(x => x.Ticker).ToHashSet();
                        allSymbols.RemoveAll(x => !currentSymbols.Contains(x.Ticker));
                    }
                }
                catch (Exception ex)
                {
                    // Switch to saved exception
                    if (ex is ThreadInterruptedException)
                        ex = ThreadMessage.ThrownException;

                    _____________________________________________________________________________Logger.Error($"Symbol provider {symbolSource.GetType().Name} failed", ex);
                }
            }

            // Check whether we have all
            if (allSymbols.Count < 50)
                throw new DataException($"S&P 100 has suspiciously low number of symbols: " + String.Join(", ", allSymbols.Select(x => x.Ticker)));

            return allSymbols;
        }

        public void DownloadHistorical(List<Symbol> symbols, bool useCache, params IHistoricalSource[] historicalSources)
        {
            // Create historical sources if not given
            if (!historicalSources.Any())
                historicalSources = DefaultHistoricalSources;

            // Delete directory with all the data
            if (!useCache && Directory.Exists(HistoricalDataPath))
                Directory.Delete(HistoricalDataPath, true);

            // Download data for the symbols
            List<Symbol> remainingSymbols = new List<Symbol>(symbols);
            foreach (IHistoricalSource historicalSource in historicalSources)
            {
                try
                {
                    // Create its own working directory
                    string workingDirectory = Path.Combine(HistoricalDataPath, historicalSource.GetType().Name);
                    Directory.CreateDirectory(workingDirectory);

                    // Skip symbols which exists
                    HashSet<string> alreadyDownloaded = Directory.GetFiles(HistoricalDataPath).Select(x => Path.GetFileNameWithoutExtension(x)).ToHashSet();
                    remainingSymbols.RemoveAll(x => alreadyDownloaded.Contains(x.Ticker));
                    if (!remainingSymbols.Any())
                        break;

                    // Download the file
                    historicalSource.DownloadHistorical(remainingSymbols, workingDirectory, x => Path.Combine(HistoricalDataPath, x.Ticker + ".csv"));
                }
                catch (Exception ex)
                {
                    // Switch to saved exception
                    if (ex is ThreadInterruptedException)
                        ex = ThreadMessage.ThrownException;

                    _____________________________________________________________________________Logger.Error($"Historical provider {historicalSource.GetType().Name} failed", ex);
                }
            }
        }

        public Dictionary<Symbol, decimal?> DownloadRealtime(List<Symbol> symbols, params IRealtimeSource[] realtimeSources)
        {
            // Create realtime sources if not given
            if (!realtimeSources.Any())
                realtimeSources = DefaultRealtimeSources;

            // Delete working directory
            if (Directory.Exists(RealtimeDataPath))
                Directory.Delete(RealtimeDataPath, true);

            // Download data for the symbols
            Dictionary<Symbol, decimal?> allPrices = symbols.ToDictionary(x => x, x => (decimal?)null);
            foreach (IRealtimeSource realtimeSource in realtimeSources)
            {
                try
                {
                    // Prepare working directory
                    string workingDirectory = Path.Combine(RealtimeDataPath, realtimeSource.GetType().Name);
                    Directory.CreateDirectory(workingDirectory);

                    // Skip symbols for which we already have the data
                    List<Symbol> toDownload = allPrices.Where(x => !x.Value.HasValue).Select(x => x.Key).ToList();
                    if (!toDownload.Any())
                        break;

                    // Download the prices
                    Dictionary<Symbol, decimal?> prices = realtimeSource.DownloadRealtime(toDownload, workingDirectory);

                    // Fill found prices
                    foreach (KeyValuePair<Symbol, decimal?> kvp in prices.Where(x => x.Value.HasValue))
                        allPrices[kvp.Key] = kvp.Value;
                }
                catch (Exception ex)
                {
                    // Switch to saved exception
                    if (ex is ThreadInterruptedException)
                        ex = ThreadMessage.ThrownException;

                    _____________________________________________________________________________Logger.Error($"Realtime provider {realtimeSource.GetType().Name} failed", ex);
                }
            }

            return allPrices;
        }
        public Dictionary<Symbol, decimal?> DownloadRealtimeParallel(List<Symbol> symbols, params IRealtimeSource[] realtimeSources)
        {
            return DownloadRealtimeParallel(symbols, 5000, realtimeSources);
        }
        public Dictionary<Symbol, decimal?> DownloadRealtimeParallel(List<Symbol> symbols, int fetchDelay, params IRealtimeSource[] realtimeSources)
        {
            // Create realtime sources if not given
            if (!realtimeSources.Any())
                realtimeSources = DefaultRealtimeSources;

            // Delete working directory
            if (Directory.Exists(RealtimeDataPath))
                Directory.Delete(RealtimeDataPath, true);

            // Prepare structures for downloading
            List<Symbol> symbolsToFetch = symbols;
            ConcurrentDictionary<Symbol, decimal?> allPrices = symbols.ToConcurrentDictionary(x => x, x => (decimal?)null);
            ConcurrentDictionary<Symbol, ManualResetEvent> locks = symbols.ToConcurrentDictionary(x => x, x => new ManualResetEvent(false));
            Dictionary<Symbol, int> symbolToPriority = symbols.ToDictionary(x => x, x => int.MaxValue);
            Dictionary<IRealtimeSource, int> sourceToPriority = realtimeSources.WithIndex().ToDictionary(x => x.value, x => x.index);
            ConcurrentDictionary<IRealtimeSource, bool> sourceToEnded = realtimeSources.ToConcurrentDictionary(x => x, x => false);
            List<Thread> threads = new List<Thread>();
            bool cancelRequested = false;

            // Download data for the symbols
            ServicePointManager.DefaultConnectionLimit = Math.Max(symbols.Count * 10 * realtimeSources.Length, ServicePointManager.DefaultConnectionLimit);
            _____________________________________________________________________________Logger.Info("Starting threads for downloading realtime data...");
            Thread startingThread = new Thread(() =>
            {
                foreach (IRealtimeSource rs in realtimeSources)
                {
                    IRealtimeSource realtimeSource = rs;
                    string sourceName = realtimeSource.GetType().Name;

                    if (cancelRequested)
                    {
                        _____________________________________________________________________________Logger.Info($"We're not going to fetch data from {sourceName} because end of fetching was requested.");
                        return;
                    }

                    // Fetch only needed data
                    lock (allPrices)
                        symbolsToFetch = allPrices.Where(x => !x.Value.HasValue).Select(x => x.Key).ToList();

                    Thread thread = new Thread(() =>
                    {
                        try
                        {
                            // Prepare working directory
                            string workingDirectory = Path.Combine(RealtimeDataPath, realtimeSource.GetType().Name);
                            Directory.CreateDirectory(workingDirectory);

                            // Download the prices
                            _____________________________________________________________________________Logger.Info($"Starting {sourceName} provider.");
                            realtimeSource.DownloadRealtime(symbolsToFetch, workingDirectory, (symbol, price) =>
                                {
                                    lock (allPrices)
                                    {
                                        if (!allPrices[symbol].HasValue || symbolToPriority[symbol] > sourceToPriority[realtimeSource])
                                        {
                                            allPrices[symbol] = price;
                                            symbolToPriority[symbol] = sourceToPriority[realtimeSource];
                                            locks[symbol].Set();

                                            _____________________________________________________________________________Logger.Info($"Price for {symbol.Ticker} received from {sourceName} with value {price}");
                                        }
                                    }
                                });
                            sourceToEnded[realtimeSource] = true;
                            _____________________________________________________________________________Logger.Info($"Realtime provider {sourceName} thread has ended downloading realtime data.");

                            // If all other threads ended, set all locks
                            if (sourceToEnded.Values.All(x => x))
                            {
                                _____________________________________________________________________________Logger.Info("All threads finished downloading data. Setting all locks...");
                                foreach (ManualResetEvent resetEvent in locks.Values)
                                    resetEvent.Set();
                            }
                        }
                        catch (Exception ex)
                        {
                            _____________________________________________________________________________Logger.Error($"Realtime provider {sourceName} failed", ex);
                        }
                    })
                    {
                        IsBackground = true,
                    };

                    threads.Add(thread);
                    thread.Start();

                    // Wait some time to prevent http flooding
                    Thread.Sleep(fetchDelay);
                }
            })
            {
                IsBackground = true
            };
            startingThread.Start();

            // Wait till we have all the data
            foreach (Symbol s in symbols)
            {
                locks[s].WaitOne();
                _____________________________________________________________________________Logger.Info($"We are sure we have realtime data for {s.Ticker}");
            }
            _____________________________________________________________________________Logger.Info("We have all realtime data or all threads ended.");

            // Request cancel
            cancelRequested = true;

            // Kill all running threads
            new Thread(() =>
            {
                _____________________________________________________________________________Logger.Info("Thread for killing threads prepared.");
                Thread.Sleep(60 * 1000);
                foreach (Thread t in threads.ConcatItem(startingThread))
                    if (t.IsAlive)
                        t.Interrupt();
                _____________________________________________________________________________Logger.Info("Threads cleaning finished");
            })
            {
                IsBackground = true
            }.Start();

            return new Dictionary<Symbol, decimal?>(allPrices);
        }
    }
}
