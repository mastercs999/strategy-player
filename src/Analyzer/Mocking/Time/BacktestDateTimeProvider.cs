using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Mocking.Time
{
    public class BacktestDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset Now { get; private set; }
        public DateTimeOffset Today => Now.UtcDateTime.Date;

        public BacktestDateTimeProvider(DateTimeOffset currentDateTime)
        {
            Now = currentDateTime;
        }

        public void SleepUntil(DateTimeOffset target)
        {
            if (Now < target)
                Now = target;
        }

        public void SleepFor(int milliseconds)
        {
            Now = Now.AddMilliseconds(milliseconds);
        }
    }
}
