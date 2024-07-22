using LiveDocs.GraphQLApi.Models;
using LiveDocs.GraphQLApi.Repositories;
using LiveDocs.ServiceDefaults;
using HotChocolate.AspNetCore;
using LiveDocs.GraphQLApi.Data;
using RxDBDotNet.Extensions;
using RxDBDotNet.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LiveDocs.GraphQLApi;

public class Startup(IConfiguration configuration)
{
    public IConfiguration Configuration { get; } = configuration;

    public virtual void ConfigureServices(
        IServiceCollection services,
        IHostEnvironment environment,
        WebApplicationBuilder builder,
        bool isAspireEnvironment)
    {
        // Configure the database context
        ConfigureDatabase(services, environment, builder, isAspireEnvironment);

        // Add service defaults & Aspire components if running with Aspire
        if (isAspireEnvironment)
        {
            builder.AddServiceDefaults();
        }

        // Add services to the container
        services.AddProblemDetails()
            .AddSingleton<IDocumentRepository<Hero>, InMemoryDocumentRepository<Hero>>()
            .AddScoped<IDocumentRepository<User>, EfDocumentRepository<User, LiveDocsDbContext>>()
            .AddScoped<IDocumentRepository<Workspace>, EfDocumentRepository<Workspace, LiveDocsDbContext>>()
            .AddScoped<IDocumentRepository<LiveDoc>, EfDocumentRepository<LiveDoc, LiveDocsDbContext>>();

        // Configure the GraphQL server
        services.AddGraphQLServer()
            .ModifyRequestOptions(o =>
            {
                o.IncludeExceptionDetails = environment.IsDevelopment();
            })
            .AddReplicationServer()
            .AddReplicatedDocument<Hero>()
            .AddReplicatedDocument<User>()
            .AddReplicatedDocument<Workspace>()
            .AddReplicatedDocument<LiveDoc>()
            .AddInMemorySubscriptions();

        // Configure CORS
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(corsPolicyBuilder =>
            {
                corsPolicyBuilder.WithOrigins("http://localhost:1337")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
    }

    protected virtual void ConfigureDatabase(
        IServiceCollection services,
        IHostEnvironment environment,
        WebApplicationBuilder builder,
        bool isAspireEnvironment)
    {
        if (isAspireEnvironment)
        {
            // Use Aspire's SQL Server configuration when running with Aspire
            builder.AddSqlServerDbContext<LiveDocsDbContext>("sqldata");
        }
        else
        {
            // Use a standard SQL Server configuration when not running with Aspire
            services.AddDbContext<LiveDocsDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));
        }
    }

    public virtual void Configure(WebApplication app, IWebHostEnvironment env)
    {
        app.UseExceptionHandler();
        
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseHsts();
        }

        // Enable CORS
        app.UseCors();

        // Enable WebSockets
        app.UseWebSockets();

        // Configure the GraphQL endpoint
        var graphQLServerOptions = new GraphQLServerOptions
        {
            EnforceMultipartRequestsPreflightHeader = false,
            Tool =
            {
                Enable = env.IsDevelopment(),
            },
        };
        app.MapGraphQL().WithOptions(graphQLServerOptions);
    }
}
