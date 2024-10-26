// tests\RxDBDotNet.Tests\Security\SubscriptionAuthenticationSchemeTests.cs

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using HotChocolate.Subscriptions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
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
        const string customScheme = "Firebase";

        // Set up signing credentials
        using var rsa = RSA.Create(2048);
        var signingKey = new RsaSecurityKey(rsa);
        var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256);

        // Generate Firebase-style token
        var tokenString = GenerateTestToken(
            issuer: "https://securetoken.google.com/rxdbdotnet",
            audience: "rxdbdotnet",
            signingCredentials);

        TestContext = new TestScenarioBuilder(configureGraphQLDefaults: false)
            .ConfigureServices(services =>
            {
                services.AddAuthorization();
                services.AddSingleton<IConnectionMultiplexer>(_ =>
                    ConnectionMultiplexer.Connect("localhost:3333"));

                services.AddAuthentication()
                    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
                    {
                        // Standard Bearer config for API endpoints
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            IssuerSigningKey = signingKey,
                            ValidateIssuerSigningKey = true,
                            ValidateIssuer = true,
                            ValidIssuer = "test-issuer",
                            ValidateAudience = true,
                            ValidAudience = "test-audience",
                            ValidateLifetime = true,
                        };
                    })
                    .AddJwtBearer(customScheme, options =>
                    {
                        // Firebase config for subscriptions
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            IssuerSigningKey = signingKey,
                            ValidateIssuerSigningKey = true,
                            ValidateIssuer = true,
                            ValidIssuer = "https://securetoken.google.com/rxdbdotnet",
                            ValidateAudience = true,
                            ValidAudience = "rxdbdotnet",
                            ValidateLifetime = true,
                        };
                    });
            })
            .ConfigureGraphQL(builder => builder
                .AddMutationConventions()
                .AddRedisSubscriptions(_ => TestContext.ServiceProvider.GetRequiredService<IConnectionMultiplexer>(), new SubscriptionOptions
                {
                    TopicPrefix = Guid.NewGuid().ToString(),
                })
                .AddReplication(options => options.Security.SubscriptionAuthenticationScheme = customScheme))
            .Build();

        // Create subscription client with Firebase token
        await using var subscriptionClient = await TestContext.Factory.CreateGraphQLSubscriptionClientAsync(
            TestContext.CancellationToken,
            bearerToken: tokenString);

        var subscriptionQuery = new SubscriptionQueryBuilderGql()
            .WithStreamWorkspace(new WorkspacePullBulkQueryBuilderGql().WithAllFields())
            .Build();

        // Act
        var subscriptionTask = CollectSubscriptionDataAsync(subscriptionClient, subscriptionQuery, TestContext.CancellationToken);

        // Create a workspace to trigger a subscription event
        await TestContext.HttpClient.CreateWorkspaceAsync(TestContext.CancellationToken);

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
    public async Task Subscription_WithCustomAuthScheme_ShouldRejectTokenFromDifferentScheme()
    {
        // Arrange
        const string customScheme = "Firebase";

        // Set up signing credentials
        using var rsa = RSA.Create(2048);
        var signingKey = new RsaSecurityKey(rsa);
        var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256);

        // Generate token using standard Bearer configuration
        var tokenString = GenerateTestToken(
            issuer: "test-issuer",
            audience: "test-audience",
            signingCredentials);

        TestContext = new TestScenarioBuilder(configureGraphQLDefaults: false)
            .ConfigureServices(services =>
            {
                services.AddAuthorization();
                services.AddSingleton<IConnectionMultiplexer>(_ =>
                    ConnectionMultiplexer.Connect("localhost:3333"));

                services.AddAuthentication()
                    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
                    {
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            IssuerSigningKey = signingKey,
                            ValidateIssuerSigningKey = true,
                            ValidateIssuer = true,
                            ValidIssuer = "test-issuer",
                            ValidateAudience = true,
                            ValidAudience = "test-audience",
                        };
                    })
                    .AddJwtBearer(customScheme, options =>
                    {
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            IssuerSigningKey = signingKey,
                            ValidateIssuerSigningKey = true,
                            ValidateIssuer = true,
                            ValidIssuer = "https://securetoken.google.com/rxdbdotnet",
                            ValidateAudience = true,
                            ValidAudience = "rxdbdotnet",
                        };
                    });
            })
            .ConfigureGraphQL(builder => builder
                .AddMutationConventions()
                .AddRedisSubscriptions(_ => TestContext.ServiceProvider.GetRequiredService<IConnectionMultiplexer>(), new SubscriptionOptions
                {
                    TopicPrefix = Guid.NewGuid().ToString(),
                })
                .AddReplication(options => options.Security.SubscriptionAuthenticationScheme = customScheme))
            .Build();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<IOException>(async () =>
        {
            await TestContext.Factory.CreateGraphQLSubscriptionClientAsync(
                TestContext.CancellationToken,
                bearerToken: tokenString);
        });

        exception.Message.Should().Be("The WebSocket connection was closed prematurely.");
    }

    private static string GenerateTestToken(
        string issuer,
        string audience,
        SigningCredentials signingCredentials)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: signingCredentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
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
