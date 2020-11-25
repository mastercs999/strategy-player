using Common;
using Common.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace Portal.ViewModels.Logs
{
    public class LogParser
    {
        private static readonly int TimestampLength = "2017-10-08 10:47:52.3805".Length;




        public static IEnumerable<Record> Load(string filePath)
        {
            // Load file content
            string[] lines = File.ReadAllLines(filePath);

            // Parse records
            int linePointer = 0;
            while (linePointer < lines.Length)
            {
                string currentLine = lines[linePointer];

                // First line must be header line
                if (!IsHeaderLine(currentLine))
                    throw new Exception($"There should be a header line on line {linePointer + 1} in {filePath}");

                // Parse header
                string timestamp = currentLine.Substring(0, TimestampLength);
                LogLevel logLevel = ParseLogLevel(currentLine);
                string sourceClass = ParseClass(currentLine);
                string sourceMethod = ParseMethod(currentLine);
                string stackPath = ParseStackPath(currentLine);

                // Parse record
                ++linePointer;
                string[] message = ParseMessage(lines, ref linePointer);

                yield return new Record()
                {
                    Timestamp = timestamp,
                    LogLevel = logLevel,
                    SourceClass = sourceClass,
                    SourceMethod = sourceMethod,
                    StackPath = stackPath,
                    Message = message
                };
            }
        }




        private static bool IsHeaderLine(string line)
        {
            return line.Length >= TimestampLength && line.Substring(0, TimestampLength).Trim().Length > 0;
        }
        private static LogLevel ParseLogLevel(string headerLine)
        {
            headerLine = headerLine.Substring(TimestampLength + 1);
            headerLine = headerLine.Substring(0, headerLine.IndexOf('|') - 1);

            return headerLine.ToEnum<LogLevel>();
        }
        private static string ParseClass(string headerLine)
        {
            headerLine = headerLine.Substring(headerLine.IndexOf('(') + 1);
            headerLine = headerLine.Substring(0, headerLine.IndexOf(')'));

            return headerLine.Trim();
        }
        private static string ParseMethod(string headerLine)
        {
            headerLine = headerLine.Substring(headerLine.IndexOf('|') + 1);
            headerLine = headerLine.Substring(0, headerLine.IndexOf('('));

            return headerLine.Trim();
        }
        private static string ParseStackPath(string headerLine)
        {
            return headerLine.Substring(headerLine.IndexOf('|') + 1).Trim();
        }
        private static string[] ParseMessage(string[] lines, ref int linePointer)
        {
            List<string> messageLines = new List<string>();

            while (linePointer < lines.Length && !IsHeaderLine(lines[linePointer]))
                messageLines.Add(lines[linePointer++].TrimStart());

            return messageLines.ToArray();
        }
    }
}