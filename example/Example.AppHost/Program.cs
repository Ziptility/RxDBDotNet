// see https://learn.microsoft.com/en-us/dotnet/aspire/get-started/build-aspire-apps-with-nodejs

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var graphQLReplicationApi = builder
    .AddProject<Projects.Example_GraphQLApi>("graphqlreplicationapi");

builder.AddProject<Projects.Example_RxDBClient>("rxdbclient")
    .WithExternalHttpEndpoints()
    .WithReference(graphQLReplicationApi)
    .WithReference(cache);

builder.Build().Run();
