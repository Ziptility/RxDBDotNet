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

        await LiveDocsDbInitializer.InitializeAsync();

        await app.RunAsync();
    }
}
