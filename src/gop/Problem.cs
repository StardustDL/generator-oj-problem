using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace gop
{
    public class Problem
    {
        public const string F_Config = "config.json", V_Name = "name", V_Author = "author", V_TimeLimit = "timeLimit", V_MemoryLimit = "memoryLimit", F_Description = "description.txt", F_Input = "input.txt", F_Output = "output.txt", F_Hint = "hint.txt", F_StandardProgram = "std.cpp", D_Test = "tests", D_Sample = "samples", E_Input = ".in", E_Output = ".out";

        public string Name { get; set; }

        public string Author { get; set; }

        public uint TimeLimit { get; set; }

        public uint MemoryLimit { get; set; }

        public string Description { get; set; }

        public string Input { get; set; }

        public string Output { get; set; }

        public string Hint { get; set; }

        public string StandardProgram { get; set; }

        public List<TestCase> SampleData { get; set; }

        public List<TestCase> TestData { get; set; }

        public static Problem Load(string path)
        {
            var result = new Problem() { SampleData = new List<TestCase>(), TestData = new List<TestCase>() };

            var config = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(Path.Join(path, F_Config), Encoding.UTF8));
            result.Name = config[V_Name].ToString();
            result.Author = config[V_Author].ToString();
            result.TimeLimit = uint.Parse(config[V_TimeLimit].ToString());
            result.MemoryLimit = uint.Parse(config[V_MemoryLimit].ToString());

            result.Description = File.ReadAllText(Path.Join(path, F_Description), Encoding.UTF8);
            result.Input = File.ReadAllText(Path.Join(path, F_Input), Encoding.UTF8);
            result.Output = File.ReadAllText(Path.Join(path, F_Output), Encoding.UTF8);
            result.Hint = File.ReadAllText(Path.Join(path, F_Hint), Encoding.UTF8);
            result.StandardProgram = File.ReadAllText(Path.Join(path, F_StandardProgram), Encoding.UTF8);

            foreach (string infile in Directory.GetFiles(Path.Join(path, D_Sample), "test*.in"))
            {
                string item = Path.GetFileNameWithoutExtension(infile).Substring(4);
                string outfile = Path.Join(Path.GetDirectoryName(infile), $"test{item}.out");
                result.SampleData.Add(new TestCase { Name = item, Input = File.ReadAllText(infile, Encoding.UTF8), Output = File.ReadAllText(outfile, Encoding.UTF8) });
            }

            foreach (string infile in Directory.GetFiles(Path.Join(path, D_Test), "test*.in"))
            {
                string item = Path.GetFileNameWithoutExtension(infile).Substring(4);
                string outfile = Path.Join(Path.GetDirectoryName(infile), $"test{item}.out");
                result.TestData.Add(new TestCase { Name = item, Input = File.ReadAllText(infile, Encoding.UTF8), Output = File.ReadAllText(outfile, Encoding.UTF8) });
            }

            return result;
        }

        public static void Initialize(string path)
        {
            File.WriteAllText(Path.Join(path, F_Config), JsonConvert.SerializeObject(new Dictionary<string, string>
            {
                [V_Name] = "",
                [V_Author] = "",
                [V_TimeLimit] = "1",
                [V_MemoryLimit] = "128",
            }, Formatting.Indented), Encoding.UTF8);

            File.WriteAllText(Path.Join(path, F_Description), "题目描述", Encoding.UTF8);
            File.WriteAllText(Path.Join(path, F_Hint), "提示", Encoding.UTF8);
            File.WriteAllText(Path.Join(path, F_Input), "输入描述", Encoding.UTF8);
            File.WriteAllText(Path.Join(path, F_Output), "输出描述", Encoding.UTF8);
            File.WriteAllText(Path.Join(path, F_StandardProgram), "// 标准程序", Encoding.UTF8);

            string samples = Path.Join(path, D_Sample);
            Directory.CreateDirectory(samples);
            File.WriteAllText(Path.Join(samples, "test0.in"), "样例输入", Encoding.UTF8);
            File.WriteAllText(Path.Join(samples, "test0.out"), "样例输出", Encoding.UTF8);

            string tests = Path.Join(path, D_Test);
            Directory.CreateDirectory(tests);
        }
    }

    public class TestCase
    {
        public string Name { get; set; }

        public string Input { get; set; }

        public string Output { get; set; }
    }
}