using gop.Helpers;
using gop.Judgers;
using gop.Problems;
using Markdig;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static gop.Helpers.ConsoleUI;
using static gop.Helpers.TextIO;
using PPipeline = gop.Adapters.Pipeline<gop.Problems.ProblemPath, System.Collections.Generic.List<gop.Adapters.Issue>>;

namespace gop.Adapters.Generic
{
    public static class Checker
    {
        public const string ID_Problem = "problem";

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

        public static PPipeline UseProfile(this PPipeline pipeline)
        {
            return pipeline.Use((pipe, problem) =>
            {
                WriteInfo(new OutputText("Checking ", false));
                Write(new OutputText("config...", false));
                var ciss = new List<Issue>();
                ProblemProfile config = null;
                if (!File.Exists(problem.Profile))
                {
                    var issue = new Issue(IssueLevel.Error, "The problem profile is not found.");
                    ciss.Add(issue);
                }
                else
                {
                    try
                    {
                        config = JsonConvert.DeserializeObject<ProblemProfile>(ReadAll(problem.Profile));
                    }
                    catch
                    {
                        var issue = new Issue(IssueLevel.Error, "The problem profile format is incorrect.");
                        ciss.Add(issue);
                    }
                }

                if (config != null)
                    ciss.AddRange(Linter.Profile(config));

                pipe.Result.AddRange(ciss);

                if (!IsPass(ciss, indent: "  "))
                    throw new Exception("Profile checking failed.");

                pipe.SetFlag(ID_Problem, config);

                return problem;
            });
        }

        public static PPipeline UseDescriptions(this PPipeline pipeline)
        {
            return pipeline.Use((pipe, problem) =>
            {
                WriteInfo(new OutputText("Checking ", false));
                Write(new OutputText("description...", false));
                var ciss = new List<Issue>(Linter.Descriptions(problem));

                pipe.Result.AddRange(ciss);

                if (!IsPass(ciss, indent: "  "))
                    throw new Exception("Description checking failed.");

                return problem;
            });
        }

        public static PPipeline UseSamples(this PPipeline pipeline)
        {
            return pipeline.Use((pipe, problem) =>
            {
                WriteInfo(new OutputText("Checking ", false));
                Write(new OutputText("samples...", true));
                var ciss = new List<Issue>();

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
                    Write(new OutputText("  [", false));
                    WriteInfo(new OutputText((i + 1).ToString(), false));
                    Write(new OutputText($"/{data.Length}]", false));
                    Write(new OutputText($" Sample {t.Name}: ", false));
                    var iss = Linter.TestCase(t);
                    ciss.AddRange(iss);
                    IsPass(iss, indent: "    ");
                }

                pipe.Result.AddRange(ciss);

                Write(new OutputText("  Samples: ", false));

                if (!IsPass(ciss, false))
                    throw new Exception("Samples checking failed.");

                return problem;
            });
        }

        public static PPipeline UseTests(this PPipeline pipeline)
        {
            return pipeline.Use((pipe, problem) =>
            {
                WriteInfo(new OutputText("Checking ", false));
                Write(new OutputText("tests...", true));
                var ciss = new List<Issue>();

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
                }

                for (int i = 0; i < data.Length; i++)
                {
                    var t = data[i];
                    Write(new OutputText("  [", false));
                    WriteInfo(new OutputText((i + 1).ToString(), false));
                    Write(new OutputText($"/{data.Length}]", false));
                    Write(new OutputText($" Test {t.Name}: ", false));
                    var iss = Linter.TestCase(t);
                    ciss.AddRange(iss);
                    IsPass(iss, indent: "    ");
                }

                pipe.Result.AddRange(ciss);

                Write(new OutputText("  Tests: ", false));

                if (!IsPass(ciss, false))
                    throw new Exception("Tests checking failed.");

                return problem;
            });
        }

        public static PPipeline UseLocalJudger(this PPipeline pipeline)
        {
            void TestOne(ProblemProfile profile, TestCasePath test, string name, List<Issue> ciss)
            {
                try
                {
                    Runner runner = new Runner(new System.Diagnostics.ProcessStartInfo(profile.StdRun[0], string.Join(" ", profile.StdRun.Skip(1))))
                    {
                        TimeLimit = TimeSpan.FromSeconds(profile.TimeLimit),
                        MemoryLimit = profile.MemoryLimit * 1024 * 1024,
                        Input = ReadAll(test.InputFile),
                    };
                    runner.Run();
                    switch (runner.State)
                    {
                        case RunnerState.Ended:
                            {
                                if (runner.ExitCode != 0)
                                {
                                    WriteError(new OutputText("Runtime Error: " + $"Exited with {runner.ExitCode}", true));
                                    ciss.Add(new Issue(IssueLevel.Error, $"Runtime error for {name}."));
                                }
                                var expected = File.ReadAllLines(test.OutputFile).Select(x => x.TrimEnd('\r', '\n')).ToList();
                                var real = runner.Output.Select(x => x.TrimEnd('\r', '\n')).ToList();
                                var diff = Diff(expected, real);
                                if (diff.Count != 0)
                                {
                                    WriteError(new OutputText("Wrong Answer", true));
                                    foreach (var s in diff)
                                    {
                                        WriteError(new OutputText("    " + s, true));
                                    }
                                    ciss.Add(new Issue(IssueLevel.Error, $"Wrong answer for {name}."));
                                }
                                else
                                {
                                    WriteSuccess(new OutputText("Accept", true));
                                }
                                break;
                            }
                        case RunnerState.OutOfMemory:
                            {
                                var message = $"Used {runner.MaximumMemory} bytes, limit {profile.MemoryLimit * 1024 * 1024} bytes.";
                                WriteError(new OutputText("Memory Limit Error: " + message, true));
                                ciss.Add(new Issue(IssueLevel.Error, $"Memory limit error for {name}. {message}"));
                                break;
                            }
                        case RunnerState.OutOfTime:
                            {
                                var message = $"Used {runner.RunningTime.TotalSeconds} seconds, limit {profile.TimeLimit} seconds.";
                                WriteError(new OutputText("Time Limit Error: " + message, true));
                                ciss.Add(new Issue(IssueLevel.Error, $"Time limit error for {name}. {message}"));
                                break;
                            }
                        default:
                            throw new Exception("The program doesn't stop.");
                    }
                }
                catch (Exception ex)
                {
                    WriteError(new OutputText("System Error: " + ex.Message, true));
                    ciss.Add(new Issue(IssueLevel.Error, $"System error for {name} with {ex.Message}."));
                }
            }

            return pipeline.Use((pipe, problem) =>
            {
                WriteInfo(new OutputText("Checking ", false));
                Write(new OutputText("all data by local judger...", true));
                var profile = pipe.GetFlag<ProblemProfile>(ID_Problem);

                var ciss = new List<Issue>();

                foreach (var t in problem.GetSamples())
                {
                    Write(new OutputText($"  Sample {t.Name}...", false));
                    TestOne(profile, t, $"sample {t.Name}", ciss);
                }

                foreach (var t in problem.GetTests())
                {
                    Write(new OutputText($"  Test {t.Name}...", false));
                    TestOne(profile, t, $"test {t.Name}", ciss);
                }

                pipe.Result.AddRange(ciss);

                Write(new OutputText("  Local judging: ", false));

                if (!IsPass(ciss, indent: "    "))
                    throw new Exception("Local judging checking failed.");

                return problem;
            });
        }
    }
}
