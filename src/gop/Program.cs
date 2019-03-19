using gop.Adapters;
using gop.Helpers;
using gop.Judgers;
using gop.Problems;
using Markdig;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using static gop.Helpers.TextIO;

namespace gop
{
    enum OnlineJudge
    {
        Generic,
        HustOJ,
        FPS,
    }

    class Program
    {
        static int Main(string[] args)
        {
            // Add them to the root command
            var rootCommand = new RootCommand
            {
                Description = "A CLI tool to generate Online-Judge Problem. Powered by StardustDL. Source codes at https://github.com/StardustDL/generator-oj-problem ."
            };

            var initCommand = new Command("init", "Initialize the problem. The current directory must be empty.")
            {
                Handler = CommandHandler.Create(() => { Init(); })
            };

            var disableLJOption = new Option("--disable-local-judger", "Disable local judging for check.", new Argument<bool>(false));
            var platformOption = new Option(new string[] { "--platform", "-p" }, "The target platform: generic, hustoj, fps.", new Argument<OnlineJudge>());

            var checkCommand = new Command("check", "Check whether the problem is available to pack.");
            checkCommand.AddOption(disableLJOption);
            checkCommand.AddOption(platformOption);
            checkCommand.Handler = CommandHandler.Create((bool disableLocalJudger, OnlineJudge platform) =>
            {
                if (Check(platform, !disableLocalJudger).Any(x => x.Level == IssueLevel.Error))
                {
                    ConsoleUI.WriteError(new OutputText("Problem checking failed.", true));
                }
                else
                {
                    ConsoleUI.WriteSuccess(new OutputText("Problem checking passed.", true));
                }
            });

            var packCommand = new Command("pack", "Pack the problem into one package to submit.");
            packCommand.AddOption(new Option("--force", "Pack although checking failing.", new Argument<bool>(false)));
            packCommand.AddOption(disableLJOption);
            packCommand.AddOption(platformOption);
            packCommand.Handler = CommandHandler.Create((bool force, bool disableLocalJudger, OnlineJudge platform) => { Pack(platform, !force, !disableLocalJudger); });

            var previewCommand = new Command("preview", "Preview the problem.");
            previewCommand.AddOption(new Option("--html", "Generate an HTML document for previewing.", new Argument<bool>(false)));
            previewCommand.Handler = CommandHandler.Create((bool html) => { Preview(html); });

            var genCommand = new Command("gen", "Generate data.");
            genCommand.AddOption(new Option(new string[] { "-o", "--output" }, "Generate output data for samples and tests.", new Argument<bool>(true)));
            genCommand.Handler = CommandHandler.Create((bool output) => { Gen(output); });

            rootCommand.Add(initCommand);
            rootCommand.Add(packCommand);
            rootCommand.Add(checkCommand);
            rootCommand.Add(previewCommand);
            rootCommand.Add(genCommand);

            // Parse the incoming args and invoke the handler
            return rootCommand.InvokeAsync(args).Result;
        }

        static void Gen(bool output)
        {
            var problem = Load();

            if (output)
            {
                ConsoleUI.Write(new OutputText("Generating outputs...", true));
                bool ShowState(JudgeState state)
                {
                    switch (state)
                    {
                        case JudgeState.Accept:
                        case JudgeState.WrongAnswer:
                            ConsoleUI.WriteSuccess(new OutputText("Success", true));
                            return true;
                        case JudgeState.MemoryLimitExceeded:
                            ConsoleUI.WriteError(new OutputText("Memory Limit Exceeded", true));
                            return false;
                        case JudgeState.TimeLimitExceeded:
                            ConsoleUI.WriteError(new OutputText("Time Limit Exceeded", true));
                            return false;
                        case JudgeState.RuntimeError:
                            ConsoleUI.WriteError(new OutputText("Runtime Error", true));
                            return false;
                        case JudgeState.SystemError:
                            ConsoleUI.WriteError(new OutputText("System Error", true));
                            return false;
                        default:
                            ConsoleUI.WriteError(new OutputText("Unexcepted", true));
                            return false;
                    }
                }
                try
                {
                    var profile = problem.GetProfile();

                    foreach (var t in problem.GetSamples())
                    {
                        ConsoleUI.Write(new OutputText($"  Sample {t.Name}...", false));
                        var result = Judger.Judge($"sample {t.Name}", profile.StdRun, TimeSpan.FromSeconds(profile.TimeLimit), profile.MemoryLimit * 1024 * 1024, ReadAll(t.InputFile), new string[0]);
                        if (ShowState(result.State))
                        {
                            File.WriteAllLines(t.OutputFile, result.Outputs);
                        }
                        else
                        {
                            foreach (var v in result.Issues)
                            {
                                ConsoleUI.ShowIssue(v, "    ");
                            }
                        }
                    }

                    foreach (var t in problem.GetTests())
                    {
                        ConsoleUI.Write(new OutputText($"  Test {t.Name}...", false));
                        var result = Judger.Judge($"test {t.Name}", profile.StdRun, TimeSpan.FromSeconds(profile.TimeLimit), profile.MemoryLimit * 1024 * 1024, ReadAll(t.InputFile), new string[0]);
                        if (ShowState(result.State))
                        {
                            File.WriteAllLines(t.OutputFile, result.Outputs);
                        }
                        else
                        {
                            foreach (var v in result.Issues)
                            {
                                ConsoleUI.ShowIssue(v, "    ");
                            }
                        }
                    }
                    ConsoleUI.WriteSuccess(new OutputText("Generating ended.", true));
                }
                catch (Exception ex)
                {
                    ConsoleUI.ShowException(ex);
                }
            }
        }

        static ProblemPath Load()
        {
            string curDir = Directory.GetCurrentDirectory();
            while (!String.IsNullOrEmpty(curDir) && !File.Exists(Path.Join(curDir, ProblemPath.F_Profile))) curDir = Path.GetDirectoryName(curDir);
            return new ProblemPath(curDir);
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
                ConsoleUI.ShowException(ex);
            }
        }

        static void Preview(bool html)
        {
            try
            {
                var problem = Load();

                if (html)
                {
                    PreviewInHTML(problem);
                }
                else
                {
                    PreviewInCLI(problem);
                }
            }
            catch (Exception ex)
            {
                ConsoleUI.ShowException(ex);
            }

            void PreviewInHTML(ProblemPath problem)
            {
                var config = problem.GetProfile();

                StringBuilder sb = new StringBuilder();

                sb.AppendLine("# Metadata");
                sb.AppendLine();
                sb.AppendLine($"- Name: {config.Name}");
                sb.AppendLine($"- Author: {config.Author}");
                sb.AppendLine($"- Time limit: {config.TimeLimit} second(s)");
                sb.AppendLine($"- Memory Limit: {config.MemoryLimit} MB");
                sb.AppendLine();

                sb.AppendLine("# Description");
                sb.AppendLine();
                sb.AppendLine(ReadAll(problem.Description));
                sb.AppendLine();

                sb.AppendLine("# Input");
                sb.AppendLine();
                sb.AppendLine(ReadAll(problem.Input));
                sb.AppendLine();

                sb.AppendLine("# Output");
                sb.AppendLine();
                sb.AppendLine(ReadAll(problem.Output));
                sb.AppendLine();

                sb.AppendLine("# Samples");
                sb.AppendLine();
                foreach (var v in problem.GetSamples())
                {
                    sb.AppendLine($"## Samples {v.Name}");
                    sb.AppendLine("### Input");
                    sb.AppendLine("```");
                    sb.AppendLine(ReadAll(v.InputFile));
                    sb.AppendLine("```");
                    sb.AppendLine("### Output");
                    sb.AppendLine("```");
                    sb.AppendLine(ReadAll(v.OutputFile));
                    sb.AppendLine("```");
                    sb.AppendLine();
                }

                sb.AppendLine("# Hint");
                sb.AppendLine();
                sb.AppendLine(ReadAll(problem.Hint));
                sb.AppendLine();

                sb.AppendLine("# Source");
                sb.AppendLine();
                sb.AppendLine(ReadAll(problem.Source));
                sb.AppendLine();

                sb.AppendLine("# Tests");
                sb.AppendLine();
                var tests = problem.GetTests().ToArray();
                sb.AppendLine($"Tests: found **{tests.Length}** test(s)");
                sb.AppendLine();
                foreach (var v in tests)
                {
                    var fi = new FileInfo(v.InputFile);
                    var fo = new FileInfo(v.OutputFile);
                    sb.AppendLine($"- Test **{v.Name}**:");
                    sb.AppendLine($"  - Length of input: **{fi.Length}** byte(s)");
                    sb.AppendLine($"  - Length of output: **{fo.Length}** byte(s)");
                }
                sb.AppendLine();

                if (!Directory.Exists(problem.Temp)) Directory.CreateDirectory(problem.Temp);

                var output = Path.Join(problem.Temp, "preview.html");

                const string HTMLPre = @"<!DOCTYPE html>
    <html>
    <head>
        <meta http-equiv=""Content-type"" content=""text/html;charset=UTF-8"">
        <title>generator-oj-problem</title>
        <link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/gh/Microsoft/vscode/extensions/markdown-language-features/media/markdown.css"">
        <link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/gh/Microsoft/vscode/extensions/markdown-language-features/media/highlight.css"">
        <style>
.task-list-item { list-style-type: none; } .task-list-item-checkbox { margin-left: -20px; vertical-align: middle; }
</style>
        <style>
            body {
                font-family: -apple-system, BlinkMacSystemFont, 'Segoe WPC', 'Segoe UI', 'Ubuntu', 'Droid Sans', sans-serif;
                font-size: 14px;
                line-height: 1.6;
            }
        </style>
    </head>
    <body>
    #body
    </body>
</html>";

                WriteAll(output, HTMLPre.Replace("#body", Markdown.ToHtml(sb.ToString())));
                ConsoleUI.Write(new OutputText($"Open this generated file: {output} .", true));
            }

            void PreviewInCLI(ProblemPath problem)
            {
                var config = problem.GetProfile();

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

                ConsoleUI.WriteInfo(new OutputText("Source", true));
                Console.WriteLine();
                ConsoleUI.Write(new OutputText(ReadAll(problem.Source), true));
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
        }

        static List<Issue> Check(OnlineJudge platform, bool withLocalJudge = true, Logger logger = null)
        {
            var pipeline = new Pipeline<ProblemPath, List<Issue>>(Load());

            if (logger != null)
                pipeline = Adapters.Generic.Checker.UseLogger(pipeline, logger);

            switch (platform)
            {
                case OnlineJudge.Generic:
                    pipeline = Adapters.Generic.Checker.UseDefault(pipeline);
                    break;
                case OnlineJudge.HustOJ:
                    pipeline = Adapters.HustOJ.Checker.UseDefault(pipeline);
                    break;
                case OnlineJudge.FPS:
                    pipeline = Adapters.FreeProblemSet.Checker.UseDefault(pipeline);
                    break;
                default:
                    ConsoleUI.WriteError(new OutputText("Unsupported platform.", true));
                    return new List<Issue> { new Issue(IssueLevel.Error, $"Unsupported platform.") };
            }

            if (withLocalJudge)
                pipeline = Adapters.Generic.Checker.UseLocalJudger(pipeline);

            var result = pipeline.Consume();
            if (result.IsOk()) return result.Result;
            else
            {
                ConsoleUI.ShowException(result.Exception);
                return new List<Issue> { new Issue(IssueLevel.Error, $"Internal error. {result.Exception.Message}") };
            }
        }

        static void Pack(OnlineJudge platform, bool check = true, bool withLocalJudge = true)
        {
            ConsoleUI.WriteInfo(new OutputText("Checking before pack...", true));

            Logger logger = new Logger();

            var issues = Check(platform, withLocalJudge, logger).ToList();

            if (check)
            {
                if (issues.Any(x => x.Level == IssueLevel.Error))
                {
                    ConsoleUI.WriteError(new OutputText("The problem information is missing or incorrectly formatted.", true));
                    ConsoleUI.Write(new OutputText("Please check and repackage after passing or use --force option to force no-checked package.", true));
                    return;
                }
            }

            var problem = Load();
            var pipeline = new Pipeline<ProblemPath, string>(problem);
            pipeline = Adapters.Generic.Packer.UseLogger(pipeline, logger);

            PackageProfile profile = PackageProfile.Create();
            {
                var propro = problem.GetProfile();

                profile.Sign = $"Signed-{propro.Author}-{Assembly.GetExecutingAssembly().FullName}";
            }
            profile.Checked = check;

            switch (platform)
            {
                case OnlineJudge.Generic:
                    pipeline = Adapters.Generic.Packer.UseDefaultMarkdown(pipeline, profile);
                    if (Directory.GetFileSystemEntries(problem.Extra).Length > 0)
                        pipeline = Adapters.Generic.Packer.UseExtra(pipeline);
                    pipeline = Adapters.Generic.Packer.UseIssues(pipeline, issues);
                    pipeline = Adapters.Generic.Packer.UsePackage(pipeline);
                    pipeline = Adapters.Generic.Packer.UseSave(pipeline);
                    break;
                case OnlineJudge.HustOJ:
                    pipeline = Adapters.HustOJ.Packer.UseDefault(pipeline, profile);
                    if (Directory.GetFileSystemEntries(problem.Extra).Length > 0)
                        pipeline = Adapters.Generic.Packer.UseExtra(pipeline);
                    pipeline = Adapters.Generic.Packer.UseIssues(pipeline, issues);
                    pipeline = Adapters.Generic.Packer.UsePackage(pipeline);
                    pipeline = Adapters.Generic.Packer.UseSave(pipeline);
                    break;
                case OnlineJudge.FPS:
                    pipeline = Adapters.FreeProblemSet.Packer.UseDefault(pipeline, profile);
                    pipeline = Adapters.FreeProblemSet.Packer.UseSave(pipeline);
                    break;
                default:
                    ConsoleUI.WriteError(new OutputText("Unsupported platform.", true));
                    return;
            }
            
            var result = pipeline.Consume();
            if (result.IsOk())
            {
                ConsoleUI.WriteSuccess(new OutputText("Succeeded", true));
                ConsoleUI.Write(new OutputText($"Please submit this file: {result.Result}", true));
            }
            else
            {
                ConsoleUI.ShowException(result.Exception);
            }
        }
    }
}
