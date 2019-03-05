using gop.Helper;
using Markdig;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace gop
{
    class Program
    {
        static int Main(string[] args)
        {
            // Add them to the root command
            var rootCommand = new RootCommand();
            rootCommand.Description = "A CLI tool to generate Online-Judge Problem. Powered by StardustDL. Source codes at https://github.com/StardustDL/generator-oj-problem";

            var initCommand = new Command("init", "Initialize the problem. The current directory must be empty.")
            {
                Handler = CommandHandler.Create(() => { Init(); })
            };

            var checkCommand = new Command("check", "Check whether the problem is available to pack.")
            {
                Handler = CommandHandler.Create(() =>
                {
                    if (Check().Any(x => x.Level == IssueLevel.Error))
                    {
                        ConsoleUI.WriteError(new OutputText("Problem checking failed.", true));
                    }
                    else
                    {
                        ConsoleUI.WriteSuccess(new OutputText("Problem checking passed.", true));
                    }
                })
            };

            var packCommand = new Command("pack", "Pack the problem into one package to submit.");
            packCommand.AddOption(new Option("--force", "Pack although checking failing.", new Argument<bool>()));
            packCommand.Handler = CommandHandler.Create<bool>((bool force) => { Pack(force); });

            var previewCommand = new Command("preview", "Preview the problem.")
            {
                Handler = CommandHandler.Create(() => { Preview(); })
            };

            rootCommand.Add(initCommand);
            rootCommand.Add(packCommand);
            rootCommand.Add(checkCommand);
            rootCommand.Add(previewCommand);

            // Parse the incoming args and invoke the handler
            return rootCommand.InvokeAsync(args).Result;
        }

        static ProblemPath Load()
        {
            return new ProblemPath(Directory.GetCurrentDirectory());
        }

        static void Init()
        {
            if (Directory.GetFileSystemEntries(Directory.GetCurrentDirectory()).Length != 0)
            {
                ConsoleUI.WriteError(new OutputText($"Current directory \"{Directory.GetCurrentDirectory()}\" is not empty. Please use an empty directory.", true));
                return;
            }

            ConsoleUI.Write(new OutputText("Initializing the problem...", false));
            try
            {
                Load().Initialize();
                ConsoleUI.WriteSuccess(new OutputText("Succeeded", true));
            }
            catch (Exception ex)
            {
                ConsoleUI.WriteError(new OutputText("Failed", true));
                ConsoleUI.Write(new OutputText($"An error has occurred: {ex.Message}。", true));
            }
        }

        static string ReadAll(string path)
        {
            return File.ReadAllText(path, Encoding.UTF8);
        }

        static void Preview()
        {
            try
            {
                var problem = Load();

                var config = JsonConvert.DeserializeObject<ProblemConfig>(ReadAll(problem.Config));

                ConsoleUI.WriteInfo(new OutputText("Metadata", true));
                Console.WriteLine();
                ConsoleUI.Write(new OutputText("Name         | ", false));
                ConsoleUI.Write(new OutputText(config.Name, true));
                ConsoleUI.Write(new OutputText("Author       | ", false));
                ConsoleUI.Write(new OutputText(config.Author, true));
                ConsoleUI.Write(new OutputText("Time Limit   | ", false));
                ConsoleUI.Write(new OutputText($"{config.TimeLimit} second(s)", true));
                ConsoleUI.Write(new OutputText("Memory Limit | ", false));
                ConsoleUI.Write(new OutputText($"{config.MemoryLimit} MB", true));
                Console.WriteLine();

                ConsoleUI.WriteInfo(new OutputText("Description", true));
                Console.WriteLine();
                ConsoleUI.Write(new OutputText(ReadAll(problem.Description), true));
                Console.WriteLine();

                ConsoleUI.WriteInfo(new OutputText("Input", true));
                Console.WriteLine();
                ConsoleUI.Write(new OutputText(ReadAll(problem.Input), true));
                Console.WriteLine();

                ConsoleUI.WriteInfo(new OutputText("Output", true));
                Console.WriteLine();
                ConsoleUI.Write(new OutputText(ReadAll(problem.Output), true));
                Console.WriteLine();

                ConsoleUI.WriteInfo(new OutputText("Samples", true));
                Console.WriteLine();
                foreach (var v in problem.GetSamples())
                {
                    ConsoleUI.WriteInfo(new OutputText($"Sample {v.Name} Input", true));
                    ConsoleUI.Write(new OutputText(ReadAll(v.InputFile), true));
                    ConsoleUI.WriteInfo(new OutputText($"Sample {v.Name} Output", true));
                    ConsoleUI.Write(new OutputText(ReadAll(v.OutputFile), true));
                    Console.WriteLine();
                }

                ConsoleUI.WriteInfo(new OutputText("Hint", true));
                Console.WriteLine();
                ConsoleUI.Write(new OutputText(ReadAll(problem.Hint), true));
                Console.WriteLine();

                var tests = problem.GetTests().ToArray();
                ConsoleUI.WriteInfo(new OutputText($"Tests: found {tests.Length} test(s)", true));
                Console.WriteLine();
                foreach (var v in tests)
                {
                    var fi = new FileInfo(v.InputFile);
                    var fo = new FileInfo(v.OutputFile);
                    ConsoleUI.Write(new OutputText($"Test {v.Name}:", true));
                    ConsoleUI.Write(new OutputText($"  Length of input: {fi.Length} byte(s)", true));
                    ConsoleUI.Write(new OutputText($"  Length of output: {fo.Length} byte(s)", true));
                }
            }
            catch (Exception ex)
            {
                ConsoleUI.WriteError(new OutputText("Previewing problem failed", true));
                ConsoleUI.Write(new OutputText($"An error has occurred: {ex.Message}。", true));
            }
        }

        static void ShowIssue(Issue issue, string indent = "")
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
            ConsoleUI.Write(new OutputText(issue.Content, true));
        }

        static List<Issue> Check()
        {
            bool IsPass(IEnumerable<Issue> issues, bool show = true, string indent = "")
            {
                bool flag = !issues.Any(x => x.Level == IssueLevel.Error);
                if (flag)
                    ConsoleUI.WriteSuccess(new OutputText("Passed", true));
                else
                    ConsoleUI.WriteError(new OutputText("Failed!!!", true));
                if (show) foreach (var v in issues) ShowIssue(v, indent);
                return flag;
            }

            var problem = Load();
            List<Issue> result = new List<Issue>();

            {
                ConsoleUI.WriteInfo(new OutputText("Checking ", false));
                ConsoleUI.Write(new OutputText("config...", false));
                List<Issue> ciss = new List<Issue>();
                ProblemConfig config = null;
                if (!File.Exists(problem.Config))
                {
                    var issue = new Issue(IssueLevel.Error, "The problem configuration information is not found.");
                    ciss.Add(issue);
                }
                else
                {
                    try
                    {
                        config = JsonConvert.DeserializeObject<ProblemConfig>(ReadAll(problem.Config));
                    }
                    catch
                    {
                        var issue = new Issue(IssueLevel.Error, "The problem profile format is incorrect.");
                        ciss.Add(issue);
                    }
                }

                if (config != null)
                    ciss.AddRange(Linter.Config(config));

                result.AddRange(ciss);

                if (!IsPass(ciss, indent: "  ")) return result;
            }

            {
                ConsoleUI.WriteInfo(new OutputText("Checking ", false));
                ConsoleUI.Write(new OutputText("description...", false));
                List<Issue> ciss = new List<Issue>(Linter.Description(problem));
                result.AddRange(ciss);

                if (!IsPass(ciss, indent: "  ")) return result;
            }

            {
                ConsoleUI.WriteInfo(new OutputText("Checking ", false));
                ConsoleUI.Write(new OutputText("samples...", true));
                List<Issue> ciss = new List<Issue>();

                var data = problem.GetSamples().ToArray();

                switch (data.Length)
                {
                    case 0:
                        {
                            var iss = new Issue(IssueLevel.Error, "Sample data is missing.");
                            ciss.Add(iss);
                            ShowIssue(iss, "  ");
                            break;
                        }
                    case 1:
                        break;
                    default:
                        {
                            var iss = new Issue(IssueLevel.Warning, "There are more than one sample of data, and only the first one will be used.");
                            ciss.Add(iss);
                            ShowIssue(iss, "  ");
                            break;
                        }
                }

                for (int i = 0; i < data.Length; i++)
                {
                    var t = data[i];
                    ConsoleUI.Write(new OutputText("  [", false));
                    ConsoleUI.WriteInfo(new OutputText((i + 1).ToString(), false));
                    ConsoleUI.Write(new OutputText($"/{data.Length}]", false));
                    ConsoleUI.Write(new OutputText($" Sample {t.Name}: ", false));
                    var iss = Linter.TestCase(t);
                    ciss.AddRange(iss);
                    IsPass(iss, indent: "    ");
                }

                result.AddRange(ciss);

                ConsoleUI.Write(new OutputText("  Samples: ", false));
                if (!IsPass(ciss, false)) return result;
            }

            {
                ConsoleUI.WriteInfo(new OutputText("Checking ", false));
                ConsoleUI.Write(new OutputText("tests...", true));
                List<Issue> ciss = new List<Issue>();

                var data = problem.GetTests().ToArray();

                switch (data.Length)
                {
                    case 0:
                        {
                            var iss = new Issue(IssueLevel.Error, "The test data is missing.");
                            ciss.Add(iss);
                            ShowIssue(iss, "  ");
                            break;
                        }
                    case 1:
                        break;
                }

                for (int i = 0; i < data.Length; i++)
                {
                    var t = data[i];
                    ConsoleUI.Write(new OutputText("  [", false));
                    ConsoleUI.WriteInfo(new OutputText((i + 1).ToString(), false));
                    ConsoleUI.Write(new OutputText($"/{data.Length}]", false));
                    ConsoleUI.Write(new OutputText($" Test {t.Name}: ", false));
                    var iss = Linter.TestCase(t);
                    ciss.AddRange(iss);
                    IsPass(iss, indent: "    ");
                }
                result.AddRange(ciss);

                ConsoleUI.Write(new OutputText("  Tests: ", false));
                if (!IsPass(ciss, false)) return result;
            }
            return result;
        }

        static void Pack(bool uncheck = false)
        {
            var problem = Load();

            ConsoleUI.WriteInfo(new OutputText("Checking before pack...", true));

            var lints = Check().ToList();

            if (!uncheck)
            {
                if (lints.Any(x => x.Level == IssueLevel.Error))
                {
                    ConsoleUI.WriteError(new OutputText("The problem information is missing or incorrectly formatted.", true));
                    ConsoleUI.Write(new OutputText("Please check and repackage after passing or use --force option to force no-checked package.", true));
                    return;
                }
            }

            ConsoleUI.WriteInfo(new OutputText("Packing the problem...", true));
            try
            {
                var config = JsonConvert.DeserializeObject<ProblemConfig>(ReadAll(problem.Config));

                Directory.CreateDirectory(problem.Target);

                if (Directory.Exists(problem.Temp)) Directory.Delete(problem.Temp, true);
                Directory.CreateDirectory(problem.Temp);

                string _file = $"package-{config.Author}.zip";
                if (uncheck) _file = $"package-{config.Author}-unchecked.zip";
                string filename = Path.Join(problem.Target, _file);
                if (File.Exists(filename)) File.Delete(filename);

                using (ZipArchive arc = ZipFile.Open(filename, ZipArchiveMode.Create, Encoding.UTF8))
                {
                    ConsoleUI.Write(new OutputText("  Copy problem metadata...", true));
                    arc.CreateEntryFromFile(problem.Config, ProblemPath.F_Config);

                    ConsoleUI.Write(new OutputText("  Copy descriptions...", true));

                    var descriptionEntry = arc.CreateEntry(Path.Join(ProblemPath.D_Description, ProblemPath.F_Description));
                    using (StreamWriter sw = new StreamWriter(descriptionEntry.Open(), Encoding.UTF8))
                        sw.WriteLine(Markdown.ToHtml(ReadAll(problem.Description)));

                    var inputEntry = arc.CreateEntry(Path.Join(ProblemPath.D_Description, ProblemPath.F_Input));
                    using (StreamWriter sw = new StreamWriter(inputEntry.Open(), Encoding.UTF8))
                        sw.WriteLine(Markdown.ToHtml(ReadAll(problem.Input)));

                    var outputEntry = arc.CreateEntry(Path.Join(ProblemPath.D_Description, ProblemPath.F_Output));
                    using (StreamWriter sw = new StreamWriter(outputEntry.Open(), Encoding.UTF8))
                        sw.WriteLine(Markdown.ToHtml(ReadAll(problem.Output)));

                    var hintEntry = arc.CreateEntry(Path.Join(ProblemPath.D_Description, ProblemPath.F_Hint));
                    using (StreamWriter sw = new StreamWriter(hintEntry.Open(), Encoding.UTF8))
                        sw.WriteLine(Markdown.ToHtml(ReadAll(problem.Hint)));

                    var sourceEntry = arc.CreateEntry(Path.Join(ProblemPath.D_Description, ProblemPath.F_Source));
                    using (StreamWriter sw = new StreamWriter(sourceEntry.Open(), Encoding.UTF8))
                        sw.WriteLine(Markdown.ToHtml(ReadAll(problem.Source)));

                    // arc.CreateEntryFromFile(problem.Description, Path.Join(ProblemPath.D_Description, ProblemPath.F_Description));
                    // arc.CreateEntryFromFile(problem.Input, Path.Join(ProblemPath.D_Description, ProblemPath.F_Input));
                    // arc.CreateEntryFromFile(problem.Output, Path.Join(ProblemPath.D_Description, ProblemPath.F_Output));
                    // arc.CreateEntryFromFile(problem.Hint, Path.Join(ProblemPath.D_Description, ProblemPath.F_Hint));

                    ConsoleUI.Write(new OutputText("  Copy source codes...", true));
                    arc.CreateEntryFromFile(problem.StandardProgram, Path.Join(ProblemPath.D_SourceCode, ProblemPath.F_StandardProgram));

                    ConsoleUI.Write(new OutputText("  Copy samples...", true));
                    foreach (var u in problem.GetSamples())
                    {
                        arc.CreateEntryFromFile(u.InputFile, Path.Join(ProblemPath.D_Sample, ProblemPath.GetTestInput(u.Name)));
                        arc.CreateEntryFromFile(u.OutputFile, Path.Join(ProblemPath.D_Sample, ProblemPath.GetTestOutput(u.Name)));
                    }

                    ConsoleUI.Write(new OutputText("  Copy tests...", true));
                    string tests = Path.Join(problem.Temp, "tests.zip");
                    ZipFile.CreateFromDirectory(problem.Tests, tests, CompressionLevel.Fastest, false, Encoding.UTF8);
                    arc.CreateEntryFromFile(tests, "tests.zip");

                    if (Directory.GetFileSystemEntries(problem.Extra).Length > 0)
                    {
                        ConsoleUI.Write(new OutputText("  Copy data...", true));
                        string data = Path.Join(problem.Temp, "data.zip");
                        ZipFile.CreateFromDirectory(problem.Extra, data, CompressionLevel.Fastest, false, Encoding.UTF8);
                        arc.CreateEntryFromFile(tests, "data.zip");
                    }

                    ConsoleUI.Write(new OutputText("  Add logs...", true));
                    var lintEntry = arc.CreateEntry(Path.Join(ProblemPath.D_Log, ProblemPath.F_LintLog));
                    using (StreamWriter sw = new StreamWriter(lintEntry.Open(), Encoding.UTF8))
                        sw.WriteLine(JsonConvert.SerializeObject(lints, Formatting.Indented));

                    ConsoleUI.Write(new OutputText("  Add package metadata...", true));
                    var packageEntry = arc.CreateEntry(ProblemPath.F_Package);
                    using (StreamWriter sw = new StreamWriter(packageEntry.Open(), Encoding.UTF8))
                    {
                        var package = PackageConfig.Create();
                        var ass = Assembly.GetExecutingAssembly();
                        package.Sign = $"Signed - {config.Author} - {ass.FullName}";
                        sw.WriteLine(JsonConvert.SerializeObject(package, Formatting.Indented));
                    }

                    ConsoleUI.Write(new OutputText("  Saving...", true));
                }

                ConsoleUI.WriteSuccess(new OutputText("Succeeded", true));
                ConsoleUI.Write(new OutputText($"Please submit this file: {filename}", true));
            }
            catch (Exception ex)
            {
                ConsoleUI.WriteError(new OutputText("Failed", true));
                Console.WriteLine($"An error has occurred: {ex.Message}。");
            }
        }
    }
}
