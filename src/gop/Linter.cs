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
                yield return new Issue(IssueLevel.Error, "The description of the problem is missing.");
            if (!File.Exists(problem.Input) || String.IsNullOrWhiteSpace(ReadAll(problem.Input)))
                yield return new Issue(IssueLevel.Error, "The input description of the problem is missing.");
            if (!File.Exists(problem.Output) || String.IsNullOrWhiteSpace(ReadAll(problem.Output)))
                yield return new Issue(IssueLevel.Error, "The output description of the problem is missing.");
            if (!File.Exists(problem.StandardProgram) || String.IsNullOrWhiteSpace(ReadAll(problem.StandardProgram)))
                yield return new Issue(IssueLevel.Error, "The standard program of the problem is missing.");
        }

        public static IEnumerable<Issue> TestCase(TestCasePath test)
        {
            var fi = new FileInfo(test.InputFile);
            if (!fi.Exists)
                yield return new Issue(IssueLevel.Error, "The input data is missing.");
            else if (fi.Length == 0)
                yield return new Issue(IssueLevel.Warning, "The input data is empty.");
            var fo = new FileInfo(test.OutputFile);
            if (!fo.Exists)
                yield return new Issue(IssueLevel.Error, "The output data is missing.");
            else if (fo.Length == 0)
                yield return new Issue(IssueLevel.Warning, "The output data is empty.");
        }
    }
}