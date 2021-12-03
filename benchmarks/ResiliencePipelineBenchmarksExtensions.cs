using System;
using System.Threading.Tasks;

namespace DotNetResiliencePipeline.Benchmarks
{
    /// <summary>
    /// Provides extension methods for <see cref="ResiliencePipelineBenchmarks"/> that simplify common benchmark
    /// operations such as resetting the pipeline, executing specific benchmark scenarios, and retrieving the
    /// resulting <see cref="PipelineStatistics"/>.
    /// </summary>
    public static class ResiliencePipelineBenchmarksExtensions
    {
        /// <summary>
        /// Resets the benchmark pipeline state by invoking the setup routine on the supplied benchmark instance.
        /// </summary>
        /// <param name="pipeline">The <see cref="ResiliencePipelineBenchmarks"/> instance whose state should be reset.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="pipeline"/> is <c>null</c>.</exception>
        public static void ResetPipelineState(this ResiliencePipelineBenchmarks pipeline)
        {
            ArgumentNullException.ThrowIfNull(pipeline);
            pipeline.Setup();
        }

        /// <summary>
        /// Executes the successful‑operation benchmark scenario and returns the statistics collected during the run.
        /// </summary>
        /// <param name="pipeline">The <see cref="ResiliencePipelineBenchmarks"/> instance to execute.</param>
        /// <returns>
        /// A <see cref="Task{PipelineStatistics}"/> that completes with the <see cref="PipelineStatistics"/>
        /// gathered from the successful‑operation benchmark.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="pipeline"/> is <c>null</c>.</exception>
        public static async Task<PipelineStatistics> RunAndCollectStatistics(this ResiliencePipelineBenchmarks pipeline)
        {
            ArgumentNullException.ThrowIfNull(pipeline);
            await pipeline.ResiliencePipeline_Execute_Successful_Operation();
            return pipeline.ResiliencePipeline_Get_Statistics();
        }

        /// <summary>
        /// Executes the full resilience‑pipeline benchmark scenario and returns the statistics collected during the run.
        /// </summary>
        /// <param name="pipeline">The <see cref="ResiliencePipelineBenchmarks"/> instance to execute.</param>
        /// <returns>
        /// A <see cref="Task{PipelineStatistics}"/> that completes with the <see cref="PipelineStatistics"/>
        /// gathered from the full pipeline benchmark.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="pipeline"/> is <c>null</c>.</exception>
        public static async Task<PipelineStatistics> RunFullPipelineAndCollectStatistics(this ResiliencePipelineBenchmarks pipeline)
        {
            ArgumentNullException.ThrowIfNull(pipeline);
            await pipeline.ResiliencePipeline_Execute_Full_Pipeline();
            return pipeline.ResiliencePipeline_Get_Statistics();
        }

        /// <summary>
        /// Executes multiple operations in parallel through the benchmark pipeline and returns the collected statistics.
        /// </summary>
        /// <param name="pipeline">The <see cref="ResiliencePipelineBenchmarks"/> instance to execute.</param>
        /// <returns>
        /// A <see cref="Task{PipelineStatistics}"/> that completes with the <see cref="PipelineStatistics"/>
        /// gathered from the parallel‑operations benchmark.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="pipeline"/> is <c>null</c>.</exception>
        public static async Task<PipelineStatistics> RunParallelOperationsAndCollectStatistics(this ResiliencePipelineBenchmarks pipeline)
        {
            ArgumentNullException.ThrowIfNull(pipeline);
            await pipeline.ResiliencePipeline_Execute_Multiple_Operations_Parallel();
            return pipeline.ResiliencePipeline_Get_Statistics();
        }
    }
}
