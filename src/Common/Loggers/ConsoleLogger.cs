using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Loggers
{
    public class ConsoleLogger : Logger
    {
        public override void FlushMessage(string message)
        {
            Console.Write(message);
        }
    }
}
