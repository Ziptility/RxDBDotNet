using Example.GraphQLApi.Models;
using HotChocolate.AspNetCore;
using RxDBDotNet.GraphQL;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to use specific ports for HTTP
builder.WebHost.ConfigureKestrel(options =>
{
    // Port for HTTP GraphQL endpoint
    options.ListenAnyIP(10102);
    // Port for WebSocket GraphQL subscription endpoint
    options.ListenAnyIP(10103);
});

// Add services to the container.
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query<Hero>>()
    .AddMutationType<Mutation>()
    .AddSubscriptionType<Subscription>()
    .AddInMemorySubscriptions();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(corsPolicyBuilder =>
    {
        corsPolicyBuilder.WithOrigins("http://localhost:1337") // Add the allowed origin
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
app.MapGraphQL().WithOptions(new GraphQLServerOptions
{
    Tool = {
        Enable = true,
    },
});

app.Run();

// Sample query, mutation, and subscription types for demonstration purposes

public class Mutation
{
    public string Echo(string message) => message;
}

public class Subscription
{
    [Subscribe]
    [Topic]
    public string OnMessageReceived([EventMessage] string message) => message;
}
