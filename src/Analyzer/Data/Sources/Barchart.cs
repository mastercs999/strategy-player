using Analyzer.TradingBase;
using Common.Loggers;
using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Globalization;
using Newtonsoft.Json.Linq;
using System.IO;
using WebSocketSharp;
using System.Threading;
using Common.Extensions;
using Microsoft.VisualBasic.FileIO;
using Analyzer.Exceptions;

namespace Analyzer.Data.Sources
{
    /// <summary>
    /// Data were obtained by reverse engineering of https://www.barchart.com/stocks/indices/sp/sp100#/viewName=main&page=all
    /// 
    /// Symbols:
    /// Perfect source of S&P100 symbols in one http request.
    /// 
    /// Historical:
    /// Link to historical data was obtained somewhere on the internet: https://www.barchart.com/proxies/timeseries/queryeod.ashx?data=daily&maxrecords=999999&volume=contract&order=asc&dividends=false&backAdjust=false&symbol=AAPL
    /// - Speed - Fast
    /// - Current day - Yes
    /// - Start year - 1984
    /// - Fields - Unadjusted, Adjusted
    /// - Limitations - None
    /// 
    /// Realtime:
    /// Downloaded using websocket technology. The download takes max 10 seconds - better to be used as alternative.
    /// Provides accurate data.
    /// </summary>
    public class Barchart : ISymbolSource, IHistoricalSource, IRealtimeSource
    {
        private ILogger _____________________________________________________________________________Logger;




        public Barchart() : this(new SilentLogger())
        {

        }
        public Barchart(ILogger logger)
        {
            _____________________________________________________________________________Logger = logger;
        }





        public List<Symbol> DownloadSymbols(string workingDirectory, bool useCache)
        {
            // Prepare filepath
            string filepath = Path.Combine(workingDirectory, "Online.json");
            Directory.CreateDirectory(workingDirectory);

            // Download symbols from barchart
            _____________________________________________________________________________Logger.Info("Getting symbol list from Barchart...");
            List<BarchartSymbol> barchartSymbols = DownloadSP100(filepath, useCache);
            _____________________________________________________________________________Logger.Info("We have received following symbols from Barchart: " + String.Join(", ", barchartSymbols.Select(x => x.Ticker).OrderBy(x => x)));

            return barchartSymbols.Cast<Symbol>().ToList();
        }
        public void DownloadHistorical(List<Symbol> symbols, string workingDirectory, Func<Symbol, string> targetFileFunc)
        {
            // Download the data
            ServicePointManager.DefaultConnectionLimit = Math.Max(symbols.Count * 10, ServicePointManager.DefaultConnectionLimit);
            _____________________________________________________________________________Logger.Info("Barchart is about to download historical data for symbols: " + String.Join(", ", symbols.Select(x => x.Ticker)));
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
                        _____________________________________________________________________________Logger.Warning($"Barchart couldn't find historical data for {symbol.Ticker}");
                        return;
                    }

                    // Transform the file the main directory
                    ConvertFile(targetPureFile, targetAdjustedFile, resultFile);
                    _____________________________________________________________________________Logger.Info($"Barchart successfully processed data for {symbol.Ticker}");
                }
                catch (Exception ex)
                {
                    _____________________________________________________________________________Logger.Error($"Barchart couldn't download data for {symbol.Ticker}", ex);
                    if (File.Exists(resultFile))
                        File.Delete(resultFile);
                }
            }, symbols.Count);
            _____________________________________________________________________________Logger.Info("Barchart downloaded all historical data");
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

            // Fill the data
            bool socketStarted = false;
            int maxWaitTime = 10 * 1000;
            int timeout = 500;
            List<BarchartSymbol> minuteData = null;
            _____________________________________________________________________________Logger.Info("Starting websocket for Barchart for symbols " + String.Join(", ", symbols.Select(x => x.Ticker).OrderBy(x => x)));
            using (WebSocket webSocket = new WebSocket("wss://jerq-aggregator-prod.aws.barchart.com/socket.io/?EIO=3&transport=websocket"))
            {
                int state = 0;

                webSocket.OnMessage += (sender, e) =>
                {
                    try
                    {
                        _____________________________________________________________________________Logger.Info(e.Data);

                        if (state == 0 && e.Data.StartsWith("0{\"sid\"", StringComparison.Ordinal))
                            ;
                        else if (state == 0 && e.Data == "40")
                        {
                            state = 1;
                            webSocket.Send("42[\"request/exchangeStatus\",{\"requestId\":\"d7efde10-d860-4adf-9185-0a3c6b4e6a30\",\"request\":{\"exchange\":\"NYSE\"}}]");
                        }
                        else if (state == 1)
                        {
                            state = 2;
                            webSocket.Send("42[\"subscribe/exchanges\",{\"codes\":[\"NYSE\"]}]");
                            webSocket.Send("42[\"subscribe/exchanges\",{\"codes\":[\"BATS\"]}]");
                            webSocket.Send("42[\"subscribe/symbols\",{\"subscribeToPrices\":true,\"symbols\":[" + String.Join(",", symbols.Select(x => "\"" + x.Ticker.Replace(" ", ".") + ".BZ\"")) + "]}]");
                        }
                        else
                        {
                            // Hey, it's started
                            socketStarted = true;

                            // Parse json
                            JArray json = JArray.Parse(e.Data.Substring(2));
                            if ((string)json[0] != "quote/snapshot")
                                return;

                            // Parse price
                            string symbol = ((string)json[1]["symbol"]).Replace(".", " ");
                            decimal price = (decimal)json[1]["lastPrice"];

                            // Remove BZ suffix
                            if (symbol.EndsWith(" BZ", StringComparison.Ordinal))
                                symbol = symbol.Substring(0, symbol.Length - 3);

                            // Set to dictionary
                            prices[tickerToSymbol[symbol]] = price;
                            _____________________________________________________________________________Logger.Info($"Received: {symbol} with price {price}");

                            // Call callback
                            onPriceFound?.Invoke(tickerToSymbol[symbol], price);
                        }
                    }
                    catch (Exception ex)
                    {
                        _____________________________________________________________________________Logger.Error($"Barchart websocket failed", ex);
                        webSocket.Close();
                    }
                };

                // Connect
                webSocket.Connect();

                // Download current prices
                string filepath = Path.Combine(workingDirectory, "Online.json");
                minuteData = DownloadGivenSymbols(filepath, symbols);

                // Wait till all symbols have a price
                bool downloaded = false;
                while (prices.Any(x => !x.Value.HasValue))
                {
                    // Download current prices again - to have latest data
                    if (socketStarted && !downloaded)
                    {
                        minuteData = DownloadGivenSymbols(filepath, symbols);
                        downloaded = true;
                    }

                    Thread.Sleep(timeout);
                    maxWaitTime -= timeout;
                    if (maxWaitTime < 0)
                        break;
                }
            }
            _____________________________________________________________________________Logger.Info("Listenning to Barchart websocket ended. The rest of waiting time is " + maxWaitTime);

            // Now fill missing prices with minute data
            List<Symbol> unsetSymbols = prices.Where(x => !x.Value.HasValue).Select(x => x.Key).ToList();
            foreach (Symbol symbol in unsetSymbols)
                prices[symbol] = minuteData.SingleOrDefault(x => x.Ticker == symbol.Ticker)?.Price;

            return prices;
        }




        private List<BarchartSymbol> DownloadSP100(string file, bool useCache)
        {
            // Download
            if (!useCache || !File.Exists(file))
                using (AdvancedWebClient wc = new AdvancedWebClient())
                {
                    // Download basic html and get cookies
                    string url = "https://www.barchart.com/stocks/indices/sp/sp100?viewName=main&page=all";
                    wc.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (Windows NT 10.0; WOW64; rv:52.0) Gecko/20100101 Firefox/52.0";
                    wc.Headers[HttpRequestHeader.AcceptEncoding] = "identity";
                    wc.Headers[HttpRequestHeader.Host] = "www.barchart.com";
                    wc.DownloadString(url);

                    url = "https://www.barchart.com/proxies/core-api/v1/quotes/get?list=stocks.markets.sp100&fields=symbol,symbolName,lastPrice,priceChange,percentChange,highPrice,lowPrice,volume,tradeTime,symbolCode,hasOptions,symbolType&orderBy=&orderDir=desc&meta=field.shortName,field.type,field.description&hasOptions=true&raw=1";
                    wc.Headers[HttpRequestHeader.Accept] = "application/json";
                    wc.Headers[HttpRequestHeader.Referer] = "https://www.barchart.com/stocks/indices/sp/sp100?viewName=main&page=all";
                    CookieCollection cookies = wc.CookieContainer.GetCookies(new Uri("https://www.barchart.com"));
                    string xsrf = cookies["XSRF-TOKEN"].Value;
                    if (xsrf.Contains("%"))
                        xsrf = xsrf.Substring(0, xsrf.IndexOf('%'));
                    wc.Headers["X-XSRF-TOKEN"] = xsrf;
                    _____________________________________________________________________________Logger.Info($"Downloading Barchart data to {file} from {url}");
                    wc.DownloadFile(url, file);
                    _____________________________________________________________________________Logger.Info("Data downloaded");
                }

            return ParseSymbols(file);
        }
        private List<BarchartSymbol> DownloadGivenSymbols(string file, List<Symbol> symbols)
        {
            using (AdvancedWebClient wc = new AdvancedWebClient())
            {
                // Download basic html and get cookies
                string url = "https://www.barchart.com/stocks/indices/sp/sp100?viewName=main&page=all";
                wc.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (Windows NT 10.0; WOW64; rv:52.0) Gecko/20100101 Firefox/52.0";
                wc.Headers[HttpRequestHeader.AcceptEncoding] = "identity";
                wc.Headers[HttpRequestHeader.Host] = "www.barchart.com";
                wc.DownloadString(url);

                url = @"https://www.barchart.com/proxies/core-api/v1/quotes/get?method=quotes&fields=symbol,symbolName,lastPrice,priceChange,percentChange,highPrice,lowPrice,volume,tradeTime,symbolCode,hasOptions,symbolType&symbols=___&description=[object Object]";
                url = url.Replace("___", String.Join(",", symbols.Select(x => x.Ticker.Replace(" ", "."))));
                wc.Headers[HttpRequestHeader.Accept] = "application/json";
                wc.Headers[HttpRequestHeader.Referer] = "https://www.barchart.com/stocks/indices/sp/sp100?viewName=main&page=all";
                wc.Headers[HttpRequestHeader.Te] = "Trailers";
                CookieCollection cookies = wc.CookieContainer.GetCookies(new Uri("https://www.barchart.com"));
                string xsrf = cookies["XSRF-TOKEN"].Value;
                if (xsrf.Contains("%"))
                    xsrf = xsrf.Substring(0, xsrf.IndexOf('%'));
                wc.Headers["X-XSRF-TOKEN"] = xsrf;
                _____________________________________________________________________________Logger.Info($"Downloading Barchart data to {file} from {url}");
                wc.DownloadFile(url, file);
                _____________________________________________________________________________Logger.Info("Data downloaded");
            }

            return ParseSymbols(file);
        }
        private List<BarchartSymbol> ParseSymbols(string file)
        {
            // Parse symbols
            return JObject.Parse(File.ReadAllText(file))["data"].Where(x => x["symbolCode"].ToString() == "STK").Select(x => new BarchartSymbol()
            {
                Ticker = x["symbol"].ToString().Replace(".", " "),
                Volume = long.Parse(x["volume"].ToString(), NumberStyles.AllowThousands | NumberStyles.AllowLeadingSign, new CultureInfo("en-US")),
                Price = decimal.Parse(x["lastPrice"].ToString(), CultureInfo.InvariantCulture)
            }).ToList();
        }

        private void DownloadHistorical(Symbol symbol, string file, bool adjusted)
        {
            using (AdvancedWebClient wc = new AdvancedWebClient())
            {
                // Download basic html and get cookies
                string url = $"https://www.barchart.com/stocks/quotes/{symbol.Ticker.Replace(" ", ".")}";
                wc.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (Windows NT 10.0; WOW64; rv:52.0) Gecko/20100101 Firefox/52.0";
                wc.Headers[HttpRequestHeader.AcceptEncoding] = "identity";
                wc.Headers[HttpRequestHeader.Host] = "www.barchart.com";
                wc.DownloadString(url);

                wc.Headers[HttpRequestHeader.Accept] = "identity";
                wc.Headers[HttpRequestHeader.Referer] = url;
                wc.Headers[HttpRequestHeader.Te] = "Trailers";
                CookieCollection cookies = wc.CookieContainer.GetCookies(new Uri("https://www.barchart.com"));
                string xsrf = cookies["XSRF-TOKEN"].Value;
                if (xsrf.Contains("%"))
                    xsrf = xsrf.Substring(0, xsrf.IndexOf('%'));
                wc.Headers["X-XSRF-TOKEN"] = xsrf;

                // Prepare url
                string basicUrl = "https://www.barchart.com/proxies/timeseries/queryeod.ashx?data=daily&maxrecords=999999&volume=contract&order=asc&dividends=false&backAdjust=|||&splits=|||&symbol=___";
                string targetUrl = basicUrl.Replace("___", symbol.Ticker.Replace(" ", ".")).Replace("|||", adjusted.ToString().ToLowerInvariant());
              
                // Download
                _____________________________________________________________________________Logger.Info($"Barchart is downloading historical data from {targetUrl} to {file}");
                wc.DownloadFile(targetUrl, file);
                _____________________________________________________________________________Logger.Info($"File {file} was downloaded");
            }

            
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
                    if (partsPure[1] == currentDay)
                        continue;

                    // Append data
                    sw.WriteLine(string.Join(",", new string[]
                    {
                        partsPure[1],       // Date
                        partsPure[2],       // Unadjusted
                        partsPure[3],
                        partsPure[4],
                        partsPure[5],
                        partsPure[6],
                        partsAdjusted[2],   // Adjusted
                        partsAdjusted[3],
                        partsAdjusted[4],
                        partsAdjusted[5],
                        partsAdjusted[6],
                        "-1",               // Dividend
                        "-1",               // Split
                    }));
                }
        }
        private Dictionary<string, string[]> ParseHistoricalFile(string filePath)
        {
            Dictionary<string, string[]> dateToLine = new Dictionary<string, string[]>();

            foreach (string line in File.ReadLines(filePath))
            {
                if (String.IsNullOrWhiteSpace(line))
                    continue;

                // Read line
                string[] parts = line.Split(',');

                // Append line
                dateToLine.Add(parts[1], parts);
            }

            return dateToLine;
        }




        private class BarchartSymbol : Symbol
        {
            public decimal Price { get; set; }
        }
    }
}
