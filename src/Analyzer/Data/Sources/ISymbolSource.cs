using Analyzer.TradingBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Data.Sources
{
    public interface ISymbolSource
    {
        List<Symbol> DownloadSymbols(string workingDirectory, bool useCache);
    }
}
