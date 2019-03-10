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
        #region Paths

        public static readonly string PF_ProblemConfig = "profile.json", PF_PackageConfig = "package.json", PF_Tests = "tests.zip", PF_Extra = "extra.zip";
        public static readonly string PD_Descriptions = "decriptions", PD_SourceCode = "src", PD_Log = "log", PD_Samples = "samples";
        public static readonly string PF_StandardProgram = Path.Join(PD_SourceCode, "std.cpp"), PF_Issues = Path.Join(PD_Log, "issues.json"), PF_Description = Path.Join(PD_Descriptions, "description.txt"), PF_Hint = Path.Join(PD_Descriptions, "hint.txt"), PF_Input = Path.Join(PD_Descriptions, "input.txt"), PF_Output = Path.Join(PD_Descriptions, "output.txt"), PF_Source = Path.Join(PD_Descriptions, "source.txt");

        #endregion

        public static PPipeline UseCreate(this PPipeline pipeline, PackageProfile package)
        {
            return pipeline.Use((pipe, problem) =>
            {
                WriteInfo(new OutputText("Packing the problem...", true));
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

                return problem;
            });
        }

        public static PPipeline UseSave(this PPipeline pipeline)
        {
            return pipeline.Use((pipe, problem) =>
            {
                Write(new OutputText("  Saving...", true));

                var arc = pipe.Container.Get<ZipArchive>();
                arc.Dispose();

                pipe.Result = pipe.Container.Get<string>();
                return problem;
            });
        }

        public static PPipeline UseMetadata(this PPipeline pipeline)
        {
            return pipeline.Use((pipe, problem) =>
            {
                Write(new OutputText("  Copy problem metadata...", true));
                var arc = pipe.Container.Get<ZipArchive>();
                arc.CreateEntryFromFile(problem.Profile, PF_ProblemConfig);

                return problem;
            });
        }

        public static PPipeline UseDescriptionsMarkdown(this PPipeline pipeline)
        {
            return pipeline.Use((pipe, problem) =>
            {
                Write(new OutputText("  Copy descriptions...", true));
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

                return problem;
            });
        }

        public static PPipeline UseDescriptionsPlainText(this PPipeline pipeline)
        {
            return pipeline.Use((pipe, problem) =>
            {
                Write(new OutputText("  Copy descriptions...", true));
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

                return problem;
            });
        }

        public static PPipeline UseSourceCode(this PPipeline pipeline)
        {
            return pipeline.Use((pipe, problem) =>
            {
                Write(new OutputText("  Copy source codes...", true));
                var arc = pipe.Container.Get<ZipArchive>();
                arc.CreateEntryFromFile(problem.StandardProgram, PF_StandardProgram);

                return problem;
            });
        }

        public static PPipeline UseSamples(this PPipeline pipeline)
        {
            return pipeline.Use((pipe, problem) =>
            {
                Write(new OutputText("  Copy samples...", true));
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

                return problem;
            });
        }

        public static PPipeline UseTests(this PPipeline pipeline)
        {
            return pipeline.Use((pipe, problem) =>
            {
                Write(new OutputText("  Copy tests...", true));
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
                return problem;
            });
        }

        public static PPipeline UseExtra(this PPipeline pipeline)
        {
            return pipeline.Use((pipe, problem) =>
            {
                Write(new OutputText("  Copy extra...", true));
                var arc = pipe.Container.Get<ZipArchive>();

                string data = Path.Join(problem.Temp, PF_Extra);
                ZipFile.CreateFromDirectory(problem.Extra, data, CompressionLevel.Fastest, false, UTF8WithoutBOM);
                arc.CreateEntryFromFile(data, PF_Extra);

                return problem;
            });
        }

        public static PPipeline UseIssues(this PPipeline pipeline, List<Issue> lints)
        {
            return pipeline.Use((pipe, problem) =>
            {
                ConsoleUI.Write(new OutputText("  Add logs...", true));
                var arc = pipe.Container.Get<ZipArchive>();

                var lintEntry = arc.CreateEntry(PF_Issues);
                using (StreamWriter sw = new StreamWriter(lintEntry.Open(), UTF8WithoutBOM))
                    sw.WriteLine(JsonConvert.SerializeObject(lints, Formatting.Indented));

                return problem;
            });
        }

        public static PPipeline UsePackage(this PPipeline pipeline)
        {
            return pipeline.Use((pipe, problem) =>
            {
                Write(new OutputText("  Add package metadata...", true));
                var arc = pipe.Container.Get<ZipArchive>();

                var packageEntry = arc.CreateEntry(PF_PackageConfig);
                using (StreamWriter sw = new StreamWriter(packageEntry.Open(), UTF8WithoutBOM))
                    sw.WriteLine(JsonConvert.SerializeObject(pipe.Container.Get<PackageProfile>(), Formatting.Indented));

                return problem;
            });
        }
    }
}
