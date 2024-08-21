using System.Diagnostics;
using FluentAssertions;
using RxDBDotNet.Tests.Helpers;
using RxDBDotNet.Tests.Model;
using RxDBDotNet.Tests.Setup;

namespace RxDBDotNet.Tests;

public class SubscriptionTests
{
    [Fact]
    public async Task TestCase5_1_CreateWorkspaceShouldPropagateNewWorkspaceThroughTheSubscriptionAsync()
    {
        TestContext? testContext = null;

        try
        {
            // Arrange
            testContext = await TestSetupUtil.SetupAsync();

            await using var subscriptionClient = await testContext.Factory.CreateGraphQLSubscriptionClientAsync(testContext.CancellationToken);

            var subscriptionQuery = new SubscriptionQueryBuilderGql().WithStreamWorkspace(new WorkspacePullBulkQueryBuilderGql()
                    .WithDocuments(new WorkspaceQueryBuilderGql().WithAllFields())
                    .WithCheckpoint(new CheckpointQueryBuilderGql().WithAllFields()), new WorkspaceInputHeadersGql
                {
                    Authorization = "test-auth-token",
                })
                .Build();

            // Start the subscription task before creating the workspace
            // so that we do not miss subscription data
            var subscriptionTask = CollectSubscriptionDataAsync(subscriptionClient, subscriptionQuery, testContext.CancellationToken, maxResponses: 3);

            // Ensure the subscription is established
            await Task.Delay(1000, testContext.CancellationToken);

            // Act
            var newWorkspace = await testContext.HttpClient.CreateNewWorkspaceAsync(testContext.CancellationToken);

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
                .Be(newWorkspace.IsDeleted?.Value, "The streamed workspace IsDeleted status should match the created workspace");
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
        finally
        {
            if (testContext != null)
            {
                await testContext.DisposeAsync();
            }
        }
    }

    [Fact]
    public async Task TestCase5_1_1_UpdateWorkspaceShouldPropagateNewWorkspaceThroughTheSubscription()
    {
        TestContext? testContext = null;

        try
        {
            // Arrange
            testContext = await TestSetupUtil.SetupAsync();

            var newWorkspace = await testContext.HttpClient.CreateNewWorkspaceAsync(testContext.CancellationToken);

        await using var subscriptionClient = await testContext.Factory.CreateGraphQLSubscriptionClientAsync(testContext.CancellationToken);

        var subscriptionQuery = new SubscriptionQueryBuilderGql().WithStreamWorkspace(new WorkspacePullBulkQueryBuilderGql()
                .WithDocuments(new WorkspaceQueryBuilderGql().WithAllFields())
                .WithCheckpoint(new CheckpointQueryBuilderGql().WithAllFields()), new WorkspaceInputHeadersGql
                {
                    Authorization = "test-auth-token",
                })
            .Build();

        // Start the subscription task before creating the workspace
        // so that we do not miss subscription data
        var subscriptionTask = CollectSubscriptionDataAsync(subscriptionClient, subscriptionQuery, testContext.CancellationToken, maxResponses: 3);

        // Ensure the subscription is established
        await Task.Delay(1000, testContext.CancellationToken);

        // Act
        var updatedWorkspace = await testContext.HttpClient.UpdateWorkspaceAsync(newWorkspace, testContext.CancellationToken);

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
            .Be(newWorkspace.Id?.Value, "The checkpoint's LastDocumentId should match the new workspace's ID");
        subscriptionResponse.Data?.StreamWorkspace?.Checkpoint?.UpdatedAt.Should()
            .BeCloseTo(updatedWorkspace.UpdatedAt, TimeSpan.FromSeconds(5),
                "The checkpoint's UpdatedAt should be close to the new workspace's timestamp");
        }
        finally
        {
            if (testContext != null)
            {
                await testContext.DisposeAsync();
            }
        }
    }

    [Fact]
    public async Task TestCase5_2_ASubscriptionCanBeFilteredByTopic()
    {
        TestContext? testContext = null;

        try
        {
            // Arrange
            testContext = await TestSetupUtil.SetupAsync();

            var workspace1 = await testContext.HttpClient.CreateNewWorkspaceAsync(testContext.CancellationToken);
        var workspace2 = await testContext.HttpClient.CreateNewWorkspaceAsync(testContext.CancellationToken);
        var workspace3 = await testContext.HttpClient.CreateNewWorkspaceAsync(testContext.CancellationToken);

        await using var subscriptionClient = await testContext.Factory.CreateGraphQLSubscriptionClientAsync(testContext.CancellationToken);

        Debug.Assert(workspace3.Id != null, "workspace3.Id != null");

        // Only subscribe to update events for workspace3
        List<string> topics = [workspace3.Id.Value.ToString()];

        var subscriptionQuery = new SubscriptionQueryBuilderGql()
            .WithStreamWorkspace(new WorkspacePullBulkQueryBuilderGql().WithAllFields(),
                new WorkspaceInputHeadersGql
                {
                    Authorization = "test-auth-token",
                }, topics)
            .Build();

        // Start the subscription task before creating the workspace
        // so that we do not miss subscription data
        // using var collectTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        // var collectTimeoutToken = collectTimeout.Token;
        var subscriptionTask = CollectSubscriptionDataAsync(subscriptionClient, subscriptionQuery, testContext.CancellationToken);

        // Ensure the subscription is established
        await Task.Delay(1000, testContext.CancellationToken);

        await testContext.HttpClient.UpdateWorkspaceAsync(workspace1, testContext.CancellationToken);
        await testContext.HttpClient.UpdateWorkspaceAsync(workspace2, testContext.CancellationToken);
        // Update workspace 3 twice
        var updatedWorkspace3 = await testContext.HttpClient.UpdateWorkspaceAsync(workspace3, testContext.CancellationToken);
        await testContext.HttpClient.UpdateWorkspaceAsync(updatedWorkspace3, testContext.CancellationToken);

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
                .Be(workspace3.Id);
        }
        }
        finally
        {
            if (testContext != null)
            {
                await testContext.DisposeAsync();
            }
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
}
