using DotNet.Testcontainers.Containers;
using Testcontainers.Redis;

namespace RxDBDotNet.Tests.Setup;

public static class RedisSetupUtil
{
    public static async Task<DockerContainer> SetupAsync(CancellationToken cancellationToken)
    {
        var redisContainer = new RedisBuilder().WithImage("redis:7.0")
            .WithPortBinding(3333, 6379)
            .Build();

        await redisContainer.StartAsync(cancellationToken);

        return redisContainer;
    }
}
