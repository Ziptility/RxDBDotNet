// tests\RxDBDotNet.Tests\SubscriptionTests.cs
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using HotChocolate.Execution;
using HotChocolate.Subscriptions;
using LiveDocs.GraphQLApi.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Tokens;
using Moq;
using RxDBDotNet.Models;
using RxDBDotNet.Tests.Model;
using RxDBDotNet.Tests.Utils;
using static RxDBDotNet.Tests.Setup.Strings;

namespace RxDBDotNet.Tests;

[Collection("DockerSetup")]
public class SubscriptionTests : IAsyncLifetime
{
    private TestContext TestContext { get; set; } = null!;

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await TestContext.DisposeAsync();
    }

    /// <summary>
    /// Tests the WebSocketJwtAuthInterceptor's ability to handle JWT authentication with OpenID Connect (OIDC) configuration.
    /// </summary>
    /// <remarks>
    /// This test verifies that:
    /// 1. The interceptor can successfully retrieve and use OIDC configuration for token validation.
    /// 2. A valid JWT token allows the establishment of a GraphQL subscription.
    /// 3. The subscription can receive events after successful authentication.
    /// <para>
    /// The test is structured as follows:
    /// - Arrange:
    ///   a) Create a test RSA key and JWT token.
    ///   b) Mock the OIDC configuration manager to return a known configuration.
    ///   c) Configure the test scenario with custom JWT options.
    ///   d) Set up a GraphQL subscription client with the test token.
    /// - Act:
    ///   a) Attempt to establish a subscription.
    ///   b) Create a workspace to trigger a subscription event.
    /// - Assert:
    ///   Verify that the subscription receives the workspace creation event without errors.
    /// </para>
    /// This structure allows us to test the entire flow from token validation to subscription handling,
    /// ensuring that the OIDC configuration retrieval and JWT validation work correctly in the context
    /// of GraphQL subscriptions over WebSockets.
    /// </remarks>
    [Fact]
    public async Task ReplicationApiShouldSupportOidcConfiguredJwtAuthentication()
    {
        // Arrange

        // Create a test RSA key and signing credentials
        // This simulates the cryptographic material that would be used by an actual OIDC provider
        using var rsa = RSA.Create(2048);
        var signingKey = new RsaSecurityKey(rsa);
        var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256);

        // Create a test JWT token signed with our test key
        var token = CreateTestToken(signingCredentials);

        // Mock the OIDC configuration manager
        // This simulates the retrieval of OIDC configuration from a discovery endpoint
        var mockConfigManager = new Mock<IConfigurationManager<OpenIdConnectConfiguration>>();
        mockConfigManager.Setup(m => m.GetConfigurationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenIdConnectConfiguration
            {
                SigningKeys = { signingKey },
            });

        // Set up the test context with custom JWT options
        // This configuration ensures that our test uses the mocked OIDC configuration
        // and validates tokens using our test signing key
        TestContext = new TestScenarioBuilder()
            .WithAuthorization()
            .ConfigureServices(services =>
            {
                services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.Configuration = null; // Ensure we're not using any pre-configured values
                    options.ConfigurationManager = mockConfigManager.Object;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
#pragma warning disable CA5404 // we are not testing this aspect
                        ValidateAudience = false,
                        ValidateIssuer = false,
                    };
                });
            })
            .Build();

        // Create a GraphQL subscription client with our test token
        await using var subscriptionClient = await TestContext.Factory.CreateGraphQLSubscriptionClientAsync(
            TestContext.CancellationToken,
            bearerToken: token);

        // Prepare a subscription query
        var subscriptionQuery = new SubscriptionQueryBuilderGql()
            .WithStreamWorkspace(new WorkspacePullBulkQueryBuilderGql().WithAllFields())
            .Build();

        // Act

        // Attempt to establish a subscription and collect responses
        var subscriptionTask = CollectSubscriptionDataAsync(subscriptionClient, subscriptionQuery, TestContext.CancellationToken, maxResponses: 1);

        // Ensure the subscription has time to be established
        await Task.Delay(1000, TestContext.CancellationToken);

        // Create a workspace to trigger a subscription event
        // This tests that the authenticated subscription is working end-to-end
        await TestContext.HttpClient.CreateWorkspaceAsync(TestContext.CancellationToken);

        // Assert

        // Verify that we received exactly one subscription response (the workspace creation event)
        var subscriptionResponses = await subscriptionTask;
        subscriptionResponses.Should().HaveCount(1, "The subscription should have received the workspace creation event");

        // Verify that the subscription response contains no errors
        subscriptionResponses[0].Errors.Should().BeNullOrEmpty("There should be no errors in the subscription response");
    }

    private static string CreateTestToken(SigningCredentials signingCredentials)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: "test-issuer",
            audience: "test-audience",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: signingCredentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public async Task CreateWorkspaceShouldNotPropagateNewWorkspaceForAnAuthenticatedUserThroughASecuredSubscriptionAsync()
    {
        // Arrange
        TestContext = new TestScenarioBuilder()
            .WithAuthorization()
            .ConfigureReplicatedDocument<ReplicatedWorkspace>(options => options.Security.RequirePolicyToRead("IsSystemAdmin"))
        .Build();

        await TestContext.CreateWorkspaceAsync(TestContext.CancellationToken);

        Func<Task> act = async () => await TestContext.Factory.CreateGraphQLSubscriptionClientAsync(TestContext.CancellationToken);

        await act.Should().ThrowAsync<IOException>();
    }

    [Fact]
    public async Task CreateWorkspaceShouldNotPropagateNewWorkspaceForAnUnAuthorizedUserThroughASecuredSubscriptionAsync()
    {
        // Arrange
        TestContext = new TestScenarioBuilder()
            .WithAuthorization()
            .ConfigureReplicatedDocument<ReplicatedWorkspace>(options => options.Security.RequirePolicyToRead("IsSystemAdmin"))
        .Build();

        var workspace = await TestContext.CreateWorkspaceAsync(TestContext.CancellationToken);
        var workspaceAdmin = await TestContext.CreateUserAsync(workspace, UserRole.WorkspaceAdmin, TestContext.CancellationToken);

        await using var subscriptionClient = await TestContext.Factory.CreateGraphQLSubscriptionClientAsync(TestContext.CancellationToken, bearerToken: workspaceAdmin.JwtAccessToken);

        var subscriptionQuery = new SubscriptionQueryBuilderGql().WithStreamWorkspace(new WorkspacePullBulkQueryBuilderGql()
                .WithDocuments(new WorkspaceQueryBuilderGql().WithAllFields())
                .WithCheckpoint(new CheckpointQueryBuilderGql().WithAllFields()))
            .Build();

        // Start the subscription task before creating the workspace
        // so that we do not miss subscription data
        var subscriptionTask = CollectSubscriptionDataAsync(subscriptionClient, subscriptionQuery, TestContext.CancellationToken, maxResponses: 3);

        // Ensure the subscription is established
        await Task.Delay(1000, TestContext.CancellationToken);

        var workspaceId = Provider.Sql.Create();

        var newWorkspace = new WorkspaceInputGql
        {
            Id = workspaceId,
            Name = CreateString(),
            UpdatedAt = DateTimeOffset.UtcNow,
            IsDeleted = false,
            Topics = new List<string>
            {
                workspaceId.ToString(),
            },
        };

        var workspaceInputPushRowGql = new WorkspaceInputPushRowGql
        {
            AssumedMasterState = null,
            NewDocumentState = newWorkspace,
        };

        var pushWorkspaceInputGql = new PushWorkspaceInputGql
        {
            WorkspacePushRow = new List<WorkspaceInputPushRowGql?>
            {
                workspaceInputPushRowGql,
            },
        };

        var createWorkspace = new MutationQueryBuilderGql().WithPushWorkspace(new PushWorkspacePayloadQueryBuilderGql().WithAllFields()
            .WithErrors(new PushWorkspaceErrorQueryBuilderGql().WithAuthenticationErrorFragment(
                new AuthenticationErrorQueryBuilderGql().WithAllFields())), pushWorkspaceInputGql);

        // Act
        await TestContext.HttpClient.PostGqlMutationAsync(createWorkspace, TestContext.CancellationToken, workspaceAdmin.JwtAccessToken);

        // Assert
        var subscriptionResponses = await subscriptionTask;

        subscriptionResponses.Should()
            .HaveCount(1);
        var subscriptionResponse = subscriptionResponses.Single();
        subscriptionResponse.Errors.Should().HaveCount(1);
        subscriptionResponse.Errors.Single()
            .Message.Should()
            .Be("The current user is not authorized to access this resource.");
    }

    [Fact]
    public async Task CreateWorkspaceShouldPropagateNewWorkspaceForAuthorizedUserThroughASecuredSubscriptionAsync()
    {
        // Arrange
        TestContext = new TestScenarioBuilder()
            .WithAuthorization()
            .ConfigureReplicatedDocument<ReplicatedWorkspace>(options => options.Security.RequirePolicyToRead("IsWorkspaceAdmin"))
        .Build();

        var workspace = await TestContext.CreateWorkspaceAsync(TestContext.CancellationToken);
        var workspaceAdmin = await TestContext.CreateUserAsync(workspace, UserRole.WorkspaceAdmin, TestContext.CancellationToken);

        await using var subscriptionClient = await TestContext.Factory.CreateGraphQLSubscriptionClientAsync(TestContext.CancellationToken, bearerToken: workspaceAdmin.JwtAccessToken);

        var subscriptionQuery = new SubscriptionQueryBuilderGql().WithStreamWorkspace(new WorkspacePullBulkQueryBuilderGql()
                .WithDocuments(new WorkspaceQueryBuilderGql().WithAllFields())
                .WithCheckpoint(new CheckpointQueryBuilderGql().WithAllFields()))
            .Build();

        // Start the subscription task before creating the workspace
        // so that we do not miss subscription data
        var subscriptionTask = CollectSubscriptionDataAsync(subscriptionClient, subscriptionQuery, TestContext.CancellationToken, maxResponses: 3);

        // Ensure the subscription is established
        await Task.Delay(1000, TestContext.CancellationToken);

        // Act
        var (newWorkspace, _) = await TestContext.HttpClient.CreateWorkspaceAsync(TestContext.CancellationToken, workspaceAdmin.JwtAccessToken);

        // Assert
        var subscriptionResponses = await subscriptionTask;

        subscriptionResponses.Should()
            .HaveCount(1);
        var subscriptionResponse = subscriptionResponses[0];
        subscriptionResponse.Should()
            .NotBeNull("Subscription data should not be null.");
        subscriptionResponse.Errors.Should()
            .BeNullOrEmpty();
        subscriptionResponse.Data.Should()
            .NotBeNull();
        subscriptionResponse.Data?.StreamWorkspace.Should()
            .NotBeNull();
        subscriptionResponse.Data?.StreamWorkspace?.Documents.Should()
            .NotBeEmpty();

        var streamedWorkspace = subscriptionResponse.Data?.StreamWorkspace?.Documents?.First();
        streamedWorkspace.Should()
            .NotBeNull();

        // Assert that the streamed workspace properties match the newWorkspace properties
        streamedWorkspace?.Id.Should()
            .Be(newWorkspace.Id, "The streamed workspace ID should match the created workspace ID");
        streamedWorkspace?.Name.Should()
            .Be(newWorkspace.Name?.Value, "The streamed workspace name should match the created workspace name");
        streamedWorkspace?.IsDeleted.Should()
            .Be(newWorkspace.IsDeleted, "The streamed workspace IsDeleted status should match the created workspace");
        streamedWorkspace?.UpdatedAt.Should()
            .BeCloseTo(newWorkspace.UpdatedAt?.Value ?? DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5),
                "The streamed workspace UpdatedAt should be close to the created workspace's timestamp");

        // Assert on the checkpoint
        subscriptionResponse.Data?.StreamWorkspace?.Checkpoint.Should()
            .NotBeNull("The checkpoint should be present");
        subscriptionResponse.Data?.StreamWorkspace?.Checkpoint?.LastDocumentId.Should()
            .Be(newWorkspace.Id?.Value, "The checkpoint's LastDocumentId should match the new workspace's ID");
        Debug.Assert(newWorkspace.UpdatedAt != null, "newWorkspace.UpdatedAt != null");
        subscriptionResponse.Data?.StreamWorkspace?.Checkpoint?.UpdatedAt.Should()
            .BeCloseTo(newWorkspace.UpdatedAt.Value, TimeSpan.FromSeconds(5),
                "The checkpoint's UpdatedAt should be close to the new workspace's timestamp");
    }

    /// <summary>
    ///     Tests the behavior of the SubscriptionResolver when handling empty document updates.
    ///     This test validates that the resolver correctly processes updates with no documents
    ///     but with an updated checkpoint, which is a valid scenario in the RxDB replication protocol.
    /// </summary>
    /// <remarks>
    ///     In the RxDB replication protocol:
    ///     1. Empty document lists in updates are valid and should be processed.
    ///     2. The checkpoint should always be updated, even if no documents are present.
    ///     3. The client (subscriber) should receive these updates to keep its checkpoint current.
    /// </remarks>
    [Fact]
    public async Task DocumentChangedStream_ShouldHandleEmptyDocuments()
    {
        // Arrange
        TestContext = new TestScenarioBuilder().Build();
        var mockSourceStream = new Mock<ISourceStream<DocumentPullBulk<ReplicatedWorkspace>>>();
        var emptyUpdate = new DocumentPullBulk<ReplicatedWorkspace>
        {
            Documents = [],
            Checkpoint = new Checkpoint
            {
                LastDocumentId = Guid.NewGuid(),
                UpdatedAt = DateTimeOffset.UtcNow,
            },
        };

        mockSourceStream.Setup(x => x.ReadEventsAsync())
            .Returns(CreateAsyncEnumerable(emptyUpdate));

        var mockTopicEventReceiver = new Mock<ITopicEventReceiver>();
        mockTopicEventReceiver.Setup(x => x.SubscribeAsync<DocumentPullBulk<ReplicatedWorkspace>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSourceStream.Object);

        TestContext = new TestScenarioBuilder()
            .ConfigureServices(services =>
            {
                services.RemoveAll<ITopicEventReceiver>();
                services.AddSingleton(mockTopicEventReceiver.Object);
            })
            .Build();

        await using var subscriptionClient = await TestContext.Factory.CreateGraphQLSubscriptionClientAsync(TestContext.CancellationToken);

        var subscriptionQuery = new SubscriptionQueryBuilderGql().WithStreamWorkspace(new WorkspacePullBulkQueryBuilderGql().WithAllFields())
            .Build();

        // Act
        var subscriptionResponses =
            await CollectSubscriptionDataAsync(subscriptionClient, subscriptionQuery, TestContext.CancellationToken, maxResponses: 1);

        // Assert
        // Validate that we received exactly one response, as per the RxDB protocol
        // which states that each change should trigger a subscription event
        subscriptionResponses.Should()
            .HaveCount(1, "The subscription should emit an event even for empty document updates");

        var response = subscriptionResponses[0];

        // Ensure no errors occurred during the subscription process
        response.Errors.Should()
            .BeNullOrEmpty("The subscription should not produce any errors");

        // Validate that the response contains the expected data structure
        response.Data.Should()
            .NotBeNull("The subscription response should contain data");
        response.Data?.StreamWorkspace.Should()
            .NotBeNull("The StreamWorkspace field should be present in the response");

        // Check that the Documents list is empty, as we sent an empty update
        response.Data?.StreamWorkspace?.Documents.Should()
            .BeEmpty("The Documents list should be empty for an empty update");

        // Validate that the Checkpoint is correctly updated and transmitted
        // This is crucial for the RxDB replication protocol to maintain sync state
        response.Data?.StreamWorkspace?.Checkpoint.Should()
            .NotBeNull("The Checkpoint should be present even in empty updates");
        response.Data?.StreamWorkspace?.Checkpoint?.LastDocumentId.Should()
            .Be(emptyUpdate.Checkpoint.LastDocumentId, "The LastDocumentId in the response should match the one in the empty update");
        response.Data?.StreamWorkspace?.Checkpoint?.UpdatedAt.Should()
            .BeCloseTo(emptyUpdate.Checkpoint.UpdatedAt.Value, TimeSpan.FromSeconds(1),
                "The UpdatedAt timestamp should be close to the one in the empty update");
    }

    private static async IAsyncEnumerable<DocumentPullBulk<ReplicatedWorkspace>> CreateAsyncEnumerable(DocumentPullBulk<ReplicatedWorkspace> item)
    {
        await Task.Delay(10);

        yield return item;
    }

    [Fact]
    public async Task DocumentChangedStream_ShouldHandleCancellationAndDisposeStream()
    {
        // Arrange
        TestContext = new TestScenarioBuilder().Build();
        await TestContext.HttpClient.CreateWorkspaceAsync(TestContext.CancellationToken);

        await using var subscriptionClient = await TestContext.Factory.CreateGraphQLSubscriptionClientAsync(TestContext.CancellationToken);

        var subscriptionQuery = new SubscriptionQueryBuilderGql().WithStreamWorkspace(new WorkspacePullBulkQueryBuilderGql().WithAllFields())
            .Build();

        // Create a cancellation token source with a short timeout
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in subscriptionClient.SubscribeAndCollectAsync<GqlSubscriptionResponse>(subscriptionQuery, cts.Token))
            {
                // This should not be reached
                Assert.Fail("Subscription should have been cancelled");
            }
        });
    }

    [Fact]
    public async Task DocumentChangedStream_ShouldHandleExceptionAndDisposeStream()
    {
        // Arrange
        TestContext = new TestScenarioBuilder().Build();
        var mockSourceStream = new Mock<ISourceStream<DocumentPullBulk<ReplicatedWorkspace>>>();
        mockSourceStream.Setup(x => x.ReadEventsAsync())
            .Returns(ThrowAfterFirstYield);

        var disposeCalled = false;
        mockSourceStream.Setup(x => x.DisposeAsync())
            .Callback(() => disposeCalled = true)
            .Returns(ValueTask.CompletedTask);

        var mockTopicEventReceiver = new Mock<ITopicEventReceiver>();
        mockTopicEventReceiver.Setup(x => x.SubscribeAsync<DocumentPullBulk<ReplicatedWorkspace>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSourceStream.Object);

        TestContext = new TestScenarioBuilder()
            .ConfigureServices(services =>
            {
                services.RemoveAll<ITopicEventReceiver>();
                services.AddSingleton(mockTopicEventReceiver.Object);
            })
            .Build();

        await TestContext.HttpClient.CreateWorkspaceAsync(TestContext.CancellationToken);

        await using var subscriptionClient = await TestContext.Factory.CreateGraphQLSubscriptionClientAsync(TestContext.CancellationToken);

        var subscriptionQuery = new SubscriptionQueryBuilderGql().WithStreamWorkspace(new WorkspacePullBulkQueryBuilderGql().WithAllFields())
            .Build();

        // Act & Assert
        var exceptionThrown = false;
        try
        {
            await foreach (var _ in subscriptionClient.SubscribeAndCollectAsync<GqlSubscriptionResponse>(subscriptionQuery,
                               TestContext.CancellationToken))
            {
                // We expect to get here once before the exception is thrown
            }
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Test exception during enumeration"))
        {
            exceptionThrown = true;
        }

        // Assert that the exception was thrown
        Assert.True(exceptionThrown, "Expected exception was not thrown");

        // Assert that DisposeAsync was called
        Assert.True(disposeCalled, "DisposeAsync was not called on the stream");
    }

    private static IAsyncEnumerable<DocumentPullBulk<ReplicatedWorkspace>> ThrowAfterFirstYield()
    {
        return new ThrowingAsyncEnumerable();
    }

    [Fact]
    public async Task CreateWorkspaceShouldPropagateNewWorkspaceThroughTheSubscriptionAsync()
    {
        // Arrange
        TestContext = new TestScenarioBuilder().Build();
        await using var subscriptionClient = await TestContext.Factory.CreateGraphQLSubscriptionClientAsync(TestContext.CancellationToken);

        var subscriptionQuery = new SubscriptionQueryBuilderGql().WithStreamWorkspace(new WorkspacePullBulkQueryBuilderGql()
                .WithDocuments(new WorkspaceQueryBuilderGql().WithAllFields())
                .WithCheckpoint(new CheckpointQueryBuilderGql().WithAllFields()))
            .Build();

        // Start the subscription task before creating the workspace
        // so that we do not miss subscription data
        var subscriptionTask = CollectSubscriptionDataAsync(subscriptionClient, subscriptionQuery, TestContext.CancellationToken, maxResponses: 3);

        // Ensure the subscription is established
        await Task.Delay(1000, TestContext.CancellationToken);

        // Act
        var (newWorkspace, _) = await TestContext.HttpClient.CreateWorkspaceAsync(TestContext.CancellationToken);

        // Assert

        var subscriptionResponses = await subscriptionTask;

        subscriptionResponses.Should()
            .HaveCount(1);
        var subscriptionResponse = subscriptionResponses[0];
        subscriptionResponse.Should()
            .NotBeNull("Subscription data should not be null.");
        subscriptionResponse.Errors.Should()
            .BeNullOrEmpty();
        subscriptionResponse.Data.Should()
            .NotBeNull();
        subscriptionResponse.Data?.StreamWorkspace.Should()
            .NotBeNull();
        subscriptionResponse.Data?.StreamWorkspace?.Documents.Should()
            .NotBeEmpty();

        var streamedWorkspace = subscriptionResponse.Data?.StreamWorkspace?.Documents?.First();
        streamedWorkspace.Should()
            .NotBeNull();

        // Assert that the streamed workspace properties match the newWorkspace properties
        streamedWorkspace?.Id.Should()
            .Be(newWorkspace.Id, "The streamed workspace ID should match the created workspace ID");
        streamedWorkspace?.Name.Should()
            .Be(newWorkspace.Name?.Value, "The streamed workspace name should match the created workspace name");
        streamedWorkspace?.IsDeleted.Should()
            .Be(newWorkspace.IsDeleted, "The streamed workspace IsDeleted status should match the created workspace");
        streamedWorkspace?.UpdatedAt.Should()
            .BeCloseTo(newWorkspace.UpdatedAt?.Value ?? DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5),
                "The streamed workspace UpdatedAt should be close to the created workspace's timestamp");

        // Assert on the checkpoint
        subscriptionResponse.Data?.StreamWorkspace?.Checkpoint.Should()
            .NotBeNull("The checkpoint should be present");
        subscriptionResponse.Data?.StreamWorkspace?.Checkpoint?.LastDocumentId.Should()
            .Be(newWorkspace.Id?.Value, "The checkpoint's LastDocumentId should match the new workspace's ID");
        Debug.Assert(newWorkspace.UpdatedAt != null, "newWorkspace.UpdatedAt != null");
        subscriptionResponse.Data?.StreamWorkspace?.Checkpoint?.UpdatedAt.Should()
            .BeCloseTo(newWorkspace.UpdatedAt.Value, TimeSpan.FromSeconds(5),
                "The checkpoint's UpdatedAt should be close to the new workspace's timestamp");
    }

    [Fact]
    public async Task UpdateWorkspaceShouldPropagateNewWorkspaceThroughTheSubscription()
    {
        // Arrange
        TestContext = new TestScenarioBuilder().Build();
        var (workspaceInputGql, _) = await TestContext.HttpClient.CreateWorkspaceAsync(TestContext.CancellationToken);

        await using var subscriptionClient = await TestContext.Factory.CreateGraphQLSubscriptionClientAsync(TestContext.CancellationToken);

        var subscriptionQuery = new SubscriptionQueryBuilderGql().WithStreamWorkspace(new WorkspacePullBulkQueryBuilderGql()
                .WithDocuments(new WorkspaceQueryBuilderGql().WithAllFields())
                .WithCheckpoint(new CheckpointQueryBuilderGql().WithAllFields()))
            .Build();

        // Start the subscription task before creating the workspace
        // so that we do not miss subscription data
        var subscriptionTask = CollectSubscriptionDataAsync(subscriptionClient, subscriptionQuery, TestContext.CancellationToken, maxResponses: 3);

        // Ensure the subscription is established
        await Task.Delay(1000, TestContext.CancellationToken);

        // Act
        var updatedWorkspace = await TestContext.HttpClient.UpdateWorkspaceAsync(workspaceInputGql, TestContext.CancellationToken);

        // Assert
        var subscriptionResponses = await subscriptionTask;

        subscriptionResponses.Should()
            .HaveCount(1);
        var subscriptionResponse = subscriptionResponses[0];
        subscriptionResponse.Should()
            .NotBeNull("Subscription data should not be null.");
        subscriptionResponse.Errors.Should()
            .BeNullOrEmpty();
        subscriptionResponse.Data.Should()
            .NotBeNull();
        subscriptionResponse.Data?.StreamWorkspace.Should()
            .NotBeNull();
        subscriptionResponse.Data?.StreamWorkspace?.Documents.Should()
            .NotBeEmpty();

        var streamedWorkspace = subscriptionResponse.Data?.StreamWorkspace?.Documents?.First();
        streamedWorkspace.Should()
            .NotBeNull();

        // Assert that the streamed workspace properties match the newWorkspace properties
        streamedWorkspace?.Id.Should()
            .Be(updatedWorkspace.Id);
        streamedWorkspace?.Name.Should()
            .Be(updatedWorkspace.Name);
        streamedWorkspace?.UpdatedAt.Should()
            .BeCloseTo(updatedWorkspace.UpdatedAt, TimeSpan.FromSeconds(5));

        // Assert on the checkpoint
        subscriptionResponse.Data?.StreamWorkspace?.Checkpoint.Should()
            .NotBeNull("The checkpoint should be present");
        subscriptionResponse.Data?.StreamWorkspace?.Checkpoint?.LastDocumentId.Should()
            .Be(workspaceInputGql.Id?.Value, "The checkpoint's LastDocumentId should match the new workspace's ID");
        subscriptionResponse.Data?.StreamWorkspace?.Checkpoint?.UpdatedAt.Should()
            .BeCloseTo(updatedWorkspace.UpdatedAt, TimeSpan.FromSeconds(5),
                "The checkpoint's UpdatedAt should be close to the new workspace's timestamp");
    }

    [Fact]
    public async Task ASubscriptionCanBeFilteredByTopic()
    {
        // Arrange
        TestContext = new TestScenarioBuilder().Build();
        var (workspaceInputGql, _) = await TestContext.HttpClient.CreateWorkspaceAsync(TestContext.CancellationToken);
        var workspace2 = await TestContext.HttpClient.CreateWorkspaceAsync(TestContext.CancellationToken);
        var workspace3 = await TestContext.HttpClient.CreateWorkspaceAsync(TestContext.CancellationToken);

        await using var subscriptionClient = await TestContext.Factory.CreateGraphQLSubscriptionClientAsync(TestContext.CancellationToken);

        Debug.Assert(workspace3.workspaceInputGql.Id != null, "workspace3.Id != null");

        // Only subscribe to update events for workspace3
        List<string> topics = [workspace3.workspaceInputGql.Id.Value.ToString()];

        var subscriptionQuery = new SubscriptionQueryBuilderGql()
            .WithStreamWorkspace(new WorkspacePullBulkQueryBuilderGql().WithAllFields(), topics: topics)
            .Build();

        // Start the subscription task before creating the workspace
        // so that we do not miss subscription data
        // using var collectTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        // var collectTimeoutToken = collectTimeout.Token;
        var subscriptionTask = CollectSubscriptionDataAsync(subscriptionClient, subscriptionQuery, TestContext.CancellationToken);

        // Ensure the subscription is established
        await Task.Delay(1000, TestContext.CancellationToken);

        await TestContext.HttpClient.UpdateWorkspaceAsync(workspaceInputGql, TestContext.CancellationToken);
        await TestContext.HttpClient.UpdateWorkspaceAsync(workspace2.workspaceInputGql, TestContext.CancellationToken);
        // Update workspace 3 twice
        var updatedWorkspace3 = await TestContext.HttpClient.UpdateWorkspaceAsync(workspace3.workspaceInputGql, TestContext.CancellationToken);
        await TestContext.HttpClient.UpdateWorkspaceAsync(updatedWorkspace3, TestContext.CancellationToken);

        var subscriptionResponses = await subscriptionTask;
        subscriptionResponses.Should()
            .NotBeNull();
        subscriptionResponses.Should()
            .HaveCount(2, "Should have received one response for each update to workspace3");

        foreach (var subscriptionResponse in subscriptionResponses)
        {
            subscriptionResponse.Errors.Should()
                .BeNullOrEmpty();
            subscriptionResponse.Data.Should()
                .NotBeNull();
            subscriptionResponse.Data?.StreamWorkspace.Should()
                .NotBeNull();
            subscriptionResponse.Data?.StreamWorkspace?.Documents.Should()
                .HaveCount(1);

            var streamedWorkspace = subscriptionResponse.Data?.StreamWorkspace?.Documents?.Single();

            Debug.Assert(streamedWorkspace != null, nameof(streamedWorkspace) + " != null");

            streamedWorkspace.Id.Should()
                .Be(workspace3.workspaceInputGql.Id);
        }
    }

    private static async Task<List<GqlSubscriptionResponse>> CollectSubscriptionDataAsync(
        GraphQLSubscriptionClient subscriptionClient,
        string subscriptionQuery,
        CancellationToken cancellationToken,
        TimeSpan? collectTimespan = null,
        int maxResponses = 10)
    {
        var responses = new List<GqlSubscriptionResponse>();

        collectTimespan ??= TimeSpan.FromSeconds(2);

        // Create a CancellationTokenSource with the specified timeout
        using var timeoutCts = new CancellationTokenSource(collectTimespan.Value);

        // Create a linked token source combining the original token and the timeout token
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            await foreach (var response in subscriptionClient.SubscribeAndCollectAsync<GqlSubscriptionResponse>(subscriptionQuery, combinedCts.Token))
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

    private sealed class ThrowingAsyncEnumerable : IAsyncEnumerable<DocumentPullBulk<ReplicatedWorkspace>>
    {
        public async IAsyncEnumerator<DocumentPullBulk<ReplicatedWorkspace>> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            yield return new DocumentPullBulk<ReplicatedWorkspace>
            {
                Documents = [],
                Checkpoint = new Checkpoint
                {
                    LastDocumentId = Guid.NewGuid(),
                    UpdatedAt = DateTimeOffset.UtcNow,
                },
            };

            await Task.Delay(1, cancellationToken);

            throw new InvalidOperationException("Test exception during enumeration");
        }
    }
}
