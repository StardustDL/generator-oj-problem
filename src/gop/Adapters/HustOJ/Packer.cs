using gop.Adapters.Generic;
using PPipeline = gop.Adapters.Pipeline<gop.Problems.ProblemPath, string>;

namespace gop.Adapters.HustOJ
{
    public static class Packer
    {
        const string Token = "HustOJ.Packer";

        public static PPipeline UseInitial(this PPipeline pipeline, PackageProfile package)
        {
            pipeline.SetToken(Token);
            pipeline.Result = null;
            package.Platform = "hustoj";
            return pipeline.UseCreate(package);
        }

        public static PPipeline UseDefault(this PPipeline pipeline, PackageProfile package)
        {
            return pipeline.UseInitial(package).UseMetadata().UseDescriptionsMarkdown().UseSamples().UseSourceCode().UseTests();
        }
    }
}
