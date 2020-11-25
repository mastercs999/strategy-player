using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpAPISync.Support
{
    [Serializable]
    public class DateRange
    {
        public DateTimeOffset From { get; set; }
        public DateTimeOffset To { get; set; }

        public override string ToString() => From + " " + To;
    }
}
