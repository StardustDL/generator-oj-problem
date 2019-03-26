using gop.Helpers;
using gop.Problems;
using Markdig;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using static gop.Helpers.TextIO;
using static gop.Helpers.ConsoleUI;
using PPipeline = gop.Adapters.Pipeline<gop.Problems.ProblemPath, string>;

namespace gop.Adapters.FreeProblemSet
{
    public static class Packer
    {
        public static XmlDocument GenerateFPS(ProblemPath problem, ProblemProfile config)
        {
            var xml = new XmlDocument();
            xml.AppendChild(xml.CreateXmlDeclaration("1.0", "UTF-8", null));
            var root = xml.CreateElement("fps");
            root.SetAttribute("url", "https://github.com/zhblue/freeproblemset/");
            root.SetAttribute("version", "1.2");

            {
                var gen = xml.CreateElement("generator");
                gen.SetAttribute("name", "HUSTOJ");
                gen.SetAttribute("url", "https://github.com/zhblue/hustoj/");
                root.AppendChild(gen);
            }

            {
                var item = xml.CreateElement("item");
                {
                    var sub = xml.CreateElement("title");
                    sub.AppendChild(xml.CreateCDataSection(config.Name));
                    item.AppendChild(sub);
                }
                {
                    var sub = xml.CreateElement("time_limit");
                    sub.SetAttribute("unit", "s");
                    sub.AppendChild(xml.CreateCDataSection(config.TimeLimit.ToString()));
                    item.AppendChild(sub);
                }
                {
                    var sub = xml.CreateElement("memory_limit");
                    sub.SetAttribute("unit", "mb");
                    sub.AppendChild(xml.CreateCDataSection(config.MemoryLimit.ToString()));
                    item.AppendChild(sub);
                }
                {
                    var sub = xml.CreateElement("description");
                    sub.AppendChild(xml.CreateCDataSection(Markdown.ToHtml(ReadAll(problem.Description))));
                    item.AppendChild(sub);
                }
                {
                    var sub = xml.CreateElement("input");
                    sub.AppendChild(xml.CreateCDataSection(Markdown.ToHtml(ReadAll(problem.Input))));
                    item.AppendChild(sub);
                }
                {
                    var sub = xml.CreateElement("output");
                    sub.AppendChild(xml.CreateCDataSection(Markdown.ToHtml(ReadAll(problem.Output))));
                    item.AppendChild(sub);
                }
                {
                    foreach (var u in problem.GetSamples())
                    {
                        var subin = xml.CreateElement("sample_input");
                        using (StreamReader sr = File.OpenText(u.InputFile))
                            subin.AppendChild(xml.CreateCDataSection(ConvertToLF(sr)));
                        var subout = xml.CreateElement("sample_output");
                        using (StreamReader sr = File.OpenText(u.OutputFile))
                            subout.AppendChild(xml.CreateCDataSection(ConvertToLF(sr)));
                        item.AppendChild(subin);
                        item.AppendChild(subout);
                    }
                }
                {
                    foreach (var u in problem.GetTests())
                    {
                        var subin = xml.CreateElement("test_input");
                        using (StreamReader sr = File.OpenText(u.InputFile))
                            subin.AppendChild(xml.CreateCDataSection(ConvertToLF(sr)));
                        var subout = xml.CreateElement("test_output");
                        using (StreamReader sr = File.OpenText(u.OutputFile))
                            subout.AppendChild(xml.CreateCDataSection(ConvertToLF(sr)));
                        item.AppendChild(subin);
                        item.AppendChild(subout);
                    }
                }
                {
                    var sub = xml.CreateElement("hint");
                    sub.AppendChild(xml.CreateCDataSection(Markdown.ToHtml(ReadAll(problem.Hint))));
                    item.AppendChild(sub);
                }
                {
                    var sub = xml.CreateElement("source");
                    sub.AppendChild(xml.CreateCDataSection(ReadAll(problem.Source)));
                    item.AppendChild(sub);
                }
                {
                    var sub = xml.CreateElement("solution");
                    sub.SetAttribute("language", "C++");
                    sub.AppendChild(xml.CreateCDataSection(ReadAll(problem.StandardProgram)));
                    item.AppendChild(sub);
                }
                root.AppendChild(item);
            }
            xml.AppendChild(root);
            return xml;
        }

        public const string LogCategory = "FreeProblemSet.Packer";
        public const string Token = "FreeProblemSet.Packer";

        public static PPipeline UseInitial(this PPipeline pipeline, PackageProfile package)
        {
            pipeline.SetToken(Token);
            pipeline.Result = null;
            package.Platform = "freeproblemset";
            return pipeline.UseCreate(package);
        }

        public static PPipeline UseDefault(this PPipeline pipeline, PackageProfile package)
        {
            return pipeline.UseInitial(package).UseFPS();
        }

        public static PPipeline UseCreate(this PPipeline pipeline, PackageProfile package)
        {
            const string LogCategory = Packer.LogCategory + ".Create";

            return pipeline.Use((pipe, problem) =>
            {
                WriteInfo(new OutputText("Packing the problem...", true));
                var logger = pipe.Logger;
                logger?.Info("Starting", LogCategory);
                var config = problem.GetProfile();
                pipe.Container.Set(config);
                pipe.Container.Set(package);
                Directory.CreateDirectory(problem.Target);

                // if (Directory.Exists(problem.Temp)) Directory.Delete(problem.Temp, true);
                // Directory.CreateDirectory(problem.Temp);

                string _file = $"{package.Platform}-{config.Author}-{config.Name}" + (package.Checked ? "" : "-unchecked") + ".xml";
                string filename = Path.Join(problem.Target, _file);
                if (File.Exists(filename)) File.Delete(filename);

                pipe.Container.Set(filename);

                logger?.Info("Ended", LogCategory);

                return problem;
            });
        }

        public static PPipeline UseFPS(this PPipeline pipeline)
        {
            const string LogCategory = Packer.LogCategory + ".FPS";

            return pipeline.Use((pipe, problem) =>
            {
                Write(new OutputText("  Create FPS data...", true));
                var logger = pipe.Logger;
                logger?.Info("Starting", LogCategory);

                var outputFile = pipe.Container.Get<string>();
                var config = pipe.Container.Get<ProblemProfile>();

                var xml = GenerateFPS(problem, config);

                using (StreamWriter sw = new StreamWriter(outputFile, false, UTF8WithoutBOM))
                    xml.Save(sw);

                logger?.Info("Ended", LogCategory);

                return problem;
            });
        }

        public static PPipeline UseSave(this PPipeline pipeline)
        {
            const string LogCategory = Packer.LogCategory + ".Save";

            return pipeline.Use((pipe, problem) =>
            {
                Write(new OutputText("  Saving...", true));
                var logger = pipe.Logger;
                logger?.Info("Starting", LogCategory);

                pipe.Result = pipe.Container.Get<string>();

                logger?.Info("Ended", LogCategory);

                return problem;
            });
        }        
    }
}
