using gop.Adapters;
using System;

namespace gop.Helpers
{
    internal struct OutputText
    {
        public ConsoleColor? ForegroundColor { get; set; }

        public ConsoleColor? BackgroundColor { get; set; }

        public string Content { get; set; }

        public bool NewLine { get; set; }

        public OutputText(string content, bool newline)
        {
            ForegroundColor = null;
            BackgroundColor = null;
            Content = content;
            NewLine = newline;
        }
    }

    internal static class ConsoleUI
    {
        public static void ShowException(Exception ex)
        {
            WriteError(new OutputText("Failed: ", false));
            Console.WriteLine($"An error has occurred: {ex.Message}");
        }

        public static void ShowIssue(Issue issue, string indent = "")
        {
            switch (issue.Level)
            {
                case IssueLevel.Error:
                    ConsoleUI.WriteError(new OutputText(indent + "[Error] ", false));
                    break;
                case IssueLevel.Warning:
                    ConsoleUI.WriteWarning(new OutputText(indent + "[Warning] ", false));
                    break;
                case IssueLevel.Info:
                    ConsoleUI.WriteInfo(new OutputText(indent + "[Info] ", false));
                    break;
            }
            Write(new OutputText(issue.Content, true));
        }

        public static void Write(OutputText item)
        {
            var oldfc = Console.ForegroundColor;
            var oldbc = Console.BackgroundColor;
            if (item.ForegroundColor.HasValue)
                Console.ForegroundColor = item.ForegroundColor.Value;
            if (item.BackgroundColor.HasValue)
                Console.BackgroundColor = item.BackgroundColor.Value;

            if (item.NewLine)
                Console.WriteLine(item.Content);
            else
                Console.Write(item.Content);

            Console.ForegroundColor = oldfc;
            Console.BackgroundColor = oldbc;
        }

        public static void WriteSuccess(OutputText item)
        {
            item.ForegroundColor = ConsoleColor.Green;
            Write(item);
        }

        public static void WriteWarning(OutputText item)
        {
            item.ForegroundColor = ConsoleColor.Yellow;
            Write(item);
        }

        public static void WriteError(OutputText item)
        {
            item.ForegroundColor = ConsoleColor.Red;
            Write(item);
        }

        public static void WriteInfo(OutputText item)
        {
            item.ForegroundColor = ConsoleColor.Cyan;
            Write(item);
        }
    }
}
