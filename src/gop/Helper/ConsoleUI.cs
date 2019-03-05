using System;
using System.Collections.Generic;
using System.Text;

namespace gop.Helper
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

    internal class ConsoleUI
    {
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
