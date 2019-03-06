using gop.Helpers;
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

        public static PPipeline UseConfig(this PPipeline pipeline)
        {
            return pipeline.Use((pipe, problem) =>
            {
                WriteInfo(new OutputText("Checking ", false));
                Write(new OutputText("config...", false));
                var ciss = new List<Issue>();
                ProblemProfile config = null;
                if (!File.Exists(problem.Profile))
                {
                    var issue = new Issue(IssueLevel.Error, "The problem configuration information is not found.");
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
                    ciss.AddRange(Linter.Config(config));

                pipe.Result.AddRange(ciss);

                if (!IsPass(ciss, indent: "  "))
                    throw new Exception("Config checking failed.");

                return problem;
            });
        }

        public static PPipeline UseDescriptions(this PPipeline pipeline)
        {
            return pipeline.Use((pipe, problem) =>
            {
                WriteInfo(new OutputText("Checking ", false));
                Write(new OutputText("description...", false));
                var ciss = new List<Issue>(Linter.Description(problem));

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
    }
}
