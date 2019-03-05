namespace gop
{
    public enum IssueLevel
    {
        Info,
        Warning,
        Error
    }

    public class Issue
    {
        public Issue(IssueLevel level, string content)
        {
            this.Level = level;
            this.Content = content;
        }

        public IssueLevel Level { get; private set; }

        public string Content { get; private set; }
    }
}