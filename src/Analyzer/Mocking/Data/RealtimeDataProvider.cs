using Analyzer.TradingBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Analyzer.Data;
using Analyzer.Mocking.Time;
using Common.Loggers;

namespace Analyzer.Mocking.Data
{
    public class RealtimeDataProvider : IDataProvider
    {
        private string DataDirectory;
        private Table Table;
        private ILogger Logger;

        public IDateTimeProvider DateTimeProvider { get; private set; }
        public bool HasData => true;




        public RealtimeDataProvider(string dataDirectory, IDateTimeProvider dateTimeProvider, ILogger logger)
        {
            DataDirectory = dataDirectory;
            DateTimeProvider = dateTimeProvider;
            Logger = logger;
        }




        public List<Symbol> GetSymbols()
        {
            return new DataManager(DataDirectory, Logger).FetchSymbols(false);
        }

        public Table GetHistory()
        {
            return Table = new DataManager(DataDirectory, Logger).CreateDataTable(false);
        }

        public StockBar[] AppendOnlineData()
        {
            Table.AppendOnlineData();

            return Table.Bars.Last();
        }

        public decimal[] DownloadOnlineData(Symbol[] symbols)
        {
            return new DataManager(DataDirectory, Logger).FetchOnlineData(symbols);
        }

        public void CalculateIndicators(IEnumerable<Action<Table>> calculateActions)
        {
            foreach (Action<Table> action in calculateActions)
                action(Table);
        }
    }
}
