// see https://learn.microsoft.com/en-us/dotnet/aspire/get-started/build-aspire-apps-with-nodejs

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

builder.AddProject<Projects.Example_GraphQLApi>("apiservice")
    .WithReference(cache);

// builder.AddProject<Projects.GraphQLExample_Web>("webfrontend")
//     .WithExternalHttpEndpoints()
//     .WithReference(cache)
//     .WithReference(apiService);

builder.Build().Run();
