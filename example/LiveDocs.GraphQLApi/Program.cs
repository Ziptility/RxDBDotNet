using LiveDocs.GraphQLApi.Models;
using LiveDocs.GraphQLApi.Repositories;
using LiveDocs.ServiceDefaults;
using HotChocolate.AspNetCore;
using LiveDocs.GraphQLApi.Data;
using RxDBDotNet.Extensions;
using RxDBDotNet.Repositories;
using LiveDocs.GraphQLApi.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// see https://learn.microsoft.com/en-us/dotnet/aspire/database/sql-server-components
// https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/external-parameters#parameter-example

// Configure the LiveDocsDbContext
builder.AddSqlServerDbContext<LiveDocsDbContext>("sqldata");

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services
    .AddProblemDetails()
    .AddSingleton<IDocumentRepository<Hero>, InMemoryDocumentRepository<Hero>>()
    .AddScoped<IDocumentRepository<User>, EfDocumentRepository<User, LiveDocsDbContext>>()
    .AddScoped<IDocumentRepository<Workspace>, EfDocumentRepository<Workspace, LiveDocsDbContext>>()
    .AddScoped<IDocumentRepository<LiveDoc>, EfDocumentRepository<LiveDoc, LiveDocsDbContext>>();

// Configure the GraphQL server
builder.Services.AddGraphQLServer()
    .ModifyRequestOptions(o =>
    {
        o.IncludeExceptionDetails = true;
    })
    .AddReplicationServer()
    .AddReplicatedDocument<Hero>()
    .AddReplicatedDocument<User>()
    .AddReplicatedDocument<Workspace>()
    .AddReplicatedDocument<LiveDoc>()
    .AddInMemorySubscriptions();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(corsPolicyBuilder =>
    {
        corsPolicyBuilder.WithOrigins("http://localhost:1337") // Add the RxDB client host origin
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

app.MapDefaultEndpoints();

// Enable CORS
app.UseCors();

// Enable WebSockets
app.UseWebSockets();

// Initialize and seed the database
await LiveDocsDbInitializer.InitializeAsync(app.Services);

// Configure the GraphQL endpoint
app.MapGraphQL()
    .WithOptions(new GraphQLServerOptions
    {
        Tool =
        {
            Enable = true,
        },
    });

await app.RunAsync();
