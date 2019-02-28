using System;
using System.Collections.Generic;

namespace gop
{
    public enum MessageLevel
    {
        Info,
        Warning,
        Error
    }

    public class Message
    {
        public Message(MessageLevel level, string content)
        {
            this.Level = level;
            this.Content = content;
        }

        public MessageLevel Level { get; private set; }

        public string Content { get; private set; }
    }

    public static class Linter
    {
        public static IEnumerable<Message> Lint(Problem problem)
        {
            if (String.IsNullOrWhiteSpace(problem.Name))
            {
                yield return new Message(MessageLevel.Error, "缺少题目名称。");
            }

            if (String.IsNullOrWhiteSpace(problem.Author))
            {
                yield return new Message(MessageLevel.Error, "缺少题目作者。");
            }

            if (problem.TimeLimit == 0)
            {
                yield return new Message(MessageLevel.Error, "时间限制不能为 0 。");
            }

            if (problem.MemoryLimit == 0)
            {
                yield return new Message(MessageLevel.Error, "空间限制不能为 0 。");
            }

            if (String.IsNullOrWhiteSpace(problem.Description))
            {
                yield return new Message(MessageLevel.Error, "缺少题目描述。");
            }

            if (String.IsNullOrWhiteSpace(problem.Input))
            {
                yield return new Message(MessageLevel.Error, "缺少输入描述。");
            }

            if (String.IsNullOrWhiteSpace(problem.Output))
            {
                yield return new Message(MessageLevel.Error, "缺少输出描述。");
            }

            if (String.IsNullOrWhiteSpace(problem.StandardProgram))
            {
                yield return new Message(MessageLevel.Error, "缺少标准程序。");
            }

            switch (problem.SampleData.Count)
            {
                case 0:
                    yield return new Message(MessageLevel.Error, "缺少样例数据。");
                    break;
                case 1:
                    break;
                default:
                    yield return new Message(MessageLevel.Warning, "有多于一个样例数据，将仅使用第一个。");
                    break;
            }

            switch (problem.TestData.Count)
            {
                case 0:
                    yield return new Message(MessageLevel.Error, "缺少测试数据。");
                    break;
            }
        }
    }
}