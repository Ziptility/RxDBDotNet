using Microsoft.Extensions.Logging;
using Polly;
using Xunit;

namespace RxDBDotNet.Tests.Setup
{
    /// <summary>
    /// Utility class for setting up Docker containers for RxDBDotNet tests.
    /// Implements IAsyncLifetime for xUnit to manage the lifecycle of Docker resources.
    /// </summary>
    public sealed class DockerSetupUtil : IAsyncLifetime
    {
        private static readonly ILogger Logger = LoggingConfig.LoggerFactory.CreateLogger(nameof(DockerSetupUtil));
        private static readonly SemaphoreSlim Semaphore = new(1, 1);
        private static volatile bool _isInitialized;

        /// <summary>
        /// Initializes the Docker environment for testing.
        /// This method is called by xUnit before running tests.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            await Semaphore.WaitAsync();
            try
            {
                if (!_isInitialized)
                {
                    await InitializeAsyncInternal();
                    _isInitialized = true;
                }
            }
            finally
            {
                Semaphore.Release();
            }
        }

        /// <summary>
        /// Cleans up Docker resources after tests have completed.
        /// This method is called by xUnit after all tests have run.
        /// </summary>
        /// <returns>A completed task, as no asynchronous cleanup is currently needed.</returns>
        public Task DisposeAsync() => Task.CompletedTask;

        /// <summary>
        /// Internal method to initialize the Docker environment.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private static Task InitializeAsyncInternal()
        {
            var policy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(3, retryAttempt =>
                        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timeSpan, retryCount, _) => Logger.LogWarning(exception, "Error during Docker setup (Attempt {RetryCount}). Retrying in {RetryTimeSpan}...", retryCount, timeSpan));

            return policy.ExecuteAsync(async () =>
            {
                Logger.LogInformation("Starting Docker setup...");
                await RedisSetupUtil.SetupAsync();
                await DbSetupUtil.SetupAsync();
                Logger.LogInformation("Docker setup completed successfully");
            });
        }
    }
}
