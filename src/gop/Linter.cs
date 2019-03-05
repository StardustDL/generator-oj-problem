using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace gop
{
    public static class Linter
    {
        static string ReadAll(string path)
        {
            return File.ReadAllText(path, Encoding.UTF8);
        }

        public static IEnumerable<Issue> Config(ProblemConfig config)
        {
            if (String.IsNullOrWhiteSpace(config.Name))
                yield return new Issue(IssueLevel.Error, "The name of the problem is missing.");
            if (String.IsNullOrWhiteSpace(config.Author))
                yield return new Issue(IssueLevel.Error, "The author of the problem is missing.");
            if (config.TimeLimit == 0)
                yield return new Issue(IssueLevel.Error, "The time limit cannot be 0.");
            if (config.MemoryLimit == 0)
                yield return new Issue(IssueLevel.Error, "The memory limit cannot be 0.");
        }

        public static IEnumerable<Issue> Description(ProblemPath problem)
        {
            if (!File.Exists(problem.Description) || String.IsNullOrWhiteSpace(ReadAll(problem.Description)))
                yield return new Issue(IssueLevel.Error, "The description of the problem is missing.", problem.Description);
            if (!File.Exists(problem.Input) || String.IsNullOrWhiteSpace(ReadAll(problem.Input)))
                yield return new Issue(IssueLevel.Error, "The input description of the problem is missing.", problem.Input);
            if (!File.Exists(problem.Output) || String.IsNullOrWhiteSpace(ReadAll(problem.Output)))
                yield return new Issue(IssueLevel.Error, "The output description of the problem is missing.", problem.Output);
            if (!File.Exists(problem.Source) || String.IsNullOrWhiteSpace(ReadAll(problem.Source)))
                yield return new Issue(IssueLevel.Error, "The source of the problem is missing. If it's original, write original.", problem.Source);
            if (!File.Exists(problem.Hint) || String.IsNullOrWhiteSpace(ReadAll(problem.Hint)))
                yield return new Issue(IssueLevel.Info, "The hint of the problem is missing.", problem.Hint);
            if (!File.Exists(problem.StandardProgram) || String.IsNullOrWhiteSpace(ReadAll(problem.StandardProgram)))
                yield return new Issue(IssueLevel.Error, "The standard program of the problem is missing.", problem.StandardProgram);
        }

        public static IEnumerable<Issue> TestCase(TestCasePath test)
        {
            var fi = new FileInfo(test.InputFile);
            if (!fi.Exists)
                yield return new Issue(IssueLevel.Error, $"The input data of {test.Name} is missing.", test.InputFile);
            else if (fi.Length == 0)
                yield return new Issue(IssueLevel.Warning, $"The input data of {test.Name} is empty.", test.InputFile);
            var fo = new FileInfo(test.OutputFile);
            if (!fo.Exists)
                yield return new Issue(IssueLevel.Error, $"The output data of {test.Name} is missing.", test.OutputFile);
            else if (fo.Length == 0)
                yield return new Issue(IssueLevel.Warning, $"The output data of {test.Name} is empty.", test.OutputFile);
        }
    }
}