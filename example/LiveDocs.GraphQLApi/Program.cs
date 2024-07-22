namespace LiveDocs.GraphQLApi;

public sealed class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Check if we're running in an Aspire environment
        var isAspireEnvironment = builder.Configuration.GetValue<bool>("IsAspireEnvironment");

        var startup = new Startup(builder.Configuration);
        startup.ConfigureServices(builder.Services, builder.Environment, builder, isAspireEnvironment);

        var app = builder.Build();
        startup.Configure(app, app.Environment);

        app.Run();
    }
}
