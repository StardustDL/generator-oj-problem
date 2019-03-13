using System;
using System.Collections.Generic;

namespace gop
{
    public enum LogLevel
    {
        Info,
        Warning,
        Error
    }

    public class LogItem
    {
        public LogLevel Level { get; set; }

        public string Category { get; set; }

        public DateTimeOffset Time { get; set; }

        public string Content { get; set; }
    }

    public class Logger
    {
        readonly List<LogItem> contents = new List<LogItem>();

        public void Issue(Issue issue, string category = "")
        {
            switch (issue.Level)
            {
                case IssueLevel.Error:
                    Error(issue.Content, category);
                    break;
                case IssueLevel.Warning:
                    Warning(issue.Content, category);
                    break;
                case IssueLevel.Info:
                    Info(issue.Content, category);
                    break;
            }
        }

        public void Error(Exception exception, string category = "")
        {
            contents.Add(new LogItem
            {
                Level = LogLevel.Error,
                Content = exception.ToString(),
                Category = category,
                Time = DateTimeOffset.Now
            });
        }

        public void Error(string content, string category = "")
        {
            contents.Add(new LogItem
            {
                Level = LogLevel.Error,
                Content = content,
                Category = category,
                Time = DateTimeOffset.Now
            });
        }

        public void Warning(string content, string category = "")
        {
            contents.Add(new LogItem
            {
                Level = LogLevel.Warning,
                Content = content,
                Category = category,
                Time = DateTimeOffset.Now
            });
        }

        public void Info(string content, string category = "")
        {
            contents.Add(new LogItem
            {
                Level = LogLevel.Info,
                Content = content,
                Category = category,
                Time = DateTimeOffset.Now
            });
        }

        public LogItem[] All()
        {
            return contents.ToArray();
        }
    }
}