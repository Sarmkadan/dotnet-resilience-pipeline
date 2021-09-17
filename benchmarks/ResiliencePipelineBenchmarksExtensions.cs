using System;
using System.Threading.Tasks;

namespace DotNetResiliencePipeline.Benchmarks
{
    public static class ResiliencePipelineBenchmarksExtensions
    {
        public static void ResetPipelineState(this ResiliencePipelineBenchmarks pipeline)
        {
            pipeline.Setup();
        }

        public static async Task<PipelineStatistics> RunAndCollectStatistics(this ResiliencePipelineBenchmarks pipeline)
        {
            await pipeline.ResiliencePipeline_Execute_Successful_Operation();
            return pipeline.ResiliencePipeline_Get_Statistics();
        }

        public static async Task<PipelineStatistics> RunFullPipelineAndCollectStatistics(this ResiliencePipelineBenchmarks pipeline)
        {
            await pipeline.ResiliencePipeline_Execute_Full_Pipeline();
            return pipeline.ResiliencePipeline_Get_Statistics();
        }

        public static async Task<PipelineStatistics> RunParallelOperationsAndCollectStatistics(this ResiliencePipelineBenchmarks pipeline)
        {
            await pipeline.ResiliencePipeline_Execute_Multiple_Operations_Parallel();
            return pipeline.ResiliencePipeline_Get_Statistics();
        }
    }
}
