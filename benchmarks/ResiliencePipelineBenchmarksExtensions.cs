using System;
using System.Threading.Tasks;

namespace DotNetResiliencePipeline.Benchmarks
{
    /// <summary>
    /// Extension methods for <see cref="ResiliencePipelineBenchmarks"/> that provide convenient operations for benchmark scenarios.
    /// </summary>
    public static class ResiliencePipelineBenchmarksExtensions
    {
        /// <summary>
        /// Resets the benchmark pipeline state by invoking the setup routine.
        /// </summary>
        /// <param name="pipeline">The benchmark instance to reset.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="pipeline"/> is null.</exception>
        public static void ResetPipelineState(this ResiliencePipelineBenchmarks pipeline)
        {
            ArgumentNullException.ThrowIfNull(pipeline);
            pipeline.Setup();
        }

        /// <summary>
        /// Executes the successful operation benchmark and collects execution statistics.
        /// </summary>
        /// <param name="pipeline">The benchmark instance.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the pipeline statistics.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="pipeline"/> is null.</exception>
        public static async Task<PipelineStatistics> RunAndCollectStatistics(this ResiliencePipelineBenchmarks pipeline)
        {
            ArgumentNullException.ThrowIfNull(pipeline);
            await pipeline.ResiliencePipeline_Execute_Successful_Operation();
            return pipeline.ResiliencePipeline_Get_Statistics();
        }

        /// <summary>
        /// Executes the full resilience pipeline benchmark and collects execution statistics.
        /// </summary>
        /// <param name="pipeline">The benchmark instance.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the pipeline statistics.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="pipeline"/> is null.</exception>
        public static async Task<PipelineStatistics> RunFullPipelineAndCollectStatistics(this ResiliencePipelineBenchmarks pipeline)
        {
            ArgumentNullException.ThrowIfNull(pipeline);
            await pipeline.ResiliencePipeline_Execute_Full_Pipeline();
            return pipeline.ResiliencePipeline_Get_Statistics();
        }

        /// <summary>
        /// Executes multiple parallel operations through the pipeline and collects execution statistics.
        /// </summary>
        /// <param name="pipeline">The benchmark instance.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the pipeline statistics.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="pipeline"/> is null.</exception>
        public static async Task<PipelineStatistics> RunParallelOperationsAndCollectStatistics(this ResiliencePipelineBenchmarks pipeline)
        {
            ArgumentNullException.ThrowIfNull(pipeline);
            await pipeline.ResiliencePipeline_Execute_Multiple_Operations_Parallel();
            return pipeline.ResiliencePipeline_Get_Statistics();
        }
    }
}
