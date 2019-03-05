using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace gop
{
    public class ProblemPath
    {
        public const string F_Config = "config.json", F_Description = "description.md", F_Input = "input.md", F_Output = "output.md", F_Hint = "hint.md", F_StandardProgram = "std.cpp", F_LintLog = "lints.json", F_Package = "package.json", F_Source = "sources.md";
        public const string D_Test = "tests", D_Sample = "samples", D_Description = "descriptions", D_SourceCode = "src", D_Target = "target", D_Temp = "temp", D_Log = "log", D_Extra = "extra";
        public const string E_Input = ".in", E_Output = ".out";

        public static string GetTestInput(string name)
        {
            return $"test{name}{E_Input}";
        }

        public static string GetTestOutput(string name)
        {
            return $"test{name}{E_Output}";
        }

        public string Root { get; private set; }

        public string Config { get; private set; }

        public string Target { get; private set; }

        public string Log { get; private set; }

        public string Temp { get; private set; }

        public string Descriptions { get; private set; }

        public string SourceCode { get; private set; }

        public string Description { get; private set; }

        public string Input { get; private set; }

        public string Output { get; private set; }

        public string Hint { get; private set; }

        public string StandardProgram { get; private set; }

        public string Samples { get; private set; }

        public string Tests { get; private set; }

        public string LintLog { get; private set; }

        public string Package { get; private set; }

        public string Extra { get; private set; }

        public string Source { get; private set; }

        public TestCasePath GetSample(string name)
        {
            return new TestCasePath
            {
                Name = name,
                InputFile = Path.Join(Samples, $"test{name}{E_Input}"),
                OutputFile = Path.Join(Samples, $"test{name}{E_Output}"),
            };
        }

        public IEnumerable<TestCasePath> GetSamples()
        {
            foreach (string infile in Directory.GetFiles(Samples, $"test*{E_Input}"))
            {
                string item = Path.GetFileNameWithoutExtension(infile).Substring(4);
                string outfile = Path.Join(Path.GetDirectoryName(infile), $"test{item}{E_Output}");
                yield return new TestCasePath { Name = item, InputFile = infile, OutputFile = outfile };
            }
        }

        public IEnumerable<TestCasePath> GetTests()
        {
            foreach (string infile in Directory.GetFiles(Tests, $"test*{E_Input}"))
            {
                string item = Path.GetFileNameWithoutExtension(infile).Substring(4);
                string outfile = Path.Join(Path.GetDirectoryName(infile), $"test{item}{E_Output}");
                yield return new TestCasePath { Name = item, InputFile = infile, OutputFile = outfile };
            }
        }

        public ProblemPath(string path)
        {
            Root = path;
            Config = Path.Join(Root, F_Config);
            Target = Path.Join(Root, D_Target);
            Temp = Path.Join(Root, D_Temp);
            Log = Path.Join(Root, D_Log);
            Descriptions = Path.Join(Root, D_Description);
            Description = Path.Join(Descriptions, F_Description);
            Input = Path.Join(Descriptions, F_Input);
            Output = Path.Join(Descriptions, F_Output);
            Hint = Path.Join(Descriptions, F_Hint);
            SourceCode = Path.Join(Root, D_SourceCode);
            StandardProgram = Path.Join(SourceCode, F_StandardProgram);
            Samples = Path.Join(Root, D_Sample);
            Tests = Path.Join(Root, D_Test);
            LintLog = Path.Join(Log, F_LintLog);
            Package = Path.Join(Root, F_Package);
            Extra = Path.Join(Root, D_Extra);
            Source = Path.Join(Descriptions, F_Source);
        }

        public void Initialize()
        {
            File.WriteAllText(Config, JsonConvert.SerializeObject(new ProblemConfig { Name = "", Author = "", TimeLimit = 1, MemoryLimit = 128 }, Formatting.Indented), Encoding.UTF8);

            Directory.CreateDirectory(Descriptions);
            File.WriteAllText(Description, "", Encoding.UTF8);
            File.WriteAllText(Hint, "", Encoding.UTF8);
            File.WriteAllText(Input, "", Encoding.UTF8);
            File.WriteAllText(Output, "", Encoding.UTF8);
            File.WriteAllText(Source, "", Encoding.UTF8);

            Directory.CreateDirectory(SourceCode);
            File.WriteAllText(StandardProgram, "// Standard program", Encoding.UTF8);

            Directory.CreateDirectory(Samples);
            var sample0 = GetSample("0");
            File.WriteAllText(sample0.InputFile, "Sample input", Encoding.UTF8);
            File.WriteAllText(sample0.OutputFile, "sample output", Encoding.UTF8);

            Directory.CreateDirectory(Tests);

            Directory.CreateDirectory(Extra);
        }
    }
}