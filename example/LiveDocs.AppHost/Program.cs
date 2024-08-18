// see https://learn.microsoft.com/en-us/dotnet/aspire/get-started/build-aspire-apps-with-nodejs

using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis");

// Add SQL Server
var password = builder.AddParameter("sqlpassword", secret: true);
var sqlDb = builder.AddSqlServer("sql", password: password, port: 16032)
    .AddDatabase("sqldata", databaseName: "LiveDocsDb");

builder.AddProject<LiveDocs_GraphQLApi>("replicationApi", "http")
    .WithReference(redis)
    .WithReference(sqlDb)
    .WithEnvironment("SQL_PASSWORD", password)
    .WithEnvironment("IsAspireEnvironment", "true");

builder.AddNpmApp("livedocs-client", "../livedocs-client", "run")
    .WithHttpEndpoint(port: 1337, env: "PORT")
    .WithEnvironment("NODE_ENV", "production")
    .WithExternalHttpEndpoints();

builder.Build().Run();
