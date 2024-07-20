using LiveDocs.GraphQLApi.Models;
using LiveDocs.GraphQLApi.Repositories;
using LiveDocs.ServiceDefaults;
using HotChocolate.AspNetCore;
using RxDBDotNet.Extensions;
using RxDBDotNet.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

// see https://learn.microsoft.com/en-us/dotnet/aspire/database/sql-server-components
// https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/external-parameters#parameter-example

// Add services to the container.
builder.Services
    .AddProblemDetails()
    .AddSingleton<IDocumentRepository<Hero>, InMemoryDocumentRepository<Hero>>();

// Configure the GraphQL server
builder.Services.AddGraphQLServer()
    .ModifyRequestOptions(o =>
    {
        o.IncludeExceptionDetails = true;
    })
    .AddReplicationServer()
    .AddReplicatedDocument<Hero>()
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

// Enable CORS
app.UseCors();

// Enable WebSockets
app.UseWebSockets();

// Configure the GraphQL endpoint
app.MapGraphQL()
    .WithOptions(new GraphQLServerOptions
    {
        Tool =
        {
            Enable = true,
        },
    });

app.Run();
