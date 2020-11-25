using Common;
using Common.Loggers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Mocking.Notify
{
    public class BacktestNotifyProvider : INotifyProvider
    {
        private ILogger _____________________________________________________________________________Logger;




        public BacktestNotifyProvider(ILogger logger)
        {
            _____________________________________________________________________________Logger = logger;
        }




        public void SendSms(string message)
        {
            _____________________________________________________________________________Logger.Info($"SMS dispatches {message}");
        }

        public void SendMail(string subject, string text)
        {
            _____________________________________________________________________________Logger.Info($"EMAIL entitles as {subject} dispatches {text}");
        }
    }
}
