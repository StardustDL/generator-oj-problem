﻿using gop.Helpers;
using gop.Adapters;
using gop.Problems;
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
            packCommand.AddOption(new Option("--force", "Pack although checking failing.", new Argument<bool>(false)));
            packCommand.AddOption(new Option(new string[] { "--platform", "-p" }, "The target platform.", new Argument<OnlineJudge>()));
            packCommand.Handler = CommandHandler.Create((bool force, OnlineJudge platform) => { Pack(platform, !force); });

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
                ConsoleUI.ShowException(ex);
            }
        }

        static void Preview()
        {
            try
            {
                var problem = Load();

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
                ConsoleUI.ShowException(ex);
            }
        }

        static List<Issue> Check()
        {
            var pipeline = new Pipeline<ProblemPath, List<Issue>>(Load());

            pipeline = Adapters.HustOJ.Checker.UseDefault(pipeline);

            var result = pipeline.Consume();
            if (result.IsOk()) return result.Result;
            else
            {
                ConsoleUI.ShowException(result.Exception);
                return new List<Issue> { new Issue(IssueLevel.Error, $"Internal error. {result.Exception.Message}") };
            }
        }

        static void Pack(OnlineJudge platform, bool check = true)
        {
            ConsoleUI.WriteInfo(new OutputText("Checking before pack...", true));

            var issues = Check().ToList();

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