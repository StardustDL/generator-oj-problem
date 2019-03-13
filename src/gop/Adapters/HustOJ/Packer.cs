using gop.Adapters.Generic;
using gop.Helpers;
using PPipeline = gop.Adapters.Pipeline<gop.Problems.ProblemPath, string>;
using static gop.Helpers.ConsoleUI;
using static gop.Helpers.TextIO;
using System.IO.Compression;
using System.IO;
using System.Xml;
using gop.Problems;
using Markdig;

namespace gop.Adapters.HustOJ
{
    public static class Packer
    {
        const string Token = "HustOJ.Packer";
        public const string LogCategory = "HustOJ.Packer";

        #region Paths

        public static readonly string PF_Release = "release.xml";

        #endregion

        public static PPipeline UseInitial(this PPipeline pipeline, PackageProfile package)
        {
            pipeline.SetToken(Token);
            pipeline.Result = null;
            package.Platform = "hustoj";
            return pipeline.UseCreate(package);
        }

        public static PPipeline UseFPS(this PPipeline pipeline)
        {
            const string LogCategory = Packer.LogCategory + ".FPS";

            return pipeline.Use((pipe, problem) =>
            {
                Write(new OutputText("  Create FPS data...", true));
                pipe.Container.TryGet<Logger>(out var logger);
                logger?.Info("Starting", LogCategory);

                var arc = pipe.Container.Get<ZipArchive>();
                var config = pipe.Container.Get<ProblemProfile>();

                var releaseEntry = arc.CreateEntry(PF_Release);

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

                using (StreamWriter sw = new StreamWriter(releaseEntry.Open(), UTF8WithoutBOM))
                    xml.Save(sw);

                logger?.Info("Ended", LogCategory);

                return problem;
            });
        }

        public static PPipeline UseDefault(this PPipeline pipeline, PackageProfile package)
        {
            return pipeline.UseInitial(package).UseMetadata().UseSourceCode().UseFPS();
        }
    }
}
