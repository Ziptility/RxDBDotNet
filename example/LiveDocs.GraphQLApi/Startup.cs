using HotChocolate.AspNetCore;
using LiveDocs.GraphQLApi.Data;
using LiveDocs.GraphQLApi.Infrastructure;
using LiveDocs.GraphQLApi.Models.Entities;
using LiveDocs.GraphQLApi.Models.Replication;
using LiveDocs.GraphQLApi.Repositories;
using LiveDocs.ServiceDefaults;
using Microsoft.EntityFrameworkCore;
using RxDBDotNet.Extensions;
using RxDBDotNet.Repositories;
using Query = LiveDocs.GraphQLApi.Models.GraphQL.Query;

namespace LiveDocs.GraphQLApi;

public class Startup
{
    public virtual void ConfigureServices(
        IServiceCollection services,
        IHostEnvironment environment,
        WebApplicationBuilder builder,
        bool isAspireEnvironment)
    {
        // Configure the database context
        ConfigureDatabase(services, builder, isAspireEnvironment);

        // Add service defaults & Aspire components if running with Aspire
        if (isAspireEnvironment)
        {
            builder.AddServiceDefaults();
        }

        // Add services to the container
        services.AddProblemDetails()
            .AddScoped<IDocumentService<ReplicatedUser>, DocumentService<User, ReplicatedUser>>()
            .AddScoped<IDocumentService<ReplicatedWorkspace>, DocumentService<Workspace,ReplicatedWorkspace>>()
            .AddScoped<IDocumentService<ReplicatedLiveDoc>, DocumentService<LiveDoc, ReplicatedLiveDoc>>();

        // Configure the GraphQL server
        services.AddGraphQLServer()
            .ModifyRequestOptions(o =>
            {
                o.IncludeExceptionDetails = environment.IsDevelopment();
            })
            // Simulate scenario where the library user
            // has already added their own root query type.
            .AddQueryType<Query>()
            .AddReplicationServer()
            .RegisterService<IDocumentService<ReplicatedWorkspace>>()
            .AddReplicatedDocument<ReplicatedUser>()
            .AddReplicatedDocument<ReplicatedWorkspace>()
            .AddReplicatedDocument<ReplicatedLiveDoc>()
            .AddInMemorySubscriptions()
            .AddSubscriptionDiagnostics();

        // Configure CORS
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(corsPolicyBuilder =>
            {
                corsPolicyBuilder.WithOrigins("http://localhost:3000")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
    }

    protected void ConfigureDatabase(
        IServiceCollection services,
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
                options.UseSqlServer(Environment.GetEnvironmentVariable(ConfigKeys.DbConnectionString)
                                     ?? throw new InvalidOperationException($"The '{ConfigKeys.DbConnectionString}' env variable must be set")));
        }
    }

    public void Configure(WebApplication app, IWebHostEnvironment env)
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

        ConfigureTheGraphQLEndpoint(app, env);
    }

    protected virtual void ConfigureTheGraphQLEndpoint(WebApplication app, IWebHostEnvironment env)
    {
        app.MapGraphQL()
            .WithOptions(new GraphQLServerOptions
            {
                EnforceMultipartRequestsPreflightHeader = false,
                Tool =
                {
                    Enable = true,
                },
            });
    }
}
