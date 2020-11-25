using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Analyzer.Mocking.Time
{
    public class RealtimeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset Now { get => DateTimeOffset.UtcNow; }
        public DateTimeOffset Today => Now.UtcDateTime.Date;

        public void SleepUntil(DateTimeOffset target)
        {
            DateTimeOffset now = Now;
            if (now < target)
                Thread.Sleep(target - now);
        }

        public void SleepFor(int milliseconds)
        {
            Thread.Sleep(milliseconds);
        }
    }
}
