// example/LiveDocs.AppHost/Program.cs

using System;
using Aspire.Hosting;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis", 6379)
    .WithImage("redis")
    .WithImageTag("latest")
    .WithEndpoint(port: 6380, targetPort: 6379, name: "redis-endpoint");

// Add SQL Server
var password = builder.AddParameter("sqlpassword", secret: true);
var sqlDb = builder.AddSqlServer("sql", password: password, port: 1433)
    .WithImage("mssql/server")
    .WithImageTag("2022-latest")
    .WithEnvironment("ACCEPT_EULA", "Y")
    .WithEnvironment("MSSQL_SA_PASSWORD", "Admin123!")
    // this is here to preserve the data across restarts
    .WithVolume("livedocs-sql-data", "/var/opt/mssql")
    .WithEndpoint(port: 1146, targetPort: 1433, name: "sql-endpoint")
    .AddDatabase("sqldata", databaseName: "LiveDocsDb");

builder.AddProject<LiveDocs_GraphQLApi>("replicationApi", "http")
    .WithReference(redis)
    .WithReference(sqlDb)
    .WithEnvironment("SQL_PASSWORD", password);

if (!string.Equals(Environment.GetEnvironmentVariable("EXCLUDE_CLIENT"), "true", StringComparison.Ordinal))
{
    builder.AddNpmApp("livedocs-client", "../livedocs-client", "run")
        .WithHttpEndpoint(port: 3001, targetPort: 3000, env: "PORT")
        .WithEnvironment("NODE_ENV", "production")
        .WithExternalHttpEndpoints();
}

await builder.Build().RunAsync();
