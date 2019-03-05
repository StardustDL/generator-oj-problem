using gop.Helper;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.IO.Compression;
using System.Linq;
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

            var initCommand = new Command("init")
            {
                Handler = CommandHandler.Create(() => { Init(); })
            };

            var checkCommand = new Command("check")
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

            var packCommand = new Command("pack")
            {
                Handler = CommandHandler.Create(() => { Pack(); })
            };

            var previewCommand = new Command("preview")
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
                    var issue = new Issue(IssueLevel.Error, "The problem configuration information is not found");
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

        static void Pack()
        {
            var problem = Load();

            ConsoleUI.WriteInfo(new OutputText("Checking before pack...", true));

            if (Check().Any(x => x.Level == IssueLevel.Error))
            {
                ConsoleUI.WriteError(new OutputText("The problem information is missing or incorrectly formatted, please check and repackage after passing.", true));
                return;
            }

            {
                ConsoleUI.Write(new OutputText("Packing the problem...", false));
                try
                {
                    string path = Directory.GetCurrentDirectory();

                    ZipFile.CreateFromDirectory(Path.Join(path, problem.Tests), Path.Join(path, "test.zip"), CompressionLevel.Fastest, false);

                    string outpath = Path.Join(Path.GetDirectoryName(path), "package.zip");

                    ZipFile.CreateFromDirectory(path, outpath, CompressionLevel.Fastest, false);

                    ConsoleUI.WriteSuccess(new OutputText("Succeeded", true));
                    ConsoleUI.Write(new OutputText($"Please submit this file: {outpath}", true));
                }
                catch (Exception ex)
                {
                    ConsoleUI.WriteError(new OutputText("Failed", true));
                    Console.WriteLine($"An error has occurred: {ex.Message}。");
                }
            }
        }
    }
}
