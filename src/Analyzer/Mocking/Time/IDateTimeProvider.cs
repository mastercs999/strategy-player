using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Mocking.Time
{
    public interface IDateTimeProvider
    {
        DateTimeOffset Now { get; }
        DateTimeOffset Today { get; }

        void SleepUntil(DateTimeOffset target);
        void SleepFor(int milliseconds);
    }
}
