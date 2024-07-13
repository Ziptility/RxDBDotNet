var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to use specific ports for HTTP
builder.WebHost.ConfigureKestrel(options =>
{
    // Port for HTTP GraphQL endpoint
    options.ListenAnyIP(10102);

    // Port for WebSocket GraphQL subscription endpoint
    options.ListenAnyIP(10103);
});

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services
    .AddProblemDetails()
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddSubscriptionType<Subscription>()
    .AddInMemorySubscriptions(); // Add in-memory subscriptions for demo purposes

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

// Middleware for serving static files and default routing
app.UseStaticFiles();
app.UseRouting();

// Enable WebSockets
app.UseWebSockets();

// Map GraphQL endpoint
app.MapGraphQL();

// Enable WebSockets
app.MapGet("/subscriptions", _ => Task.CompletedTask);

app.MapDefaultEndpoints();

app.Run();

// Sample query, mutation, and subscription types for demonstration purposes
public class Query
{
    public string Hello() => "Hello, world!";
}

public class Mutation
{
    public string Echo(string message) => message;
}

public class Subscription
{
    [Subscribe]
    [Topic]
    public string OnMessageReceived(string message) => message;
}
