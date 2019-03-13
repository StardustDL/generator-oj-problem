using gop.Helpers;
using gop.Judgers;
using gop.Problems;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static gop.Helpers.ConsoleUI;
using static gop.Helpers.TextIO;
using PPipeline = gop.Adapters.Pipeline<gop.Problems.ProblemPath, System.Collections.Generic.List<gop.Issue>>;

namespace gop.Adapters.Generic
{
    public static class Checker
    {
        public const string LogCategory = "Checker";

        static bool IsPass(IEnumerable<Issue> issues, bool show = true, string indent = "")
        {
            bool flag = !issues.Any(x => x.Level == IssueLevel.Error);
            if (flag)
                WriteSuccess(new OutputText("Passed", true));
            else
                WriteError(new OutputText("Failed!!!", true));
            if (show) foreach (var v in issues) ShowIssue(v, indent);
            return flag;
        }

        public static PPipeline UseLogger(this PPipeline pipeline, Logger logger)
        {
            return pipeline.Use((pipe, problem) =>
            {
                pipe.Container.Set(logger);
                return problem;
            });
        }

        public static PPipeline UseProfile(this PPipeline pipeline)
        {
            const string LogCategory = Checker.LogCategory + ".Profile";
            return pipeline.Use((pipe, problem) =>
            {
                WriteInfo(new OutputText("Checking ", false));
                Write(new OutputText("config...", false));
                var ciss = new List<Issue>();
                ProblemProfile config = null;
                pipe.Container.TryGet<Logger>(out var logger);
                logger?.Info("Starting", LogCategory);

                if (!File.Exists(problem.Profile))
                {
                    var issue = new Issue(IssueLevel.Error, "The problem profile is not found.");
                    logger?.Issue(issue, LogCategory);
                    ciss.Add(issue);
                }
                else
                {
                    try
                    {
                        config = problem.GetProfile();
                    }
                    catch (Exception ex)
                    {
                        var issue = new Issue(IssueLevel.Error, "The problem profile format is incorrect.");
                        logger?.Error(issue.Content + ex.ToString(), LogCategory);
                        ciss.Add(issue);
                    }
                }

                if (config != null)
                    ciss.AddRange(Linter.Profile(config));

                pipe.Result.AddRange(ciss);

                if (!IsPass(ciss, indent: "  "))
                    throw new Exception("Profile checking failed.");

                pipe.Container.Set(config);

                logger?.Info("Ended", LogCategory);

                return problem;
            });
        }

        public static PPipeline UseDescriptions(this PPipeline pipeline)
        {
            const string LogCategory = Checker.LogCategory + ".Descriptions";
            return pipeline.Use((pipe, problem) =>
            {
                WriteInfo(new OutputText("Checking ", false));
                Write(new OutputText("description...", false));

                pipe.Container.TryGet<Logger>(out var logger);
                logger?.Info("Starting", LogCategory);

                var ciss = new List<Issue>(Linter.Descriptions(problem));

                pipe.Result.AddRange(ciss);

                if (!IsPass(ciss, indent: "  "))
                    throw new Exception("Description checking failed.");

                logger?.Info("Ended", LogCategory);

                return problem;
            });
        }

        public static PPipeline UseSamples(this PPipeline pipeline)
        {
            const string LogCategory = Checker.LogCategory + ".Samples";
            return pipeline.Use((pipe, problem) =>
            {
                WriteInfo(new OutputText("Checking ", false));
                Write(new OutputText("samples...", true));

                pipe.Container.TryGet<Logger>(out var logger);
                logger?.Info("Starting", LogCategory);

                var ciss = new List<Issue>();

                var data = problem.GetSamples().ToArray();

                switch (data.Length)
                {
                    case 0:
                        {
                            var iss = new Issue(IssueLevel.Error, "Sample data is missing.");
                            logger?.Issue(iss, LogCategory);
                            ciss.Add(iss);
                            ShowIssue(iss, "  ");
                            break;
                        }
                    case 1:
                        break;
                    default:
                        {
                            var iss = new Issue(IssueLevel.Warning, "There are more than one sample of data, and only the first one will be used.");
                            logger?.Issue(iss, LogCategory);
                            ciss.Add(iss);
                            ShowIssue(iss, "  ");
                            break;
                        }
                }

                for (int i = 0; i < data.Length; i++)
                {
                    var t = data[i];
                    Write(new OutputText("  [", false));
                    WriteInfo(new OutputText((i + 1).ToString(), false));
                    Write(new OutputText($"/{data.Length}]", false));
                    Write(new OutputText($" Sample {t.Name}: ", false));
                    logger?.Info($"Sample {t.Name}", LogCategory);
                    var iss = Linter.TestCase(t).ToList();
                    iss.ForEach(x => logger?.Issue(x, LogCategory));
                    ciss.AddRange(iss);
                    IsPass(iss, indent: "    ");
                }

                pipe.Result.AddRange(ciss);

                Write(new OutputText("  Samples: ", false));

                if (!IsPass(ciss, false))
                    throw new Exception("Samples checking failed.");

                logger?.Info("Ended", LogCategory);

                return problem;
            });
        }

        public static PPipeline UseTests(this PPipeline pipeline)
        {
            const string LogCategory = Checker.LogCategory + ".Tests";
            return pipeline.Use((pipe, problem) =>
            {
                WriteInfo(new OutputText("Checking ", false));
                Write(new OutputText("tests...", true));

                pipe.Container.TryGet<Logger>(out var logger);
                logger?.Info("Starting", LogCategory);

                var ciss = new List<Issue>();

                var data = problem.GetTests().ToArray();

                switch (data.Length)
                {
                    case 0:
                        {
                            var iss = new Issue(IssueLevel.Error, "The test data is missing.");
                            logger?.Issue(iss, LogCategory);
                            ciss.Add(iss);
                            ShowIssue(iss, "  ");
                            break;
                        }
                }

                for (int i = 0; i < data.Length; i++)
                {
                    var t = data[i];
                    Write(new OutputText("  [", false));
                    WriteInfo(new OutputText((i + 1).ToString(), false));
                    Write(new OutputText($"/{data.Length}]", false));
                    Write(new OutputText($" Test {t.Name}: ", false));
                    logger?.Info($"Test {t.Name}", LogCategory);
                    var iss = Linter.TestCase(t).ToList();
                    iss.ForEach(v => logger.Issue(v, LogCategory));
                    ciss.AddRange(iss);
                    IsPass(iss, indent: "    ");
                }

                pipe.Result.AddRange(ciss);

                Write(new OutputText("  Tests: ", false));

                if (!IsPass(ciss, false))
                    throw new Exception("Tests checking failed.");

                logger?.Info("Ended", LogCategory);

                return problem;
            });
        }

        public static PPipeline UseLocalJudger(this PPipeline pipeline)
        {
            const string LogCategory = Checker.LogCategory + ".LocalJudger";
            void ShowState(JudgeState state)
            {
                switch (state)
                {
                    case JudgeState.Accept:
                        WriteSuccess(new OutputText("Accept", true));
                        break;
                    case JudgeState.MemoryLimitError:
                        WriteError(new OutputText("Memory Limit Error", true));
                        break;
                    case JudgeState.TimeLimitError:
                        WriteError(new OutputText("Time Limit Error", true));
                        break;
                    case JudgeState.RuntimeError:
                        WriteError(new OutputText("Runtime Error", true));
                        break;
                    case JudgeState.SystemError:
                        WriteError(new OutputText("System Error", true));
                        break;
                    case JudgeState.WrongAnswer:
                        WriteError(new OutputText("Wrong Answer", true));
                        break;
                    default:
                        throw new Exception("Unexcepted judge state: " + state.ToString());
                }
            }

            return pipeline.Use((pipe, problem) =>
            {
                WriteInfo(new OutputText("Checking ", false));
                Write(new OutputText("all data by local judger...", true));

                pipe.Container.TryGet<Logger>(out var logger);
                logger?.Info("Starting", LogCategory);

                var profile = pipe.Container.Get<ProblemProfile>();

                var ciss = new List<Issue>();

                foreach (var t in problem.GetSamples())
                {
                    Write(new OutputText($"  Sample {t.Name}...", false));
                    var result = Judger.Judge($"sample {t.Name}", profile.StdRun, TimeSpan.FromSeconds(profile.TimeLimit), profile.MemoryLimit * 1024 * 1024, ReadAll(t.InputFile), File.ReadAllLines(t.OutputFile, Encoding.UTF8));
                    ShowState(result.State);
                    logger?.Info($"Sample {t.Name}: {result.State}", LogCategory);
                    result.Issues.ForEach(i => logger.Issue(i, LogCategory));
                    ciss.AddRange(result.Issues);
                }

                foreach (var t in problem.GetTests())
                {
                    Write(new OutputText($"  Test {t.Name}...", false));
                    var result = Judger.Judge($"test {t.Name}", profile.StdRun, TimeSpan.FromSeconds(profile.TimeLimit), profile.MemoryLimit * 1024 * 1024, ReadAll(t.InputFile), File.ReadAllLines(t.OutputFile, Encoding.UTF8));
                    ShowState(result.State);
                    logger?.Info($"Test {t.Name}: {result.State}", LogCategory);
                    result.Issues.ForEach(i => logger.Issue(i, LogCategory));
                    ciss.AddRange(result.Issues);
                }

                pipe.Result.AddRange(ciss);

                Write(new OutputText("  Local judging: ", false));

                if (!IsPass(ciss, indent: "    "))
                    throw new Exception("Local judging checking failed.");

                logger?.Info("Ended", LogCategory);

                return problem;
            });
        }
    }
}
