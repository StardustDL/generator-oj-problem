using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace gop
{
    public class TestCasePath
    {
        public string Name { get; set; }

        public string InputFile { get; set; }

        public string OutputFile { get; set; }
    }
}