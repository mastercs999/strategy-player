using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Portal.ViewModels.Logs
{
    public class Record
    {
        public string Timestamp { get; set; }
        public LogLevel LogLevel { get; set; }
        public string SourceClass { get; set; }
        public string SourceMethod { get; set; }
        public string StackPath { get; set; }
        public string[] Message { get; set; }
    }
}