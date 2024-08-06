using LiveDocs.GraphQLApi.Infrastructure;

namespace LiveDocs.GraphQLApi;

public sealed class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Check if we're running in an Aspire environment
        var isAspireEnvironment = builder.Configuration.GetValue<bool>($"{ConfigKeys.IsAspireEnvironment}");

        var startup = new Startup();
        startup.ConfigureServices(builder.Services, builder.Environment, builder, isAspireEnvironment);

        var app = builder.Build();
        startup.Configure(app, app.Environment);

        if (isAspireEnvironment)
        {
#pragma warning disable ASP0000
            var serviceProvider = builder.Services.BuildServiceProvider();
#pragma warning restore ASP0000

            await LiveDocsDbInitializer.InitializeAsync(serviceProvider);
        }

        await app.RunAsync();
    }
}
