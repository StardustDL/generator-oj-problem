namespace gop.Problems
{
    public class ProblemProfile
    {
        public string Name { get; set; }

        public string Author { get; set; }

        public uint TimeLimit { get; set; }

        public uint MemoryLimit { get; set; }

        public string[] StdRun { get; set; }
    }
}