using Xunit;

namespace RxDBDotNet.Tests.Setup
{
    public sealed class DockerSetupUtil : IAsyncLifetime
    {
        private static readonly SemaphoreSlim Semaphore = new(1, 1);
        private static volatile bool _isInitialized;

        public async Task InitializeAsync()
        {
            if (_isInitialized)
            {
                return;
            }

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

        public Task DisposeAsync() => Task.CompletedTask;

        private static async Task InitializeAsyncInternal()
        {
            Console.WriteLine("Starting Docker setup...");
            await RedisSetupUtil.SetupAsync();
            await DbSetupUtil.SetupAsync();
            Console.WriteLine("Docker setup completed successfully");
        }
    }
}
