using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace gop
{
    public class ProblemPath
    {
        public const string F_Config = "config.json", F_Description = "description.txt", F_Input = "input.txt", F_Output = "output.txt", F_Hint = "hint.txt", F_StandardProgram = "std.cpp", D_Test = "tests", D_Sample = "samples", E_Input = ".in", E_Output = ".out";

        public string Root { get; private set; }

        public string Config { get; private set; }

        public string Description { get; private set; }

        public string Input { get; private set; }

        public string Output { get; private set; }

        public string Hint { get; private set; }

        public string StandardProgram { get; private set; }

        public string Samples { get; private set; }

        public string Tests { get; private set; }

        public TestCasePath GetSample(string name)
        {
            return new TestCasePath
            {
                Name = name,
                InputFile = Path.Join(Samples, $"test{name}.in"),
                OutputFile = Path.Join(Samples, $"test{name}.out"),
            };
        }

        public IEnumerable<TestCasePath> GetSamples()
        {
            foreach (string infile in Directory.GetFiles(Samples, "test*.in"))
            {
                string item = Path.GetFileNameWithoutExtension(infile).Substring(4);
                string outfile = Path.Join(Path.GetDirectoryName(infile), $"test{item}.out");
                yield return new TestCasePath { Name = item, InputFile = infile, OutputFile = outfile };
            }
        }

        public IEnumerable<TestCasePath> GetTests()
        {
            foreach (string infile in Directory.GetFiles(Tests, "test*.in"))
            {
                string item = Path.GetFileNameWithoutExtension(infile).Substring(4);
                string outfile = Path.Join(Path.GetDirectoryName(infile), $"test{item}.out");
                yield return new TestCasePath { Name = item, InputFile = infile, OutputFile = outfile };
            }
        }

        public ProblemPath(string path)
        {
            Root = path;
            Config = Path.Join(path, F_Config);
            Description = Path.Join(path, F_Description);
            Input = Path.Join(path, F_Input);
            Output = Path.Join(path, F_Output);
            Hint = Path.Join(path, F_Hint);
            StandardProgram = Path.Join(path, F_StandardProgram);
            Samples = Path.Join(path, D_Sample);
            Tests = Path.Join(path, D_Test);
        }

        public void Initialize()
        {
            File.WriteAllText(Config, JsonConvert.SerializeObject(new ProblemConfig { Name = "", Author = "", TimeLimit = 1, MemoryLimit = 128 }, Formatting.Indented), Encoding.UTF8);

            File.WriteAllText(Description, "Description", Encoding.UTF8);
            File.WriteAllText(Hint, "Hint", Encoding.UTF8);
            File.WriteAllText(Input, "Input description", Encoding.UTF8);
            File.WriteAllText(Output, "Output description", Encoding.UTF8);
            File.WriteAllText(StandardProgram, "// Standard program", Encoding.UTF8);

            Directory.CreateDirectory(Samples);
            var sample0 = GetSample("0");
            File.WriteAllText(sample0.InputFile, "Sample input", Encoding.UTF8);
            File.WriteAllText(sample0.OutputFile, "sample output", Encoding.UTF8);

            Directory.CreateDirectory(Tests);
        }
    }
}