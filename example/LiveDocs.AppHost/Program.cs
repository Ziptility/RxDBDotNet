using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis", 6379)
    .WithEndpoint(6380, 6379, name: "redis-endpoint");

// Add SQL Server
var password = builder.AddParameter("sqlpassword", true);
var sqlDb = builder.AddSqlServer("sql", password, 1433)
    .WithEndpoint(1146, 1433, name: "sql-endpoint")
    .AddDatabase("sqldata", "LiveDocsDb");

builder.AddProject<LiveDocs_GraphQLApi>("replicationApi", "http")
    .WithReference(redis)
    .WithReference(sqlDb)
    .WithEnvironment("SQL_PASSWORD", password);

if (!string.Equals(Environment.GetEnvironmentVariable("EXCLUDE_CLIENT"), "true", StringComparison.Ordinal))
{
    builder.AddNpmApp("livedocs-client", "../livedocs-client", "dev")
        .WithHttpEndpoint(3001, 3000, env: "PORT")
        .WithEnvironment("NODE_ENV", "development");
}

await builder.Build()
    .RunAsync();
