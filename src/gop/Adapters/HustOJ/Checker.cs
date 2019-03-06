using System.Collections.Generic;
using gop.Adapters.Generic;
using PPipeline = gop.Adapters.Pipeline<gop.Problems.ProblemPath, System.Collections.Generic.List<gop.Adapters.Issue>>;

namespace gop.Adapters.HustOJ
{
    public static class Checker
    {
        const string Token = "HustOJ.Checker";

        public static PPipeline UseInitial(this PPipeline pipeline)
        {
            pipeline.SetToken(Token);
            pipeline.Result = new List<Issue>();
            return pipeline;
        }

        public static PPipeline UseDefault(this PPipeline pipeline)
        {
            return pipeline.UseInitial().UseConfig().UseDescriptions().UseSamples().UseTests();
        }
    }
}
