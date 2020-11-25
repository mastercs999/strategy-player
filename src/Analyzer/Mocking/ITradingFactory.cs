using Analyzer.Mocking.Api;
using Analyzer.Mocking.Api.Models;
using Analyzer.Mocking.Api.Models.Orders;
using Analyzer.Mocking.Data;
using Analyzer.Mocking.Gateway;
using Analyzer.Mocking.Notify;
using Analyzer.Mocking.Time;
using Analyzer.TradingBase;
using Common.Loggers;
using CSharpAPISync.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Mocking
{
    public interface ITradingFactory
    {
        ILogger Logger { get; }
        IDataProvider DataProvider { get; }
        IDateTimeProvider DateTimeProvider { get; }
        INotifyProvider NotifyProvider { get; }
        IGateway Gateway { get; }

        IApiClient CreateApi();
        IMarket CreateMarketOrder(IProduct product, OrderAction action, decimal quantity);
    }
}
