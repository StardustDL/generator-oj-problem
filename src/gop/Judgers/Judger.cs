using gop;
using gop.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace gop.Judgers
{
    public enum JudgeState
    {
        Pending,
        Judging,
        Accept,
        WrongAnswer,
        TimeLimitError,
        MemoryLimitError,
        RuntimeError,
        SystemError,
    }

    public class JudgeResult
    {
        public JudgeState State { get; set; }

        public List<Issue> Issues { get; set; }

        public string[] Outputs { get; set; }
    }

    public static class Judger
    {
        public static JudgeResult Judge(string name, string[] executor, TimeSpan timelimit, long memoryLimit, string input, string[] standardOutput)
        {
            JudgeResult res = new JudgeResult
            {
                State = JudgeState.Pending,
                Issues = new List<Issue>(),
                Outputs = null
            };
            try
            {
                using (var runner = new Runner(new System.Diagnostics.ProcessStartInfo(executor[0], string.Join(" ", executor.Skip(1))))
                {
                    TimeLimit = timelimit,
                    MemoryLimit = memoryLimit,
                    Input = input,
                })
                {
                    runner.Run();
                    switch (runner.State)
                    {
                        case RunnerState.Ended:
                            {
                                if (runner.ExitCode != 0)
                                {
                                    res.Issues.Add(new Issue(IssueLevel.Error, $"Runtime error for {name}: exited with {runner.ExitCode}."));
                                    res.State = JudgeState.RuntimeError;
                                    break;
                                }
                                var expected = standardOutput.Select(x => x.TrimEnd('\r', '\n')).ToList();
                                var real = runner.Output.Select(x => x.TrimEnd('\r', '\n')).ToList();
                                var diff = TextIO.Diff(expected, real);
                                res.Outputs = real.ToArray();
                                if (diff.Count != 0)
                                {
                                    foreach (var s in diff)
                                        res.Issues.Add(new Issue(IssueLevel.Warning, $"Diff for {name}: {s}"));
                                    res.Issues.Add(new Issue(IssueLevel.Error, $"Wrong answer for {name}."));
                                    res.State = JudgeState.WrongAnswer;
                                }
                                else
                                {
                                    if (timelimit.TotalSeconds / runner.RunningTime.TotalSeconds < 2)
                                        res.Issues.Add(new Issue(IssueLevel.Warning, $"The time limit is too thin for {name}. It used {runner.RunningTime.TotalSeconds} seconds"));
                                    if ((double)memoryLimit / runner.MaximumMemory < 2)
                                        res.Issues.Add(new Issue(IssueLevel.Warning, $"The memory limit is too thin for {name}. It used {runner.MaximumMemory} bytes"));
                                    res.State = JudgeState.Accept;
                                }
                                break;
                            }
                        case RunnerState.OutOfMemory:
                            {
                                var message = $"Used {runner.MaximumMemory} bytes, limit {memoryLimit} bytes.";
                                res.Issues.Add(new Issue(IssueLevel.Error, $"Memory limit error for {name}. {message}"));
                                res.State = JudgeState.MemoryLimitError;
                                break;
                            }
                        case RunnerState.OutOfTime:
                            {
                                var message = $"Used {runner.RunningTime.TotalSeconds} seconds, limit {timelimit.TotalSeconds} seconds.";
                                res.Issues.Add(new Issue(IssueLevel.Error, $"Time limit error for {name}. {message}"));
                                res.State = JudgeState.TimeLimitError;
                                break;
                            }
                        default:
                            throw new Exception("The program doesn't stop.");
                    }
                }
            }
            catch (Exception ex)
            {
                res.Issues.Add(new Issue(IssueLevel.Error, $"System error for {name} with {ex.Message}."));
                res.State = JudgeState.SystemError;
            }
            return res;
        }
    }
}
