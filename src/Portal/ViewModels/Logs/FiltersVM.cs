using Common.TreeStructure;
using Common.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Common;

namespace Portal.ViewModels.Logs
{
    public class FiltersVM
    {
        public List<Record> Records { get; set; }
        public List<Node<string>> Filters { get; set; }
        public Dictionary<string, bool> LogLevelToIsChecked { get; set; }

        public string FilePath { get; set; }
        public string TreeFilter { get; set; }
        public string TextFilter { get; set; }

        public FiltersVM()
        {

        }
        public FiltersVM(string filePath)
        {
            Records = LogParser.Load(filePath).ToList();
            Filters = TreeBuilder.Create(Records, x => (x.SourceClass, x.SourceClass), x => (x.SourceMethod, x.SourceMethod));
            LogLevelToIsChecked = Records.Select(x => x.LogLevel).Distinct().ToDictionary(x => x.Text(), x => true);

            FilePath = filePath;
            TreeFilter = "[]";
            TextFilter = null;
        }
    }
}