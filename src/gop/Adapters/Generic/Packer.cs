﻿using gop.Helpers;
using gop.Problems;
using Markdig;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using static gop.Helpers.ConsoleUI;
using static gop.Helpers.TextIO;
using PPipeline = gop.Adapters.Pipeline<gop.Problems.ProblemPath, string>;

namespace gop.Adapters.Generic
{
    public static class Packer
    {
        public const string ID_Problem = "problem", ID_OutputFile = "output", ID_Archive = "archive", ID_Package = "package";

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
                var config = JsonConvert.DeserializeObject<ProblemProfile>(ReadAll(problem.Profile));
                pipe.SetFlag(ID_Problem, config);
                pipe.SetFlag(ID_Package, package);
                Directory.CreateDirectory(problem.Target);

                if (Directory.Exists(problem.Temp)) Directory.Delete(problem.Temp, true);
                Directory.CreateDirectory(problem.Temp);

                string _file = $"{package.Platform}-{config.Author}" + (package.Checked ? "" : "-unchecked") + ".zip";
                string filename = Path.Join(problem.Target, _file);
                if (File.Exists(filename)) File.Delete(filename);

                pipe.SetFlag(ID_OutputFile, filename);
                pipe.SetFlag(ID_Archive, ZipFile.Open(filename, ZipArchiveMode.Create, Encoding.UTF8));

                return problem;
            });
        }

        public static PPipeline UseSave(this PPipeline pipeline)
        {
            return pipeline.Use((pipe, problem) =>
            {
                Write(new OutputText("  Saving...", true));

                var arc = pipe.GetFlag<ZipArchive>(ID_Archive);
                arc.Dispose();

                pipe.Result = pipe.GetFlag<string>(ID_OutputFile);
                return problem;
            });
        }

        public static PPipeline UseMetadata(this PPipeline pipeline)
        {
            return pipeline.Use((pipe, problem) =>
            {
                Write(new OutputText("  Copy problem metadata...", true));
                var arc = pipe.GetFlag<ZipArchive>(ID_Archive);
                arc.CreateEntryFromFile(problem.Profile, PF_ProblemConfig);

                return problem;
            });
        }

        public static PPipeline UseDescriptionsMarkdown(this PPipeline pipeline)
        {
            return pipeline.Use((pipe, problem) =>
            {
                Write(new OutputText("  Copy descriptions...", true));
                var arc = pipe.GetFlag<ZipArchive>(ID_Archive);

                var descriptionEntry = arc.CreateEntry(PF_Description);
                using (StreamWriter sw = new StreamWriter(descriptionEntry.Open(), Encoding.UTF8))
                    sw.WriteLine(Markdown.ToHtml(ReadAll(problem.Description)));

                var inputEntry = arc.CreateEntry(PF_Input);
                using (StreamWriter sw = new StreamWriter(inputEntry.Open(), Encoding.UTF8))
                    sw.WriteLine(Markdown.ToHtml(ReadAll(problem.Input)));

                var outputEntry = arc.CreateEntry(PF_Output);
                using (StreamWriter sw = new StreamWriter(outputEntry.Open(), Encoding.UTF8))
                    sw.WriteLine(Markdown.ToHtml(ReadAll(problem.Output)));

                var hintEntry = arc.CreateEntry(PF_Hint);
                using (StreamWriter sw = new StreamWriter(hintEntry.Open(), Encoding.UTF8))
                    sw.WriteLine(Markdown.ToHtml(ReadAll(problem.Hint)));

                var sourceEntry = arc.CreateEntry(PF_Source);
                using (StreamWriter sw = new StreamWriter(sourceEntry.Open(), Encoding.UTF8))
                    sw.WriteLine(ReadAll(problem.Source));

                return problem;
            });
        }

        public static PPipeline UseDescriptionsPlainText(this PPipeline pipeline)
        {
            return pipeline.Use((pipe, problem) =>
            {
                Write(new OutputText("  Copy descriptions...", true));
                var arc = pipe.GetFlag<ZipArchive>(ID_Archive);

                var descriptionEntry = arc.CreateEntry(PF_Description);
                using (StreamWriter sw = new StreamWriter(descriptionEntry.Open(), Encoding.UTF8))
                    sw.WriteLine(ReadAll(problem.Description));

                var inputEntry = arc.CreateEntry(PF_Input);
                using (StreamWriter sw = new StreamWriter(inputEntry.Open(), Encoding.UTF8))
                    sw.WriteLine(ReadAll(problem.Input));

                var outputEntry = arc.CreateEntry(PF_Output);
                using (StreamWriter sw = new StreamWriter(outputEntry.Open(), Encoding.UTF8))
                    sw.WriteLine(ReadAll(problem.Output));

                var hintEntry = arc.CreateEntry(PF_Hint);
                using (StreamWriter sw = new StreamWriter(hintEntry.Open(), Encoding.UTF8))
                    sw.WriteLine(ReadAll(problem.Hint));

                var sourceEntry = arc.CreateEntry(PF_Source);
                using (StreamWriter sw = new StreamWriter(sourceEntry.Open(), Encoding.UTF8))
                    sw.WriteLine(ReadAll(problem.Source));

                return problem;
            });
        }

        public static PPipeline UseSourceCode(this PPipeline pipeline)
        {
            return pipeline.Use((pipe, problem) =>
            {
                Write(new OutputText("  Copy source codes...", true));
                var arc = pipe.GetFlag<ZipArchive>(ID_Archive);
                arc.CreateEntryFromFile(problem.StandardProgram, PF_StandardProgram);

                return problem;
            });
        }

        public static PPipeline UseSamples(this PPipeline pipeline)
        {
            return pipeline.Use((pipe, problem) =>
            {
                ConsoleUI.Write(new OutputText("  Copy samples...", true));
                var arc = pipe.GetFlag<ZipArchive>(ID_Archive);

                foreach (var u in problem.GetSamples())
                {
                    arc.CreateEntryFromFile(u.InputFile, Path.Join(PD_Samples, ProblemPath.GetTestInput(u.Name)));
                    arc.CreateEntryFromFile(u.OutputFile, Path.Join(PD_Samples, ProblemPath.GetTestOutput(u.Name)));
                }

                return problem;
            });
        }

        public static PPipeline UseTests(this PPipeline pipeline)
        {
            return pipeline.Use((pipe, problem) =>
            {
                Write(new OutputText("  Copy tests...", true));
                var arc = pipe.GetFlag<ZipArchive>(ID_Archive);

                var testsEntry = arc.CreateEntry(PF_Tests);

                string tests = Path.Join(problem.Temp, PF_Tests);
                if (File.Exists(tests)) File.Delete(tests);
                using (var testarc = ZipFile.Open(tests, ZipArchiveMode.Create, Encoding.UTF8))
                {
                    foreach (var u in problem.GetTests())
                    {
                        testarc.CreateEntryFromFile(u.InputFile, ProblemPath.GetTestInput(u.Name));
                        testarc.CreateEntryFromFile(u.OutputFile, ProblemPath.GetTestOutput(u.Name));
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
                var arc = pipe.GetFlag<ZipArchive>(ID_Archive);

                string data = Path.Join(problem.Temp, PF_Extra);
                ZipFile.CreateFromDirectory(problem.Extra, data, CompressionLevel.Fastest, false, Encoding.UTF8);
                arc.CreateEntryFromFile(data, PF_Extra);

                return problem;
            });
        }

        public static PPipeline UseIssues(this PPipeline pipeline, List<Issue> lints)
        {
            return pipeline.Use((pipe, problem) =>
            {
                ConsoleUI.Write(new OutputText("  Add logs...", true));
                var arc = pipe.GetFlag<ZipArchive>(ID_Archive);

                var lintEntry = arc.CreateEntry(PF_Issues);
                using (StreamWriter sw = new StreamWriter(lintEntry.Open(), Encoding.UTF8))
                    sw.WriteLine(JsonConvert.SerializeObject(lints, Formatting.Indented));

                return problem;
            });
        }

        public static PPipeline UsePackage(this PPipeline pipeline)
        {
            return pipeline.Use((pipe, problem) =>
            {
                Write(new OutputText("  Add package metadata...", true));
                var arc = pipe.GetFlag<ZipArchive>(ID_Archive);

                var packageEntry = arc.CreateEntry(PF_PackageConfig);
                using (StreamWriter sw = new StreamWriter(packageEntry.Open(), Encoding.UTF8))
                    sw.WriteLine(JsonConvert.SerializeObject(pipe.GetFlag<PackageProfile>(ID_Package), Formatting.Indented));

                return problem;
            });
        }
    }
}
