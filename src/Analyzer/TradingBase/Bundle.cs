using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Analyzer.TradingBase
{
    public class Bundle
    {
        public string Ticker { get; set; }
        public string StrategyName { get; set; }
        public DateTimeOffset DateTimeOpened { get; set; }
        public DateTimeOffset DateTimeClosed { get; set; }
        public decimal AccountValueOpened { get; set; }
        public decimal OpenPrice { get; set; }
        public decimal ClosePrice { get; set; }
        public ulong Shares { get; set; }
        public decimal OpenCommission { get; set; }
        public decimal CloseCommission { get; set; }
        public decimal Profit => Shares * (ClosePrice - OpenPrice) - OpenCommission - CloseCommission;
        public decimal ProductProfitPercent => (OpenPrice * Shares) == 0 ? 0 : Profit / (OpenPrice * Shares) * 100;
        public decimal AccountProfitPercent => AccountValueOpened == 0 ? 0 : Profit / AccountValueOpened * 100;
        public decimal OpenAssetValue => OpenPrice * Shares;
        public decimal CloseAssetValue => ClosePrice * Shares;
        public decimal TotalCommission => OpenCommission + CloseCommission;

        private Bundle()
        {
        }
        public Bundle(string ticker, string strategyName, DateTimeOffset dateTimeOpened, decimal accountValueOpened, decimal openPrice, ulong shares)
        {
            Ticker = ticker;
            StrategyName = strategyName;
            DateTimeOpened = dateTimeOpened;
            AccountValueOpened = accountValueOpened;
            OpenPrice = openPrice;
            Shares = shares;
        }

        public void Exit(DateTimeOffset dateTimeClosed, decimal closePrice)
        {
            DateTimeClosed = dateTimeClosed;
            ClosePrice = closePrice;
        }

        public override string ToString()
        {
            return String.Join(Utilities.CsvSeparator,
                Ticker,
                DateTimeOpened.ToString(),
                OpenPrice,
                Shares,
                DateTimeClosed.ToString(),
                ClosePrice,
                Profit,
                ProductProfitPercent,
                AccountProfitPercent
            );
        }
    }
}
