// tests\RxDBDotNet.Tests\Security\SubscriptionAuthenticationSchemeTests.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using HotChocolate.Subscriptions;
using LiveDocs.GraphQLApi.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using RxDBDotNet.Extensions;
using RxDBDotNet.Tests.Model;
using RxDBDotNet.Tests.Setup;
using RxDBDotNet.Tests.Utils;
using StackExchange.Redis;

namespace RxDBDotNet.Tests;

[Collection("DockerSetup")]
public class SubscriptionAuthenticationSchemeTests : IAsyncLifetime
{
    private TestContext TestContext { get; set; } = null!;

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (TestContext != null)
        {
            await TestContext.DisposeAsync();
        }
    }

    [Fact]
    public async Task Subscription_WithCustomAuthScheme_ShouldValidateTokenUsingSpecifiedScheme()
    {
        // Arrange
        const string customSchemeName = "CustomScheme";

        var customSchemeTokenParameters = new TokenParameters
        {
            Audience = "CustomSchemeAudience",
            Issuer = "CustomSchemeIssuer",
            SecretKey = Guid.NewGuid().ToString(),
        };

        TestContext = new TestScenarioBuilder(configureGraphQLDefaults: false)
            .ConfigureServices(services =>
            {
                services.AddAuthorization();
                services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect("localhost:3333"));

                services.AddAuthentication()
                    // Add the default JWT bearer configuration
                    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
                    {
                        options.Audience = JwtUtil.Audience;
                        options.IncludeErrorDetails = true;
                        options.RequireHttpsMetadata = false;
                        options.TokenValidationParameters = JwtUtil.GetTokenValidationParameters();
                    })
                    // Add the custom JWT bearer configuration
                    .AddJwtBearer(customSchemeName, options =>
                    {
                        options.Audience = customSchemeTokenParameters.Audience;
                        options.IncludeErrorDetails = true;
                        options.RequireHttpsMetadata = false;
                        options.TokenValidationParameters = JwtUtil.GetTokenValidationParameters(customSchemeTokenParameters);
                    });
            })
            .ConfigureGraphQL(builder => builder
                .AddMutationConventions()
                .AddRedisSubscriptions(_ => TestContext.ServiceProvider.GetRequiredService<IConnectionMultiplexer>(), new SubscriptionOptions
                {
                    TopicPrefix = Guid.NewGuid().ToString(),
                })
                .AddReplication(options => options.Security.TryAddSubscriptionAuthenticationScheme(customSchemeName)))
            .Build();

        var workspace = await TestContext.CreateWorkspaceAsync(TestContext.CancellationToken);
        var customSchemeUser = await TestContext.CreateUserAsync(workspace, UserRole.WorkspaceAdmin, TestContext.CancellationToken, customSchemeTokenParameters);

        // Create subscription client with custom scheme token
        await using var subscriptionClient = await TestContext.Factory.CreateGraphQLSubscriptionClientAsync(
            TestContext.CancellationToken,
            bearerToken: customSchemeUser.JwtAccessToken);

        var subscriptionQuery = new SubscriptionQueryBuilderGql()
            .WithStreamWorkspace(new WorkspacePullBulkQueryBuilderGql().WithAllFields())
            .Build();

        // Act
        var subscriptionTask = CollectSubscriptionDataAsync(subscriptionClient, subscriptionQuery, TestContext.CancellationToken);

        // Create a workspace to trigger a subscription event
        await TestContext.HttpClient.CreateWorkspaceAsync(TestContext.CancellationToken, customSchemeUser.JwtAccessToken);

        // Assert
        var subscriptionResponses = await subscriptionTask;
        subscriptionResponses.Should().NotBeEmpty("The subscription should successfully receive events when using the correct authentication scheme");

        // Verify the received event data
        var response = subscriptionResponses[0];
        response.Errors.Should().BeNullOrEmpty();
        response.Data.Should().NotBeNull();
        response.Data?.StreamWorkspace.Should().NotBeNull();
        response.Data?.StreamWorkspace?.Documents.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Subscription_WithCustomAuthScheme_ShouldRejectTokenFromInvalidScheme()
    {
        // Arrange
        var customSchemeTokenParameters = new TokenParameters
        {
            Audience = "CustomSchemeAudience",
            Issuer = "CustomSchemeIssuer",
            SecretKey = Guid.NewGuid().ToString(),
        };

        TestContext = new TestScenarioBuilder(configureGraphQLDefaults: false)
            .ConfigureServices(services =>
            {
                services.AddAuthorization();
                services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect("localhost:3333"));

                services.AddAuthentication()
                    // Only add the default JWT bearer configuration
                    // without configuring the custom scheme
                    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
                    {
                        options.Audience = JwtUtil.Audience;
                        options.IncludeErrorDetails = true;
                        options.RequireHttpsMetadata = false;
                        options.TokenValidationParameters = JwtUtil.GetTokenValidationParameters();
                    });
            })
            .ConfigureGraphQL(builder => builder
                .AddMutationConventions()
                .AddRedisSubscriptions(_ => TestContext.ServiceProvider.GetRequiredService<IConnectionMultiplexer>(),
                    new SubscriptionOptions { TopicPrefix = Guid.NewGuid().ToString(), })
                .AddReplication(options => options.Security.TryAddSubscriptionAuthenticationScheme(JwtBearerDefaults.AuthenticationScheme)))
            .Build();

        var workspace = await TestContext.CreateWorkspaceAsync(TestContext.CancellationToken);
        var customSchemeUser = await TestContext.CreateUserAsync(workspace, UserRole.WorkspaceAdmin, TestContext.CancellationToken, customSchemeTokenParameters);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<IOException>(async () =>
        {
            await TestContext.Factory.CreateGraphQLSubscriptionClientAsync(
                TestContext.CancellationToken,
                bearerToken: customSchemeUser.JwtAccessToken);
        });

        exception.Message.Should().Be("WebSocket connection closed unexpectedly");
    }

    private static async Task<List<GqlSubscriptionResponse>> CollectSubscriptionDataAsync(
        GraphQLSubscriptionClient subscriptionClient,
        string subscriptionQuery,
        CancellationToken cancellationToken,
        TimeSpan? collectTimespan = null,
        int maxResponses = 1)
    {
        var responses = new List<GqlSubscriptionResponse>();

        collectTimespan ??= TimeSpan.FromSeconds(5);

        using var timeoutCts = new CancellationTokenSource(collectTimespan.Value);
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            await foreach (var response in subscriptionClient.SubscribeAndCollectAsync<GqlSubscriptionResponse>(
                             subscriptionQuery,
                             combinedCts.Token))
            {
                responses.Add(response);

                if (responses.Count >= maxResponses)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException) when (combinedCts.IsCancellationRequested)
        {
            // Timeout occurred, but we'll still return any responses we've collected
        }

        return responses;
    }
}
