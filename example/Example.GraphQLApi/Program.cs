using Example.GraphQLApi.Models;
using HotChocolate.AspNetCore;
using RxDBDotNet.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGraphQLServer()
    .ModifyRequestOptions(o =>
    {
        o.IncludeExceptionDetails = true;
    })
    .AddReplicationSupport<Hero>()
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

namespace Example.GraphQLApi
{
    // Sample query, mutation, and subscription types for demonstration purposes

    public class Mutation
    {
        public string Echo(string message)
        {
            return message;
        }
    }

    public class Subscription
    {
        [Subscribe]
        [Topic]
        public string OnMessageReceived([EventMessage] string message)
        {
            return message;
        }
    }
}
