using HotChocolate.AspNetCore;
using LiveDocs.GraphQLApi.Data;
using LiveDocs.GraphQLApi.Infrastructure;
using LiveDocs.GraphQLApi.Models.Replication;
using LiveDocs.GraphQLApi.Services;
using LiveDocs.ServiceDefaults;
using RxDBDotNet.Extensions;
using RxDBDotNet.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

ConfigureServices();

ConfigureGraphQL();

var app = builder.Build();

ConfigureApp();

await InitializeLiveDocsDbAsync();

await app.RunAsync();

return;

void ConfigureServices()
{
    builder.AddServiceDefaults();

    // ConfigureAuthorization();

    builder.Services.AddProblemDetails();

    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(corsPolicyBuilder =>
        {
            corsPolicyBuilder.WithOrigins(
                    "http://localhost:3000",
                    "http://127.0.0.1:8888")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

    builder.AddSqlServerDbContext<LiveDocsDbContext>("sqldata");

    // Redis is required for Hot Chocolate subscriptions
    builder.AddRedisClient("redis");

    builder.Services
        .AddScoped<IDocumentService<ReplicatedUser>, UserService>()
        .AddScoped<IDocumentService<ReplicatedWorkspace>, WorkspaceService>()
        .AddScoped<IDocumentService<ReplicatedLiveDoc>, LiveDocService>();
}

void ConfigureGraphQL()
{
    builder.Services.AddGraphQLServer()
        .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
        .AddAuthorization()
        // Mutation conventions must be enabled for replication to work
        .AddMutationConventions()
        .AddReplication()
        .AddReplicatedDocument<ReplicatedLiveDoc>()
        .AddReplicatedDocument<ReplicatedUser>()
        .AddReplicatedDocument<ReplicatedWorkspace>()
        .AddRedisSubscriptions(provider => provider.GetRequiredService<IConnectionMultiplexer>());
}

// void ConfigureAuthorization()
// {
//     builder.Services.AddScoped<AuthorizationHelper>();
//
//     ConfigurePolicies();
//
//     ConfigureAuthentication();
// }

// void ConfigurePolicies()
// {
//     builder.Services.AddAuthorizationBuilder()
//         // The roles are hierarchical
//         .AddPolicy(PolicyNames.HasStandardUserAccess,
//             policy => policy.RequireClaim(
//                 ClaimTypes.Role,
//                 nameof(UserRole.StandardUser),
//                 nameof(UserRole.WorkspaceAdmin),
//                 nameof(UserRole.SystemAdmin)))
//         .AddPolicy(PolicyNames.HasWorkspaceAdminAccess,
//             policy => policy.RequireClaim(
//                 ClaimTypes.Role,
//                 nameof(UserRole.WorkspaceAdmin),
//                 nameof(UserRole.SystemAdmin)))
//         .AddPolicy(PolicyNames.HasSystemAdminAccess,
//             policy => policy.RequireClaim(
//                 ClaimTypes.Role,
//                 nameof(UserRole.SystemAdmin)));
// }

// void ConfigureAuthentication()
// {
//     builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//         .AddJwtBearer(options =>
//         {
//             options.Audience = JwtUtil.Audience;
//             options.IncludeErrorDetails = true;
//             options.RequireHttpsMetadata = false;
//             options.TokenValidationParameters = JwtUtil.GetTokenValidationParameters();
//             options.Events = new JwtBearerEvents
//             {
//                 OnMessageReceived = _ => Task.CompletedTask,
//                 OnAuthenticationFailed = ctx =>
//                 {
//                     ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
//                     ctx.Fail(ctx.Exception);
//                     return Task.CompletedTask;
//                 },
//                 OnForbidden = ctx =>
//                 {
//                     ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
//                     ctx.Fail(nameof(HttpStatusCode.Forbidden));
//                     return Task.CompletedTask;
//                 },
//             };
//         });
// }

static Task InitializeLiveDocsDbAsync()
{
    const string dbConnectionString = "Server=127.0.0.1,1146;Database=LiveDocsDb;User Id=sa;Password=Admin123!;TrustServerCertificate=True";

    Environment.SetEnvironmentVariable(ConfigKeys.DbConnectionString, dbConnectionString);

    return LiveDocsDbInitializer.InitializeAsync();
}

void ConfigureApp()
{
    app.UseExceptionHandler();

    app.UseDeveloperExceptionPage();

    app.UseCors();

    // app.UseAuthentication();
    //
    // app.UseAuthorization();

    app.UseWebSockets();

    app.MapGraphQL()
        .WithOptions(new GraphQLServerOptions
        {
            Tool =
            {
                Enable = true,
            },
        });
}
