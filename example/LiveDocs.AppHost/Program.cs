// see https://learn.microsoft.com/en-us/dotnet/aspire/get-started/build-aspire-apps-with-nodejs

using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var graphQLApi = builder.AddProject<LiveDocs_GraphQLApi>("graphqlapi");

// Add SQL Server with the specified password and port
var passwordParameter = builder.AddParameter("sqlpassword", true);
var sqlDb = builder.AddSqlServer("SqlServer", passwordParameter, 16032)
    .PublishAsConnectionString()
    .AddDatabase("SqlDb", "LiveDocsDb");

builder.AddNpmApp("rxdbclient", "../LiveDocs.RxDBClient", "run")
    .WithHttpEndpoint(port: 1337, env: "PORT")
    .WithExternalHttpEndpoints()
    .WithReference(graphQLApi)
    .WithReference(cache)
    .WithReference(sqlDb)
    .WithEnvironment("SQL_PASSWORD", passwordParameter);

builder.Build()
    .Run();
