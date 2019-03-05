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
        public Issue(IssueLevel level, string content,string addition = "")
        {
            Level = level;
            Content = content;
            Addition = addition;
        }

        public IssueLevel Level { get; private set; }

        public string Content { get; private set; }

        public string Addition { get; private set; }
    }
}