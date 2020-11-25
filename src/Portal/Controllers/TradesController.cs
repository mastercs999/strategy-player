using Analyzer.Data.Sources;
using Analyzer.TradingBase;
using Common.Loggers;
using Portal.Controllers.Base;
using Portal.ViewModels.Trades;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Portal.Controllers
{
    public partial class TradesController : BaseController
    {
        private string StateFile => Path.Combine(SourceFolder, "Trades", "State.json");
        private string HistoricTradesFile => Path.Combine(SourceFolder, "Trades", "Trades.json");

        public virtual ActionResult Index()
        {
            IndexVM model = new IndexVM(StateFile, HistoricTradesFile, WorkingDirectory);

            return View(model);
        }
    }
}