using LiveDocs.GraphQLApi.Infrastructure;

namespace LiveDocs.GraphQLApi;

public sealed class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        Startup.ConfigureServices(builder.Services, builder);

        var app = builder.Build();

        Startup.Configure(app);

        await InitializeDatabaseAsync();

        await app.RunAsync();
    }

    private static Task InitializeDatabaseAsync()
    {
        const string dbConnectionString = "Server=127.0.0.1,1146;Database=LiveDocsDb;User Id=sa;Password=Admin123!;TrustServerCertificate=True";

        Environment.SetEnvironmentVariable(ConfigKeys.DbConnectionString, dbConnectionString);

        return LiveDocsDbInitializer.InitializeAsync();
    }
}
