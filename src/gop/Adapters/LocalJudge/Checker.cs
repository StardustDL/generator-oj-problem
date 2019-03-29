using gop.Adapters.Generic;
using System.Collections.Generic;
using PPipeline = gop.Adapters.Pipeline<gop.Problems.ProblemPath, System.Collections.Generic.List<gop.Issue>>;

namespace gop.Adapters.LocalJudge
{
    public static class Checker
    {
        const string Token = "LocalJudge.Checker";

        public static PPipeline UseInitial(this PPipeline pipeline)
        {
            pipeline.SetToken(Token);
            pipeline.Result = new List<Issue>();
            return pipeline;
        }

        public static PPipeline UseDefault(this PPipeline pipeline)
        {
            return pipeline.UseInitial().UseProfile().UseDescriptions().UseSamples().UseTests();
        }
    }
}
