using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace Portal.ViewModels.Logs
{
    public class IndexVM
    {
        public List<LogFile> LogFiles { get; set; }
        public string FilePath { get; set; }

        public IndexVM()
        {

        }
        public IndexVM(string logFolder)
        {
            // Find all logs
            LogFiles = Directory.GetFiles(logFolder).OrderByDescending(x => Path.GetFileName(x)).Select(x => new LogFile()
            {
                Text = Path.GetFileNameWithoutExtension(x),
                FilePath = x
            }).ToList();
            FilePath = LogFiles.FirstOrDefault()?.FilePath;
        }
    }
}