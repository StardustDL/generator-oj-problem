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
                var logger = pipe.Logger;
                logger?.Info("Starting", LogCategory);

                var arc = pipe.Container.Get<ZipArchive>();
                var config = pipe.Container.Get<ProblemProfile>();

                var releaseEntry = arc.CreateEntry(PF_Release);

                var xml = FreeProblemSet.Packer.GenerateFPS(problem, config);

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
