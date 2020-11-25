using Analyzer.TradingBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Portal.ViewModels.Trades
{
    public class ScaleBundle : Bundle
    {
        public int Scale { get; set; }

        public ScaleBundle(string ticker, string strategyName, DateTimeOffset dateTimeOpened, decimal accountValueOpened, decimal openPrice, ulong shares, int scale, decimal openCommission, decimal closeCommission) : base(ticker, strategyName, dateTimeOpened, accountValueOpened, openPrice, shares)
        {
            Scale = scale;
            OpenCommission = openCommission;
            CloseCommission = closeCommission;
        }
    }
}