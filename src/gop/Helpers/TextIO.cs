using System.IO;
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
    }
}
