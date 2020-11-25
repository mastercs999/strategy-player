using Common;
using Common.Loggers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Mocking.Notify
{
    public interface INotifyProvider
    {
        void SendSms(string message);
        void SendMail(string subject, string text);
    }
}
