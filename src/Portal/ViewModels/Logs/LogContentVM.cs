using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Common;
using Common.Extensions;

namespace Portal.ViewModels.Logs
{
    public class LogContentVM
    {
        public List<Record> Records { get; set; }

        public LogContentVM(string filePath, List<TreeFilterItem> treeFilter, string textFilter, HashSet<LogLevel> logLevels)
        {
            // Parse file file
            Records = LogParser.Load(filePath).ToList();

            // Filter
            HashSet<(string sourceClass, string sourceMethod)> selectedFilters = new HashSet<(string sourceClass, string sourceMethod)>(treeFilter.Select(x => (x.SourceClass, x.SourceMethod)));
            Records = Records.Where(x => selectedFilters.Contains((x.SourceClass, x.SourceMethod))).ToList();
            if (!String.IsNullOrWhiteSpace(textFilter))
                Records = Records.Where(x => x.Message.Any(y => y.Contains(textFilter, StringComparison.OrdinalIgnoreCase))).ToList();
            Records = Records.Where(x => logLevels.Contains(x.LogLevel)).ToList();
        }
    }
}