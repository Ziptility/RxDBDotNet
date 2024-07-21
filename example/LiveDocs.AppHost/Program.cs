// see https://learn.microsoft.com/en-us/dotnet/aspire/get-started/build-aspire-apps-with-nodejs

using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

// Add SQL Server
var password = builder.AddParameter("sqlpassword", true);
var sqlDb = builder.AddSqlServer("sql", password: password, port: 16032)
    .AddDatabase("sqldata", databaseName: "LiveDocsDb")
    .WithEndpoint(port: 16033);

builder.AddProject<LiveDocs_GraphQLApi>("replicationApi")
    .WithReference(sqlDb)
    .WithReference(cache)
    .WithEnvironment("SQL_PASSWORD", password);

// Add SQL Server with the specified password and port
// var password = builder.AddParameter("sqlpassword", true);
// var sqlDb = builder.AddSqlServer("SqlServer", password: password, port: 16032)
//     .AddDatabase("SqlDb", "LiveDocsDb")
//     .WithEndpoint(port: 16033);


builder.AddNpmApp("rxdbclient", "../LiveDocs.RxDBClient", "run")
    .WithHttpEndpoint(port: 1337, env: "PORT")
    .WithExternalHttpEndpoints();
    //.WithReference(replicationApi)
    //.WithReference(cache)
    //.WithReference(sqlDb);
    //.WithEnvironment("SQL_PASSWORD", password);

builder.Build()
    .Run();
