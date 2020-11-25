using Analyzer.Strategies.FBLL;
using Analyzer.Strategies.Ninety;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.TradingBase.Composition
{
    public class CompositeConfig : SharedConfig
    {
        public string AccountName { get; set; }
        public int ClientId { get; set; }
        public int Port { get; set; }
        public int StartBeforeEnd { get; set; }
        public int MainAttemptBeforeEnd { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string LogDirectory => Path.Combine(BaseDirectory, "Log");
        public string TradesDirectory => Path.Combine(BaseDirectory, "Trades");
        public string StateFile => Path.Combine(TradesDirectory, "State.json");
        public string TradeHistoryFile => Path.Combine(TradesDirectory, "Trades.json");

        public string GatewayLogin { get; set; }
        public string GatewayPassword { get; set; }
        public string GatewayVersion { get; set; }
        public bool LiveTrading { get; set; }
    }
}
