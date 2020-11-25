using Analyzer.Data;
using Analyzer.Mocking;
using Analyzer.Mocking.Api;
using Analyzer.Mocking.Api.Models;
using Analyzer.Mocking.Data;
using Analyzer.Mocking.Notify;
using Analyzer.Mocking.Time;
using Common;
using Common.Loggers;
using Common.Extensions;
using CSharpAPISync.Support;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Analyzer.Exceptions;

namespace Analyzer.TradingBase
{
    public abstract class TradingSystem
    {
        private SharedConfig Config;

        public string Name => Config.Name;




        protected TradingSystem(SharedConfig config)
        {
            Config = config;
        }




        public abstract void Backtest();

        protected void AnalyzeTrades(List<Bundle> trades)
        {
            PerformanceReport report = new PerformanceReport(trades, Config.Capital);

            // Print and export
            report.PrintStats();
            report.ExportToExcel(Config.ResultFile);
        }

        public abstract StrategyStateBase CreateState();
        public abstract void CalculateIndicators(Table table, ILogger logger);
        public abstract decimal ExitPositions(State state, List<IPosition> positions, ITradingFactory tradingFactory, IApiClient apiClient, StockBar[] latestBars, Table table, IAccount account, out List<IOrder> closeOrders, out List<Bundle> closedTrades, ILogger _____________________________________________________________________________logger);
        public abstract void OpenPositions(State state, List<IPosition> positions, ITradingFactory tradingFactory, IApiClient apiClient, StockBar[] latestBars, Table table, IAccount account, decimal assignedAccountSize, decimal positionsValue,  out List<IOrder> openOrders, out List<Bundle> openedTrades, ILogger _____________________________________________________________________________logger);
    }
}
