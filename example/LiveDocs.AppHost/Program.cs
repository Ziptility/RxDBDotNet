// see https://learn.microsoft.com/en-us/dotnet/aspire/get-started/build-aspire-apps-with-nodejs

using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var graphQLApi = builder.AddProject<LiveDocs_GraphQLApi>("graphqlapi");

builder.AddNpmApp("rxdbclient", "../LiveDocs.RxDBClient", "run")
    .WithHttpEndpoint(port: 1337, env: "PORT")
    .WithExternalHttpEndpoints()
    .WithReference(graphQLApi)
    .WithReference(cache);

builder.Build()
    .Run();
