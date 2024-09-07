using HotChocolate.Execution.Configuration;
using System.Security.Claims;
using LiveDocs.GraphQLApi.Infrastructure;
using LiveDocs.GraphQLApi.Models.Shared;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using RxDBDotNet.Documents;
using RxDBDotNet.Extensions;
using Microsoft.AspNetCore.Http;
using System.Net;
using HotChocolate.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using HotChocolate.Subscriptions;
using StackExchange.Redis;
using HotChocolate.Execution.Options;
using LiveDocs.GraphQLApi.Data;
using LiveDocs.GraphQLApi.Models.Replication;
using LiveDocs.GraphQLApi.Services;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using RxDBDotNet.Services;
using RxDBDotNet.Security;

namespace RxDBDotNet.Tests.Setup
{
    /// <summary>
    /// Provides a fluent builder pattern for arranging a test scenario.
    /// </summary>
    public class TestScenarioBuilder
    {
        private bool _setupAuthorization;
        private Action<IWebHostBuilder> _configureWebHostBuilder = _ => { };
        private Action<IServiceCollection> _configureServices = _ => { };
        private Action<IRequestExecutorBuilder> _configureGraphQL = _ => { };
        private readonly Dictionary<Type, Action<IRequestExecutorBuilder>> _configureReplicatedDocuments = [];

        /// <summary>
        /// Initializes a new instance of the <see cref="TestScenarioBuilder"/> class with optional default GraphQL configurations.
        /// </summary>
        /// <param name="configureGraphQLDefaults">
        /// If set to <c>true</c>, the constructor configures default GraphQL options and services that do not change between tests.
        /// </param>
        public TestScenarioBuilder(bool configureGraphQLDefaults = true)
        {
            if (configureGraphQLDefaults)
            {
                // Configure default graphql options that don't change between tests
                ConfigureGraphQL(builder =>
                {
                    builder.ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                        .AddMutationConventions()
                        .AddReplication()
                        .AddRedisSubscriptions(provider => provider.GetRequiredService<IConnectionMultiplexer>(), new SubscriptionOptions
                        {
                            TopicPrefix = Guid.NewGuid()
                                .ToString(),
                        })
                        .AddSubscriptionDiagnostics();
                });
            }

            // Configure default servcies that don't change between tests
            ConfigureServices(services =>
            {
                services.AddProblemDetails();
                services.Configure<WebSocketOptions>(options => options.KeepAliveInterval = TimeSpan.FromMinutes(2));
                services.Configure<RequestExecutorOptions>(options => options.ExecutionTimeout = TimeSpan.FromMinutes(15));
                services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect("localhost:3333"));
                services.AddDbContext<LiveDocsDbContext>(options =>
                    options.UseSqlServer(Environment.GetEnvironmentVariable(ConfigKeys.DbConnectionString)
                                         ?? throw new InvalidOperationException($"The '{ConfigKeys.DbConnectionString}' env variable must be set")));

                services.AddScoped<IDocumentService<ReplicatedUser>, UserService>()
                    .AddScoped<IDocumentService<ReplicatedWorkspace>, WorkspaceService>()
                    .AddScoped<IDocumentService<ReplicatedLiveDoc>, LiveDocService>();
            });

            // Add replicated documents with no additional configuration
            // Can be overridden by the test scenario
            ConfigureReplicatedDocument<ReplicatedUser>();
            ConfigureReplicatedDocument<ReplicatedWorkspace>();
            ConfigureReplicatedDocument<ReplicatedLiveDoc>();
        }

        /// <summary>
        /// Configures authorization for the test setup.
        /// </summary>
        /// <param name="setup">If true, sets up authorization.</param>
        /// <returns>The current <see cref="TestScenarioBuilder"/> instance.</returns>
        public TestScenarioBuilder WithAuthorization(bool setup = true)
        {
            _setupAuthorization = setup;
            return this;
        }

        /// <summary>
        /// Configures the web host builder.
        /// </summary>
        /// <param name="configure">An action to configure the web host builder.</param>
        /// <returns>The current <see cref="TestScenarioBuilder"/> instance.</returns>
        public TestScenarioBuilder ConfigureWebHost(Action<IWebHostBuilder> configure)
        {
            _configureWebHostBuilder += configure;
            return this;
        }

        /// <summary>
        /// Configures services for the test setup.
        /// </summary>
        /// <param name="configure">An action to configure services.</param>
        /// <returns>The current <see cref="TestScenarioBuilder"/> instance.</returns>
        public TestScenarioBuilder ConfigureServices(Action<IServiceCollection> configure)
        {
            _configureServices += configure;
            return this;
        }

        /// <summary>
        /// Configures GraphQL for the test setup.
        /// </summary>
        /// <param name="configure">An action to configure GraphQL.</param>
        /// <returns>The current <see cref="TestScenarioBuilder"/> instance.</returns>
        public TestScenarioBuilder ConfigureGraphQL(Action<IRequestExecutorBuilder> configure)
        {
            _configureGraphQL += configure;
            return this;
        }

        /// <summary>
        /// Configures options for a specific document type.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="configure">An action to configure replication options.</param>
        /// <returns>The current <see cref="TestScenarioBuilder"/> instance.</returns>
        public TestScenarioBuilder ConfigureReplicatedDocument<TDocument>(Action<ReplicationOptions<TDocument>>? configure = null)
            where TDocument : class, IReplicatedDocument
        {
            _configureReplicatedDocuments[typeof(TDocument)] = builder => builder.AddReplicatedDocument(configure);
            return this;
        }

        /// <summary>
        /// Builds and returns a new <see cref="TestContext"/> based on the configured options.
        /// </summary>
        /// <returns>A new <see cref="TestContext"/> instance.</returns>
        public TestContext Build()
        {
            var asyncDisposables = new List<IAsyncDisposable>();
            var disposables = new List<IDisposable>();

            var testTimeout = Debugger.IsAttached ? TimeSpan.FromMinutes(5) : TimeSpan.FromSeconds(10);

            var timeoutTokenSource = new CancellationTokenSource(testTimeout);
            disposables.Add(timeoutTokenSource);

            var timeoutToken = timeoutTokenSource.Token;

            #pragma warning disable CA2000 // This is disposed in the TestContext.DisposeAsync method
            var factory = ConfigureWebApplicationFactory();

            asyncDisposables.Add(factory);

            var asyncTestServiceScope = factory.Services.CreateAsyncScope();
            asyncDisposables.Add(asyncTestServiceScope);

            var applicationStoppingToken = asyncTestServiceScope
                .ServiceProvider
                .GetRequiredService<IHostApplicationLifetime>()
                .ApplicationStopping;

            var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutToken, applicationStoppingToken);
            disposables.Add(linkedTokenSource);

            var linkedToken = linkedTokenSource.Token;

            return new TestContext
            {
                Factory = factory,
                HttpClient = factory.CreateClient(),
                ServiceProvider = asyncTestServiceScope.ServiceProvider,
                CancellationToken = linkedToken,
                AsyncDisposables = asyncDisposables,
                Disposables = disposables,
            };
        }

        private WebApplicationFactory<TestProgram> ConfigureWebApplicationFactory()
        {
            return new WebApplicationFactory<TestProgram>().WithWebHostBuilder(builder =>
            {
                builder.UseSolutionRelativeContentRoot("example/LiveDocs.GraphQLApi")
                    .ConfigureServices(services =>
                    {
                        if (_setupAuthorization)
                        {
                            SetupAuthorization(services);
                        }

                        _configureServices(services);

                        var graphQLBuilder = services.AddGraphQLServer();

                        foreach (var replicatedDocumentConfig in _configureReplicatedDocuments.Values)
                        {
                            replicatedDocumentConfig(graphQLBuilder);
                        }

                        _configureGraphQL(graphQLBuilder);
                    });

                builder.Configure(ConfigureAppDefaults);

                _configureWebHostBuilder(builder);
            });
        }

        /// <summary>
        /// Sets up authorization for testing purposes.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to configure.</param>
        private void SetupAuthorization(IServiceCollection services)
        {
            services.AddScoped<AuthorizationHelper>();

            ConfigurePolicies(services);

            ConfigureAuthentication(services);

            ConfigureGraphQL(builder => builder.AddAuthorization());
        }

        private static void ConfigurePolicies(IServiceCollection services)
        {
            services.AddAuthorizationBuilder()
                .AddPolicy("IsWorkspaceAdmin",
                    policy => policy.RequireClaim(ClaimTypes.Role, nameof(UserRole.WorkspaceAdmin), nameof(UserRole.SystemAdmin)))
                .AddPolicy("IsSystemAdmin", policy => policy.RequireClaim(ClaimTypes.Role, nameof(UserRole.SystemAdmin)));
        }

        /// <summary>
        /// Configures authentication for testing purposes.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to configure.</param>
        private static void ConfigureAuthentication(IServiceCollection services)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Audience = JwtUtil.Audience;
                    options.IncludeErrorDetails = true;
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters = JwtUtil.GetTokenValidationParameters();
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = _ => Task.CompletedTask,
                        OnAuthenticationFailed = ctx =>
                        {
                            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            ctx.Fail(ctx.Exception);
                            return Task.CompletedTask;
                        },
                        OnForbidden = ctx =>
                        {
                            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                            ctx.Fail(nameof(HttpStatusCode.Forbidden));
                            return Task.CompletedTask;
                        },
                    };
                });
        }

        /// <summary>
        /// Configures the default application settings.
        /// </summary>
        /// <param name="app">The application builder.</param>
        private void ConfigureAppDefaults(IApplicationBuilder app)
        {
            if (_setupAuthorization)
            {
                app.UseAuthentication();
            }

            app.UseExceptionHandler();
            app.UseDeveloperExceptionPage();
            app.UseWebSockets();
            app.UseRouting();

            if (_setupAuthorization)
            {
                app.UseAuthorization();
            }

            app.UseEndpoints(endpoints => endpoints.MapGraphQL().WithOptions(new GraphQLServerOptions
            {
                Tool = { Enable = true },
            }));
        }
    }
}
