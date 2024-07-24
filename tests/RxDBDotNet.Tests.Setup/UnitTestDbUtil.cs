﻿using LiveDocs.GraphQLApi.Infrastructure;
using Testcontainers.MsSql;
using Xunit.Abstractions;

namespace RxDBDotNet.Tests.Setup;

public static class UnitTestDbUtil
{
    private static readonly SemaphoreSlim Semaphore = new(
        1,
        1);

    private static volatile bool _isInitialized;

    public static async Task InitializeAsync(IServiceProvider serviceProvider, ITestOutputHelper output)
    {
        ArgumentNullException.ThrowIfNull(output);

        await Semaphore.WaitAsync();

        try
        {
            if (!_isInitialized)
            {
                output.WriteLine("Initializing the unit test db");

                var sqlServerDockerContainer = new MsSqlBuilder()
                    .WithName("livedocs_test_db")
                    .WithPassword("Password123!")
                    .Build();

                await sqlServerDockerContainer.StartAsync();

                var connectionStringToMaster = sqlServerDockerContainer.GetConnectionString();

                var connectionStringToTestDb = connectionStringToMaster.Replace("master", "LiveDocsTestDb", StringComparison.OrdinalIgnoreCase);

                Environment.SetEnvironmentVariable(
                    ConfigKeys.DbConnectionString,
                    connectionStringToTestDb);

                await LiveDocsDbInitializer.InitializeAsync(serviceProvider);

                _isInitialized = true;
            }
        }
        finally
        {
            Semaphore.Release();
        }
    }
}