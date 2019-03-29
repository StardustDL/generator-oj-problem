using gop.Helpers;
using gop.Problems;
using Markdig;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using static gop.Helpers.ConsoleUI;
using static gop.Helpers.TextIO;
using PPipeline = gop.Adapters.Pipeline<gop.Problems.ProblemPath, string>;

namespace gop.Adapters.Generic
{
    public static class Packer
    {
        const string Token = "Generic.Packer";
        public const string LogCategory = "Packer";

        #region Paths

        public static readonly string PF_ProblemConfig = "profile.json", PF_PackageConfig = "package.json", PF_Tests = "tests.zip", PF_Extra = "extra.zip";
        public static readonly string PD_Descriptions = "decriptions", PD_SourceCode = "src", PD_Log = "log", PD_Samples = "samples";
        public static readonly string PF_StandardProgram = Path.Join(PD_SourceCode, "std.cpp"), PF_Issues = Path.Join(PD_Log, "issues.json"), PF_Description = Path.Join(PD_Descriptions, "description.txt"), PF_Hint = Path.Join(PD_Descriptions, "hint.txt"), PF_Input = Path.Join(PD_Descriptions, "input.txt"), PF_Output = Path.Join(PD_Descriptions, "output.txt"), PF_Source = Path.Join(PD_Descriptions, "source.txt"), PF_Log = Path.Join(PD_Log, "log.json");

        #endregion

        public static PPipeline UseInitial(this PPipeline pipeline, PackageProfile package)
        {
            pipeline.SetToken(Token);
            pipeline.Result = null;
            package.Platform = "generic";
            return pipeline.UseCreate(package);
        }

        public static PPipeline UseDefaultMarkdown(this PPipeline pipeline, PackageProfile package)
        {
            return pipeline.UseInitial(package).UseMetadata().UseDescriptionsMarkdown().UseSamples().UseSourceCode().UseTests();
        }

        public static PPipeline UseDefaultPlainText(this PPipeline pipeline, PackageProfile package)
        {
            return pipeline.UseInitial(package).UseMetadata().UseDescriptionsPlainText().UseSamples().UseSourceCode().UseTests();
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

                string _file = $"{package.Platform}-{config.Author}-{config.Name}" + (package.Checked ? "" : "-unchecked") + ".zip";
                string filename = Path.Join(problem.Target, _file);
                if (File.Exists(filename)) File.Delete(filename);

                pipe.Container.Set(filename);
                pipe.Container.Set(ZipFile.Open(filename, ZipArchiveMode.Create, UTF8WithoutBOM));

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

                var arc = pipe.Container.Get<ZipArchive>();

                if (pipe.Container.TryGet<List<Issue>>(out var lints))
                {
                    Write(new OutputText("    Add issues for log...", true));
                    var lintEntry = arc.CreateEntry(PF_Issues);
                    using (StreamWriter sw = new StreamWriter(lintEntry.Open(), UTF8WithoutBOM))
                        sw.WriteLine(JsonConvert.SerializeObject(lints, Formatting.Indented));
                }

                logger?.Info("Ended", LogCategory);

                if (pipe.Container.TryGet<Logger>(out var logs))
                {
                    Write(new OutputText("    Add logs...", true));
                    var logEntry = arc.CreateEntry(PF_Log);
                    using (StreamWriter sw = new StreamWriter(logEntry.Open(), UTF8WithoutBOM))
                        sw.WriteLine(JsonConvert.SerializeObject(logs.All(), Formatting.Indented));
                }

                arc.Dispose();

                pipe.Result = pipe.Container.Get<string>();
                
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
                arc.CreateEntryFromFile(problem.Profile, PF_ProblemConfig);

                logger?.Info("Ended", LogCategory);

                return problem;
            });
        }

        public static PPipeline UseDescriptionsMarkdown(this PPipeline pipeline)
        {
            const string LogCategory = Packer.LogCategory + ".DescriptionsMarkdown";

            return pipeline.Use((pipe, problem) =>
            {
                Write(new OutputText("  Copy descriptions...", true));
                var logger = pipe.Logger;
                logger?.Info("Starting", LogCategory);

                var arc = pipe.Container.Get<ZipArchive>();

                var descriptionEntry = arc.CreateEntry(PF_Description);
                using (StreamWriter sw = new StreamWriter(descriptionEntry.Open(), UTF8WithoutBOM))
                    sw.WriteLine(Markdown.ToHtml(ReadAll(problem.Description)));

                var inputEntry = arc.CreateEntry(PF_Input);
                using (StreamWriter sw = new StreamWriter(inputEntry.Open(), UTF8WithoutBOM))
                    sw.WriteLine(Markdown.ToHtml(ReadAll(problem.Input)));

                var outputEntry = arc.CreateEntry(PF_Output);
                using (StreamWriter sw = new StreamWriter(outputEntry.Open(), UTF8WithoutBOM))
                    sw.WriteLine(Markdown.ToHtml(ReadAll(problem.Output)));

                var hintEntry = arc.CreateEntry(PF_Hint);
                using (StreamWriter sw = new StreamWriter(hintEntry.Open(), UTF8WithoutBOM))
                    sw.WriteLine(Markdown.ToHtml(ReadAll(problem.Hint)));

                var sourceEntry = arc.CreateEntry(PF_Source);
                using (StreamWriter sw = new StreamWriter(sourceEntry.Open(), UTF8WithoutBOM))
                    sw.WriteLine(ReadAll(problem.Source));

                logger?.Info("Ended", LogCategory);

                return problem;
            });
        }

        public static PPipeline UseDescriptionsPlainText(this PPipeline pipeline)
        {
            const string LogCategory = Packer.LogCategory + ".DescriptionsPlainText";

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

                var sourceEntry = arc.CreateEntry(PF_Source);
                using (StreamWriter sw = new StreamWriter(sourceEntry.Open(), UTF8WithoutBOM))
                    sw.WriteLine(ReadAll(problem.Source));

                logger?.Info("Ended", LogCategory);

                return problem;
            });
        }

        public static PPipeline UseSourceCode(this PPipeline pipeline)
        {
            const string LogCategory = Packer.LogCategory + ".SourceCode";

            return pipeline.Use((pipe, problem) =>
            {
                Write(new OutputText("  Copy source codes...", true));

                var logger = pipe.Logger;
                logger?.Info("Starting", LogCategory);

                var arc = pipe.Container.Get<ZipArchive>();
                arc.CreateEntryFromFile(problem.StandardProgram, PF_StandardProgram);

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

                foreach (var u in problem.GetSamples())
                {
                    var inEntry = arc.CreateEntry(Path.Join(PD_Samples, ProblemPath.GetTestInput(u.Name)));
                    using (StreamWriter sw = new StreamWriter(inEntry.Open(), UTF8WithoutBOM))
                    using (StreamReader sr = File.OpenText(u.InputFile))
                        ConvertToLF(sr, sw);
                    var outEntry = arc.CreateEntry(Path.Join(PD_Samples, ProblemPath.GetTestOutput(u.Name)));
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

                string tests = Path.Join(problem.Temp, PF_Tests);
                if (File.Exists(tests)) File.Delete(tests);
                using (var testarc = ZipFile.Open(tests, ZipArchiveMode.Create, UTF8WithoutBOM))
                {
                    foreach (var u in problem.GetTests())
                    {
                        var inEntry = testarc.CreateEntry(ProblemPath.GetTestInput(u.Name));
                        using (StreamWriter sw = new StreamWriter(inEntry.Open(), UTF8WithoutBOM))
                        using (StreamReader sr = File.OpenText(u.InputFile))
                            ConvertToLF(sr, sw);
                        var outEntry = testarc.CreateEntry(ProblemPath.GetTestOutput(u.Name));
                        using (StreamWriter sw = new StreamWriter(outEntry.Open(), UTF8WithoutBOM))
                        using (StreamReader sr = File.OpenText(u.OutputFile))
                            ConvertToLF(sr, sw);
                    }
                }

                arc.CreateEntryFromFile(tests, PF_Tests);

                logger?.Info("Ended", LogCategory);
                return problem;
            });
        }

        public static PPipeline UseExtra(this PPipeline pipeline)
        {
            const string LogCategory = Packer.LogCategory + ".Extra";

            return pipeline.Use((pipe, problem) =>
            {
                Write(new OutputText("  Copy extra...", true));
                var logger = pipe.Logger;
                logger?.Info("Starting", LogCategory);

                var arc = pipe.Container.Get<ZipArchive>();

                string data = Path.Join(problem.Temp, PF_Extra);
                ZipFile.CreateFromDirectory(problem.Extra, data, CompressionLevel.Fastest, false, UTF8WithoutBOM);
                arc.CreateEntryFromFile(data, PF_Extra);

                logger?.Info("Ended", LogCategory);

                return problem;
            });
        }

        public static PPipeline UseIssues(this PPipeline pipeline, List<Issue> lints)
        {
            return pipeline.Use((pipe, problem) =>
            {
                pipe.Container.Set(lints);

                return problem;
            });
        }

        public static PPipeline UseLogger(this PPipeline pipeline, Logger logger)
        {
            return pipeline.Use((pipe, problem) =>
            {
                pipe.Logger = logger;
                return problem;
            });
        }

        public static PPipeline UsePackage(this PPipeline pipeline)
        {
            const string LogCategory = Packer.LogCategory + ".Package";

            return pipeline.Use((pipe, problem) =>
            {
                Write(new OutputText("  Add package metadata...", true));
                var logger = pipe.Logger;
                logger?.Info("Starting", LogCategory);

                var arc = pipe.Container.Get<ZipArchive>();

                var packageEntry = arc.CreateEntry(PF_PackageConfig);
                using (StreamWriter sw = new StreamWriter(packageEntry.Open(), UTF8WithoutBOM))
                    sw.WriteLine(JsonConvert.SerializeObject(pipe.Container.Get<PackageProfile>(), Formatting.Indented));

                logger?.Info("Ended", LogCategory);

                return problem;
            });
        }
    }
}
