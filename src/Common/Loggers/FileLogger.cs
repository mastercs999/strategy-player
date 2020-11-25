using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Loggers
{
    public class FileLogger : Logger
    {
        private readonly string DirectoryPath;
        private readonly object Lock = new object();


        public FileLogger(string logDirectory)
        {
            DirectoryPath = logDirectory;

            // Create directory if doesn't exist
            Directory.CreateDirectory(DirectoryPath);
        }


        public override void FlushMessage(string message)
        {
            // Write to the file
            lock (Lock)
                File.AppendAllText(Path.Combine(DirectoryPath, DateTimeOffset.UtcNow.ToString("yyyy_MM_dd", CultureInfo.InvariantCulture) + ".log"), message);
        }
    }
}
