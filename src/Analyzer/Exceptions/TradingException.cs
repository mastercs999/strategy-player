using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Exceptions
{
    [Serializable]
    public class TradingException : Exception
    {
        public TradingException()
        {
        }

        public TradingException(string message) : base(message)
        {
        }

        public TradingException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected TradingException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
