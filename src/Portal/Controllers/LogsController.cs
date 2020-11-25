using Common;
using Common.Extensions;
using Newtonsoft.Json;
using Portal.Controllers.Base;
using Portal.ViewModels.Logs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Portal.Controllers
{
    public partial class LogsController : BaseController
    {
        private string LogDirectory => Path.Combine(SourceFolder, "Log");

        public virtual ActionResult Index()
        {
            IndexVM model = new IndexVM(LogDirectory);

            return View(model);
        }

        public virtual ActionResult Filters(IndexVM vm)
        {
            FiltersVM model = new FiltersVM(vm.FilePath);

            return PartialView(model);
        }

        public virtual ActionResult LogContent(FiltersVM vm)
        {
            // Parse tree filter
            List<TreeFilterItem> treeFilter = JsonConvert.DeserializeObject<List<TreeFilterItem>>(vm.TreeFilter);
            foreach (TreeFilterItem item in treeFilter)
            {
                item.SourceClass = HttpUtility.HtmlDecode(item.SourceClass);
                item.SourceMethod = HttpUtility.HtmlDecode(item.SourceMethod);
            }

            // Parse log levels
            HashSet<LogLevel> logLevels = vm.LogLevelToIsChecked.Where(x => x.Value).Select(x => x.Key.ToEnum<LogLevel>()).ToHashSet();

            // Create model
            LogContentVM model = new LogContentVM(vm.FilePath, treeFilter, vm.TextFilter, logLevels);

            return PartialView(model);
        }
    }
}