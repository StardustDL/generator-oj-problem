using gop.Helpers;
using gop.Problems;
using Markdig;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using static gop.Helpers.ConsoleUI;
using static gop.Helpers.TextIO;
using PPipeline = gop.Adapters.Pipeline<gop.Problems.ProblemPath, string>;

namespace gop.Adapters.LocalJudge
{
    public static class Packer
    {
        const string Token = "LocalJudge.Packer";
        public const string LogCategory = "LocalJudge.Packer";

        #region Paths

        public static readonly string PF_ProblemConfig = "profile.json", PF_Extra = "extra.zip";
        public static readonly string PD_Descriptions = "description", PD_Log = "log", PD_Samples = "samples", PD_Tests = "tests";
        public static readonly string PF_Description = Path.Join(PD_Descriptions, "description.md"), PF_Hint = Path.Join(PD_Descriptions, "hint.md"), PF_Input = Path.Join(PD_Descriptions, "input.md"), PF_Output = Path.Join(PD_Descriptions, "output.md");

        #endregion

        public static PPipeline UseInitial(this PPipeline pipeline, PackageProfile package)
        {
            pipeline.SetToken(Token);
            pipeline.Result = null;
            package.Platform = "localjudge";
            return pipeline.UseCreate(package);
        }

        public static PPipeline UseDefault(this PPipeline pipeline, PackageProfile package)
        {
            return pipeline.UseInitial(package).UseMetadata().UseDescriptions().UseSamples().UseTests();
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

                if (Directory.Exists(problem.Temp)) Directory.Delete(problem.Temp, true);
                Directory.CreateDirectory(problem.Temp);

                string _file = $"{package.Platform}-{config.Author}" + (package.Checked ? "" : "-unchecked") + ".zip";
                string filename = Path.Join(problem.Target, _file);
                if (File.Exists(filename)) File.Delete(filename);

                pipe.Container.Set(filename);
                pipe.Container.Set(ZipFile.Open(filename, ZipArchiveMode.Create, UTF8WithoutBOM));

                logger?.Info("Ended", LogCategory);

                return problem;
            });
        }

        public static PPipeline UseMetadata(this PPipeline pipeline)
        {
            const string LogCategory = Packer.LogCategory + ".Metadata";

            return pipeline.Use((pipe, problem) =>
            {
                Write(new OutputText("  Copy problem metadata...", true));
                var logger = pipe.Logger;
                logger?.Info("Starting", LogCategory);

                var arc = pipe.Container.Get<ZipArchive>();
                var profile = pipe.Container.Get<ProblemProfile>();

                var profileEntry = arc.CreateEntry(PF_ProblemConfig);
                using (StreamWriter sw = new StreamWriter(profileEntry.Open(), UTF8WithoutBOM))
                {
                    Dictionary<string, object> op = new Dictionary<string, object>
                    {
                        { "ID", "0" },
                        { "Name", profile.Name },
                        { "Author", profile.Author },
                        { "Source", ReadAll(problem.Source) }
                    };
                    sw.WriteLine(JsonConvert.SerializeObject(op, Formatting.Indented));
                }

                logger?.Info("Ended", LogCategory);

                return problem;
            });
        }

        public static PPipeline UseDescriptions(this PPipeline pipeline)
        {
            const string LogCategory = Packer.LogCategory + ".Descriptions";

            return pipeline.Use((pipe, problem) =>
            {
                Write(new OutputText("  Copy descriptions...", true));
                var logger = pipe.Logger;
                logger?.Info("Starting", LogCategory);

                var arc = pipe.Container.Get<ZipArchive>();

                var descriptionEntry = arc.CreateEntry(PF_Description);
                using (StreamWriter sw = new StreamWriter(descriptionEntry.Open(), UTF8WithoutBOM))
                    sw.WriteLine(ReadAll(problem.Description));

                var inputEntry = arc.CreateEntry(PF_Input);
                using (StreamWriter sw = new StreamWriter(inputEntry.Open(), UTF8WithoutBOM))
                    sw.WriteLine(ReadAll(problem.Input));

                var outputEntry = arc.CreateEntry(PF_Output);
                using (StreamWriter sw = new StreamWriter(outputEntry.Open(), UTF8WithoutBOM))
                    sw.WriteLine(ReadAll(problem.Output));

                var hintEntry = arc.CreateEntry(PF_Hint);
                using (StreamWriter sw = new StreamWriter(hintEntry.Open(), UTF8WithoutBOM))
                    sw.WriteLine(ReadAll(problem.Hint));

                logger?.Info("Ended", LogCategory);

                return problem;
            });
        }

        public static PPipeline UseSamples(this PPipeline pipeline)
        {
            const string LogCategory = Packer.LogCategory + ".Samples";

            return pipeline.Use((pipe, problem) =>
            {
                Write(new OutputText("  Copy samples...", true));
                var logger = pipe.Logger;
                logger?.Info("Starting", LogCategory);

                var arc = pipe.Container.Get<ZipArchive>();
                var profile = pipe.Container.Get<ProblemProfile>();

                foreach (var u in problem.GetSamples())
                {
                    var rt = Path.Join(PD_Samples, u.Name);
                    var proEntry = arc.CreateEntry(Path.Join(rt, "profile.json"));
                    using (StreamWriter sw = new StreamWriter(proEntry.Open(), UTF8WithoutBOM))
                    {
                        Dictionary<string, object> pro = new Dictionary<string, object>
                        {
                            {"ID",u.Name },
                            {"TimeLimit",TimeSpan.FromSeconds(profile.TimeLimit) },
                            {"MemoryLimit",profile.MemoryLimit*1024*1024 },
                        };
                        sw.WriteLine(JsonConvert.SerializeObject(pro, Formatting.Indented));
                    }

                    var inEntry = arc.CreateEntry(Path.Join(rt, "input.data"));
                    using (StreamWriter sw = new StreamWriter(inEntry.Open(), UTF8WithoutBOM))
                    using (StreamReader sr = File.OpenText(u.InputFile))
                        ConvertToLF(sr, sw);
                    var outEntry = arc.CreateEntry(Path.Join(rt, "output.data"));
                    using (StreamWriter sw = new StreamWriter(outEntry.Open(), UTF8WithoutBOM))
                    using (StreamReader sr = File.OpenText(u.OutputFile))
                        ConvertToLF(sr, sw);
                }

                logger?.Info("Ended", LogCategory);

                return problem;
            });
        }

        public static PPipeline UseTests(this PPipeline pipeline)
        {
            const string LogCategory = Packer.LogCategory + ".Tests";

            return pipeline.Use((pipe, problem) =>
            {
                Write(new OutputText("  Copy tests...", true));
                var logger = pipe.Logger;
                logger?.Info("Starting", LogCategory);

                var arc = pipe.Container.Get<ZipArchive>();
                var profile = pipe.Container.Get<ProblemProfile>();

                foreach (var u in problem.GetTests())
                {
                    var rt = Path.Join(PD_Tests, u.Name);
                    var proEntry = arc.CreateEntry(Path.Join(rt, "profile.json"));
                    using (StreamWriter sw = new StreamWriter(proEntry.Open(), UTF8WithoutBOM))
                    {
                        Dictionary<string, object> pro = new Dictionary<string, object>
                        {
                            {"ID",u.Name },
                            {"TimeLimit",TimeSpan.FromSeconds(profile.TimeLimit) },
                            {"MemoryLimit",profile.MemoryLimit*1024*1024 },
                        };
                        sw.WriteLine(JsonConvert.SerializeObject(pro, Formatting.Indented));
                    }

                    var inEntry = arc.CreateEntry(Path.Join(rt, "input.data"));
                    using (StreamWriter sw = new StreamWriter(inEntry.Open(), UTF8WithoutBOM))
                    using (StreamReader sr = File.OpenText(u.InputFile))
                        ConvertToLF(sr, sw);
                    var outEntry = arc.CreateEntry(Path.Join(rt, "output.data"));
                    using (StreamWriter sw = new StreamWriter(outEntry.Open(), UTF8WithoutBOM))
                    using (StreamReader sr = File.OpenText(u.OutputFile))
                        ConvertToLF(sr, sw);
                }

                logger?.Info("Ended", LogCategory);

                return problem;
            });
        }
    }
}
