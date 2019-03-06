using gop.Adapters;
using gop.Helpers;
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
        HustOJ,
    }

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

            var checkCommand = new Command("check", "Check whether the problem is available to pack.");
            checkCommand.AddOption(new Option("--disable-local-judger", "Disable local judging for check", new Argument<bool>(false)));
            checkCommand.Handler = CommandHandler.Create((bool disableLocalJudger) =>
            {
                if (Check(!disableLocalJudger).Any(x => x.Level == IssueLevel.Error))
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
            packCommand.AddOption(new Option("--disable-local-judger", "Disable local judging for check", new Argument<bool>(false)));
            packCommand.AddOption(new Option(new string[] { "--platform", "-p" }, "The target platform.", new Argument<OnlineJudge>()));
            packCommand.Handler = CommandHandler.Create((bool force, bool disableLocalJudger, OnlineJudge platform) => { Pack(platform, !force, !disableLocalJudger); });

            var previewCommand = new Command("preview", "Preview the problem.");
            previewCommand.AddOption(new Option("--html", "Generate an HTML document for previewing.", new Argument<bool>(false)));
            previewCommand.Handler = CommandHandler.Create((bool html) => { Preview(html); });

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
                var config = JsonConvert.DeserializeObject<ProblemProfile>(ReadAll(problem.Profile));

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
                var config = JsonConvert.DeserializeObject<ProblemProfile>(ReadAll(problem.Profile));

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

        static List<Issue> Check(bool withLocalJudge = true)
        {
            var pipeline = new Pipeline<ProblemPath, List<Issue>>(Load());

            pipeline = Adapters.HustOJ.Checker.UseDefault(pipeline);
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

            var issues = Check(withLocalJudge).ToList();

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

            PackageProfile profile = PackageProfile.Create();
            {
                var propro = JsonConvert.DeserializeObject<ProblemProfile>(ReadAll(problem.Profile));

                profile.Sign = $"Signed-{propro.Author}-{Assembly.GetExecutingAssembly().FullName}";
            }
            profile.Checked = check;

            switch (platform)
            {
                case OnlineJudge.HustOJ:
                    profile.Platform = "hustoj";
                    pipeline = Adapters.HustOJ.Packer.UseDefault(pipeline, profile);

                    break;
                default:
                    ConsoleUI.WriteError(new OutputText("Unsupported platform.", true));
                    return;
            }

            if (Directory.GetFileSystemEntries(problem.Extra).Length > 0)
                pipeline = Adapters.Generic.Packer.UseExtra(pipeline);

            pipeline = Adapters.Generic.Packer.UseIssues(pipeline, issues);
            pipeline = Adapters.Generic.Packer.UsePackage(pipeline);
            pipeline = Adapters.Generic.Packer.UseSave(pipeline);

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
