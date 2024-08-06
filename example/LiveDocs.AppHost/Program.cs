// see https://learn.microsoft.com/en-us/dotnet/aspire/get-started/build-aspire-apps-with-nodejs

using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

// Add SQL Server
var password = builder.AddParameter("sqlpassword", true);
var sqlDb = builder.AddSqlServer("sql", password: password, port: 16032)
    .AddDatabase("sqldata", databaseName: "LiveDocsDb");

builder.AddProject<LiveDocs_GraphQLApi>("replicationApi", "http")
    .WithReference(sqlDb)
    .WithReference(cache)
    .WithEnvironment("SQL_PASSWORD", password)
    .WithEnvironment("IsAspireEnvironment", "true");

builder.AddNpmApp("rxdbclient", "../LiveDocs.RxDBClient", "run")
    .WithHttpEndpoint(port: 1337, env: "PORT")
    .WithExternalHttpEndpoints();

builder.Build().Run();
