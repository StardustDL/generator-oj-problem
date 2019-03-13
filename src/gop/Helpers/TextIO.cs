using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace gop.Helpers
{
    internal static class TextIO
    {
        public static readonly Encoding UTF8WithoutBOM = new UTF8Encoding(false);

        public static string ReadAll(string path)
        {
            return File.ReadAllText(path, Encoding.UTF8);
        }

        public static void WriteAll(string path, string content)
        {
            File.WriteAllText(path, content, Encoding.UTF8);
        }

        public static void ConvertToLF(StreamReader reader, StreamWriter writer)
        {
            while (!reader.EndOfStream)
                writer.Write(reader.ReadLine() + "\n");
        }

        public static string ConvertToLF(StreamReader reader)
        {
            StringBuilder sb = new StringBuilder();
            while (!reader.EndOfStream)
                sb.Append(reader.ReadLine() + "\n");
            return sb.ToString();
        }

        public static List<string> Diff(List<string> expected, List<string> real)
        {
            while (expected.Count > 0 && string.IsNullOrEmpty(expected.Last())) expected.RemoveAt(expected.Count - 1);
            while (real.Count > 0 && string.IsNullOrEmpty(real.Last())) real.RemoveAt(real.Count - 1);

            List<string> result = new List<string>();

            if (expected.Count != real.Count)
            {
                result.Add($"The count of lines are not equal: expected {expected}, but real {real}.");
                return result;
            }
            for (int i = 0; i < expected.Count; i++)
            {
                if (expected[i].TrimEnd() != real[i].TrimEnd())
                {
                    result.Add($"Contents at line {i + 1} are not equal.");
                }
            }

            return result;
        }
    }
}
