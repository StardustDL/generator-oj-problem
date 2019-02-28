using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace gop
{
    class Program
    {
        static int Main(string[] args)
        {
            // Add them to the root command
            var rootCommand = new RootCommand();
            rootCommand.Description = "A CLI tool to generate Online-Judge Problem. Powered by StardustDL. Source codes at https://github.com/StardustDL/generator-oj-problem";

            var initCommand = new Command("init");
            initCommand.Handler = CommandHandler.Create(() => { init(); });

            var checkCommand = new Command("check");
            checkCommand.Handler = CommandHandler.Create(() => { check(); });

            var packCommand = new Command("pack");
            packCommand.Handler = CommandHandler.Create(() => { pack(); });

            var previewCommand = new Command("preview");
            previewCommand.Handler = CommandHandler.Create(() => { preview(); });

            rootCommand.Add(initCommand);
            rootCommand.Add(packCommand);
            rootCommand.Add(checkCommand);
            rootCommand.Add(previewCommand);

            // Parse the incoming args and invoke the handler
            return rootCommand.InvokeAsync(args).Result;
        }

        static Problem load()
        {
            Console.Write("正在加载题目...");
            Problem problem = null;
            try
            {
                problem = Problem.Load(Directory.GetCurrentDirectory());
                Console.WriteLine("加载成功。");
            }
            catch (Exception ex)
            {
                Console.WriteLine("加载失败。");
                Console.WriteLine($"发生错误：{ex.Message}。");

            }
            return problem;
        }

        static void init()
        {
            try
            {
                Problem.Initialize(Directory.GetCurrentDirectory());
                Console.WriteLine("初始化题目成功。");
            }
            catch (Exception ex)
            {
                Console.WriteLine("初始化题目失败。");
                Console.WriteLine($"发生错误：{ex.Message}。");
            }
        }

        static void preview()
        {
            var problem = load();

            if (problem == null) return;

            Console.WriteLine($"题目名：{problem.Name}");
            Console.WriteLine($"作者：{problem.Author}");
            Console.WriteLine($"时间限制：{problem.TimeLimit} 秒");
            Console.WriteLine($"空间限制：{problem.MemoryLimit} MB");
            Console.WriteLine();
            Console.WriteLine($"题目描述：{problem.Description}");
            Console.WriteLine();
            Console.WriteLine($"输入描述：{problem.Input}");
            Console.WriteLine();
            Console.WriteLine($"输出描述：{problem.Output}");
            Console.WriteLine();
            Console.WriteLine($"提示：{problem.Hint}");
            Console.WriteLine();
            Console.WriteLine($"有 {problem.SampleData.Count} 个样例数据，{problem.TestData.Count} 个测试数据。");
        }

        static void check()
        {
            var problem = load();

            if (problem == null) return;

            foreach (var message in Linter.Lint(problem))
            {
                switch (message.Level)
                {
                    case MessageLevel.Error:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("[Error] ");
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    case MessageLevel.Warning:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write("[Warning] ");
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    case MessageLevel.Info:
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.Write("[Info] ");
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                }
                Console.WriteLine(message.Content);
            }
        }

        static void pack()
        {
            var problem = load();

            if (problem == null) return;

            if (Linter.Lint(problem).Any(x => x.Level == MessageLevel.Error))
            {
                Console.WriteLine("题目信息缺失或格式有误，请使用 check 命令检查。");
                return;
            }

            {
                Console.Write("正在打包题目...");
                try
                {
                    string path = Directory.GetCurrentDirectory();

                    ZipFile.CreateFromDirectory(Path.Join(path, Problem.D_Test), Path.Join(path, "test.zip"), CompressionLevel.Fastest, false);

                    string outpath = Path.Join(Path.GetDirectoryName(path), "package.zip");

                    ZipFile.CreateFromDirectory(path, outpath, CompressionLevel.Fastest, false);

                    Console.WriteLine($"打包成功。");
                    Console.WriteLine($"请提交此文件： {outpath}");
                }
                catch (Exception ex)
                {
                    Console.Write("打包失败。");
                    Console.WriteLine($"发生错误：{ex.Message}。");
                }
            }
        }
    }
}
