using RxDBDotNet.Tests.Model;
using RxDBDotNet.Tests.Utils;

namespace RxDBDotNet.Tests;

[Collection("DockerSetup")]
public class AdditionalCoverageTests : IAsyncLifetime
{
    private TestContext _testContext = null!;

    public Task InitializeAsync()
    {
        _testContext = TestSetupUtil.Setup();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _testContext.DisposeAsync();
    }

    [Fact]
    public async Task PushMultipleWorkspacesShouldHandleConflictsCorrectly()
    {
        // Arrange
        var (workspace1, _) = await _testContext.HttpClient.CreateWorkspaceAsync(_testContext.CancellationToken);
        var (workspace2, _) = await _testContext.HttpClient.CreateWorkspaceAsync(_testContext.CancellationToken);

        // Modify workspace1
        workspace1.Name = Strings.CreateString();
        workspace1.UpdatedAt = DateTimeOffset.UtcNow;

        // Create a conflict for workspace2
        var conflictingWorkspace2 = new WorkspaceInputGql
        {
            Id = workspace2.Id,
            Name = Strings.CreateString(),
            UpdatedAt = DateTimeOffset.UtcNow.AddSeconds(1),
            IsDeleted = false,
            Topics = workspace2.Topics,
        };

        var pushRows = new List<WorkspaceInputPushRowGql?>
        {
            new()
            {
                AssumedMasterState = workspace1,
                NewDocumentState = workspace1,
            },
            new()
            {
                AssumedMasterState = workspace2,
                NewDocumentState = conflictingWorkspace2,
            },
        };

        var pushWorkspaceInputGql = new PushWorkspaceInputGql
        {
            WorkspacePushRow = pushRows,
        };

        var pushWorkspaceMutation = new MutationQueryBuilderGql()
            .WithPushWorkspace(new PushWorkspacePayloadQueryBuilderGql().WithAllFields(), pushWorkspaceInputGql);

        // Act
        var response = await _testContext.HttpClient.PostGqlMutationAsync(pushWorkspaceMutation, _testContext.CancellationToken);

        // Assert
        response.Errors.Should().BeNullOrEmpty();
        response.Data.PushWorkspace.Should().NotBeNull();
        response.Data.PushWorkspace?.Workspace.Should().HaveCount(1);
        response.Data.PushWorkspace?.Workspace?.First().Id.Should().Be(workspace1.Id!.Value);
        response.Data.PushWorkspace?.Errors.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task PullWorkspacesWithComplexFiltering()
    {
        // Arrange
        var workspace1 = await _testContext.HttpClient.CreateWorkspaceAsync(_testContext.CancellationToken);
        var workspace2 = await _testContext.HttpClient.CreateWorkspaceAsync(_testContext.CancellationToken);
        await _testContext.HttpClient.CreateWorkspaceAsync(_testContext.CancellationToken);

        var filter = new WorkspaceFilterInputGql
        {
            Or = new List<WorkspaceFilterInputGql>
            {
                new()
                {
                    Id = new UuidOperationFilterInputGql { Eq = workspace1.workspaceInputGql.Id?.Value },
                },
                new()
                {
                    And = new List<WorkspaceFilterInputGql>
                    {
                        new()
                        {
                            Name = new StringOperationFilterInputGql { Contains = workspace2.workspaceInputGql.Name?.Value },
                        },
                        new()
                        {
                            IsDeleted = new BooleanOperationFilterInputGql { Eq = false },
                        },
                    },
                },
            },
        };

        var query = new QueryQueryBuilderGql()
            .WithPullWorkspace(new WorkspacePullBulkQueryBuilderGql().WithAllFields(), 1000, where: filter);

        // Act
        var response = await _testContext.HttpClient.PostGqlQueryAsync(query, _testContext.CancellationToken);

        // Assert
        response.Errors.Should().BeNullOrEmpty();
        response.Data.PullWorkspace.Should().NotBeNull();
        response.Data.PullWorkspace?.Documents.Should().HaveCount(2);
        response.Data.PullWorkspace?.Documents.Should().Contain(w => w.Id == workspace1.workspaceInputGql.Id);
        response.Data.PullWorkspace?.Documents.Should().Contain(w => w.Id == workspace2.workspaceInputGql.Id);
    }

    [Fact]
    public async Task SubscriptionShouldHandleMultipleTopics()
    {
        // Arrange
        var workspace1 = await _testContext.HttpClient.CreateWorkspaceAsync(_testContext.CancellationToken);
        var workspace2 = await _testContext.HttpClient.CreateWorkspaceAsync(_testContext.CancellationToken);

        await using var subscriptionClient = await _testContext.Factory.CreateGraphQLSubscriptionClientAsync(_testContext.CancellationToken);

        var topics = new List<string>
    {
        workspace1.workspaceInputGql.Id!.Value.ToString(),
        workspace2.workspaceInputGql.Id!.Value.ToString(),
    };

        var subscriptionQuery = new SubscriptionQueryBuilderGql()
            .WithStreamWorkspace(new WorkspacePullBulkQueryBuilderGql().WithAllFields(),
                new WorkspaceInputHeadersGql { Authorization = "test-auth-token" }, topics)
            .Build();

        var subscriptionTask = CollectSubscriptionDataAsync(subscriptionClient, subscriptionQuery, _testContext.CancellationToken, maxResponses: 2);

        await Task.Delay(1000, _testContext.CancellationToken);

        // Act
        await _testContext.HttpClient.UpdateWorkspaceAsync(workspace1.workspaceInputGql, _testContext.CancellationToken);
        await _testContext.HttpClient.UpdateWorkspaceAsync(workspace2.workspaceInputGql, _testContext.CancellationToken);

        // Assert
        var subscriptionResponses = await subscriptionTask;
        subscriptionResponses.Should().HaveCount(2);

        foreach (var response in subscriptionResponses)
        {
            response.Errors.Should().BeNullOrEmpty();
            response.Data.Should().NotBeNull();
            response.Data?.StreamWorkspace.Should().NotBeNull();
            response.Data?.StreamWorkspace?.Documents.Should().HaveCount(1);

            var streamedWorkspace = response.Data?.StreamWorkspace?.Documents?.First();
            streamedWorkspace.Should()
                .NotBeNull();
            var workspaceIds = new List<Guid?>
            {
                workspace1.workspaceInputGql.Id!.Value,
                workspace2.workspaceInputGql.Id!.Value,
            };

            workspaceIds.Should()
                .Contain(streamedWorkspace?.Id, "the streamed workspace ID should match one of the updated workspaces");
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

        collectTimespan ??= TimeSpan.FromSeconds(5);

        using var timeoutCts = new CancellationTokenSource(collectTimespan.Value);
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
