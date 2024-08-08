using System.Diagnostics;
using FluentAssertions;
using RxDBDotNet.Tests.Helpers;
using RxDBDotNet.Tests.Model;
using Xunit.Abstractions;

namespace RxDBDotNet.Tests;

public class SubscriptionTests(ITestOutputHelper output) : TestBase(output)
{
    [Fact]
    public async Task TestCase5_1_CreateWorkspaceShouldPropagateNewWorkspaceThroughTheSubscription()
    {
        // Arrange
        using var timeoutTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var timeoutToken = timeoutTokenSource.Token;

        await using var subscriptionClient = await Factory.CreateGraphQLSubscriptionClientAsync(timeoutToken);

        var subscriptionQuery = new SubscriptionQueryBuilderGql().WithStreamWorkspace(new WorkspacePullBulkQueryBuilderGql()
                .WithDocuments(new WorkspaceQueryBuilderGql().WithAllFields())
                .WithCheckpoint(new CheckpointQueryBuilderGql().WithAllFields()), new WorkspaceInputHeadersGql
            {
                Authorization = "test-auth-token",
            })
            .Build();

        // Start the subscription task before creating the workspace
        // so that we do not miss subscription data
        var subscriptionTask = CollectSubscriptionDataAsync(
            subscriptionClient,
            subscriptionQuery,
            timeoutToken,
            maxResponses: 3);

        // Ensure the subscription is established
        await Task.Delay(1000, timeoutToken);

        // Act
        var newWorkspace = await HttpClient.CreateNewWorkspaceAsync();

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
    //
    // [Fact]
    // public async Task TestCase5_2_SubscriptionsSupportFilteringByDocumentProperties()
    // {
    //     // Arrange
    //     var workspace1 = await HttpClient.CreateNewWorkspaceAsync();
    //     var workspace2 = await HttpClient.CreateNewWorkspaceAsync();
    //     var workspace3 = await HttpClient.CreateNewWorkspaceAsync();
    //
    //     await using var subscriptionClient = await Factory.CreateGraphQLSubscriptionClientAsync();
    //
    //     Debug.Assert(workspace3.Name != null, "workspace3.Name != null");
    //
    //     var filterWorkspaceByName = new WorkspaceFilterInputGql
    //     {
    //         Name = new StringOperationFilterInputGql
    //         {
    //             Eq = workspace3.Name.Value,
    //         },
    //     };
    //
    //     // Filter the subscription to only receive updates for workspace3
    //     var subscriptionQuery = new SubscriptionQueryBuilderGql()
    //         .WithStreamWorkspace(new WorkspacePullBulkQueryBuilderGql()
    //             .WithDocuments(new WorkspaceQueryBuilderGql().WithAllFields(), where: filterWorkspaceByName)
    //             .WithCheckpoint(new CheckpointQueryBuilderGql().WithAllFields()),
    //             new WorkspaceInputHeadersGql { Authorization = "test-auth-token" })
    //         .Build();
    //
    //     var subscription = subscriptionClient.SubscribeAsync<GqlSubscriptionResponse>(subscriptionQuery);
    //
    //     using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    //
    //     // Start listening for subscription data
    //     var subscriptionTask = ListenForSubscriptionDataAsync(subscription, cts.Token);
    //
    //     // Ensure the subscription is established before updating workspace3
    //     await Task.Delay(1000, cts.Token);
    //
    //     await HttpClient.UpdateWorkspaceAsync(workspace1);
    //     await HttpClient.UpdateWorkspaceAsync(workspace2);
    //     // Update workspace 3 twice
    //     var updatedWorkspace3_1 = await HttpClient.UpdateWorkspaceAsync(workspace3);
    //     var updatedWorkspace3_2 = await HttpClient.UpdateWorkspaceAsync(workspace3);
    //
    //     // Wait for the subscription data or timeout
    //     var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5), cts.Token);
    //     var completedTask = await Task.WhenAny(subscriptionTask, timeoutTask);
    //
    //     // Assert
    //     if (completedTask == timeoutTask)
    //     {
    //         throw new TimeoutException("Subscription data was not received within the expected timeframe.");
    //     }
    //
    //     var receivedData = await subscriptionTask;
    //     receivedData.Should().NotBeNull("Subscription data should not be null.");
    //     receivedData.Errors.Should().BeNullOrEmpty();
    //     receivedData.Data.Should().NotBeNull();
    //     receivedData.Data?.StreamWorkspace.Should().NotBeNull();
    //     receivedData.Data?.StreamWorkspace?.Documents.Should().NotBeEmpty();
    //
    //     var streamedWorkspace = receivedData.Data?.StreamWorkspace?.Documents?.First();
    //     streamedWorkspace.Should().NotBeNull();
    //
    //     // Assert that the streamed workspace properties match the newWorkspace properties
    //     streamedWorkspace?.Id.Should().Be(updatedWorkspace3.Id, "The streamed workspace ID should match the created workspace ID");
    //     streamedWorkspace?.Name.Should().Be(updatedWorkspace3.Name?.Value, "The streamed workspace name should match the created workspace name");
    //     streamedWorkspace?.IsDeleted.Should().Be(updatedWorkspace3.IsDeleted?.Value, "The streamed workspace IsDeleted status should match the created workspace");
    //     streamedWorkspace?.UpdatedAt.Should().BeCloseTo(updatedWorkspace3.UpdatedAt?.Value ?? DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5), "The streamed workspace UpdatedAt should be close to the created workspace's timestamp");
    //
    //     // Assert on the checkpoint
    //     receivedData.Data?.StreamWorkspace?.Checkpoint.Should().NotBeNull("The checkpoint should be present");
    //     receivedData.Data?.StreamWorkspace?.Checkpoint?.LastDocumentId.Should().Be(updatedWorkspace3.Id?.Value, "The checkpoint's LastDocumentId should match the new workspace's ID");
    //     Debug.Assert(updatedWorkspace3.UpdatedAt != null, "newWorkspace.UpdatedAt != null");
    //     receivedData.Data?.StreamWorkspace?.Checkpoint?.UpdatedAt.Should().BeCloseTo(updatedWorkspace3.UpdatedAt.Value, TimeSpan.FromSeconds(5), "The checkpoint's UpdatedAt should be close to the new workspace's timestamp");
    // }

    private static async Task<List<GqlSubscriptionResponse>> CollectSubscriptionDataAsync(
        GraphQLSubscriptionClient subscriptionClient,
        string subscriptionQuery,
        CancellationToken cancellationToken,
        int maxResponses = 10)
    {
        var responses = new List<GqlSubscriptionResponse>();

        try
        {
            await foreach (var response in subscriptionClient.SubscribeAndCollectAsync<GqlSubscriptionResponse>(subscriptionQuery, cancellationToken))
            {
                responses.Add(response);

                if (responses.Count >= maxResponses)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Timeout occurred, but we'll still return any responses we've collected
        }

        return responses;
    }
}
