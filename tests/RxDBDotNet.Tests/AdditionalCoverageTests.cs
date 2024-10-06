// tests\RxDBDotNet.Tests\AdditionalCoverageTests.cs

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using RxDBDotNet.Tests.Model;
using RxDBDotNet.Tests.Setup;
using RxDBDotNet.Tests.Utils;

namespace RxDBDotNet.Tests;

[Collection("DockerSetup")]
public class AdditionalCoverageTests : IAsyncLifetime
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

    [Fact]
    public async Task PushMultipleWorkspacesShouldHandleConflictsCorrectly()
    {
        // Arrange
        TestContext = new TestScenarioBuilder().Build();
        var (workspace1, _) = await TestContext.HttpClient.CreateWorkspaceAsync(TestContext.CancellationToken);
        var (workspace2, _) = await TestContext.HttpClient.CreateWorkspaceAsync(TestContext.CancellationToken);

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

        var pushWorkspaceMutation =
            new MutationQueryBuilderGql().WithPushWorkspace(new PushWorkspacePayloadQueryBuilderGql().WithAllFields(), pushWorkspaceInputGql);

        // Act
        var response = await TestContext.HttpClient.PostGqlMutationAsync(pushWorkspaceMutation, TestContext.CancellationToken);

        // Assert
        response.Errors.Should()
            .BeNullOrEmpty();
        response.Data.PushWorkspace.Should()
            .NotBeNull();
        response.Data.PushWorkspace?.Workspace.Should()
            .HaveCount(1);
        response.Data.PushWorkspace?.Workspace?.First()
            .Id.Should()
            .Be(workspace1.Id);
        response.Data.PushWorkspace?.Errors.Should()
            .BeNullOrEmpty();
    }

    [Fact]
    public async Task PullWorkspacesWithComplexFiltering()
    {
        // Arrange
        TestContext = new TestScenarioBuilder().Build();
        var workspace1 = await TestContext.HttpClient.CreateWorkspaceAsync(TestContext.CancellationToken);
        var workspace2 = await TestContext.HttpClient.CreateWorkspaceAsync(TestContext.CancellationToken);
        await TestContext.HttpClient.CreateWorkspaceAsync(TestContext.CancellationToken);

        var filter = new WorkspaceFilterInputGql
        {
            Or = new List<WorkspaceFilterInputGql>
            {
                new()
                {
                    Id = new UuidOperationFilterInputGql
                    {
                        Eq = workspace1.workspaceInputGql.Id?.Value,
                    },
                },
                new()
                {
                    And = new List<WorkspaceFilterInputGql>
                    {
                        new()
                        {
                            Name = new StringOperationFilterInputGql
                            {
                                Contains = workspace2.workspaceInputGql.Name?.Value,
                            },
                        },
                        new()
                        {
                            IsDeleted = new BooleanOperationFilterInputGql
                            {
                                Eq = false,
                            },
                        },
                    },
                },
            },
        };

        var query = new QueryQueryBuilderGql().WithPullWorkspace(new WorkspacePullBulkQueryBuilderGql().WithAllFields(), 1000, where: filter);

        // Act
        var response = await TestContext.HttpClient.PostGqlQueryAsync(query, TestContext.CancellationToken);

        // Assert
        response.Errors.Should()
            .BeNullOrEmpty();
        response.Data.PullWorkspace.Should()
            .NotBeNull();
        response.Data.PullWorkspace?.Documents.Should()
            .HaveCount(2);
        response.Data.PullWorkspace?.Documents.Should()
            .Contain(w => w.Id == workspace1.workspaceInputGql.Id);
        response.Data.PullWorkspace?.Documents.Should()
            .Contain(w => w.Id == workspace2.workspaceInputGql.Id);
    }

    [Fact]
    public async Task SubscriptionShouldHandleMultipleTopics()
    {
        // Arrange
        TestContext = new TestScenarioBuilder().Build();
        var workspace1 = await TestContext.HttpClient.CreateWorkspaceAsync(TestContext.CancellationToken);
        var workspace2 = await TestContext.HttpClient.CreateWorkspaceAsync(TestContext.CancellationToken);
        workspace1.workspaceInputGql.Topics?.Value.Should()
            .HaveCount(1);
        workspace2.workspaceInputGql.Topics?.Value.Should()
            .HaveCount(1);

        await using var subscriptionClient = await TestContext.Factory.CreateGraphQLSubscriptionClientAsync(TestContext.CancellationToken);

        Debug.Assert(workspace1.workspaceInputGql.Id != null, "workspace1.workspaceInputGql.Id != null");
        Debug.Assert(workspace2.workspaceInputGql.Id != null, "workspace2.workspaceInputGql.Id != null");
        var topics = new List<string>
        {
            workspace1.workspaceInputGql.Id.Value.ToString() ?? throw new InvalidOperationException(),
            workspace2.workspaceInputGql.Id.Value.ToString() ?? throw new InvalidOperationException(),
        };

        var subscriptionQuery = new SubscriptionQueryBuilderGql()
            .WithStreamWorkspace(new WorkspacePullBulkQueryBuilderGql().WithAllFields(), topics: topics)
            .Build();

        var subscriptionTask = CollectSubscriptionDataAsync(subscriptionClient, subscriptionQuery, TestContext.CancellationToken, maxResponses: 2);

        await Task.Delay(1000, TestContext.CancellationToken);

        // Act
        await TestContext.HttpClient.UpdateWorkspaceAsync(workspace1.workspaceInputGql, TestContext.CancellationToken);
        await TestContext.HttpClient.UpdateWorkspaceAsync(workspace2.workspaceInputGql, TestContext.CancellationToken);

        // Assert
        var subscriptionResponses = await subscriptionTask;
        subscriptionResponses.Should()
            .HaveCount(2);

        foreach (var response in subscriptionResponses)
        {
            response.Errors.Should()
                .BeNullOrEmpty();
            response.Data.Should()
                .NotBeNull();
            response.Data?.StreamWorkspace.Should()
                .NotBeNull();
            response.Data?.StreamWorkspace?.Documents.Should()
                .HaveCount(1);

            var streamedWorkspace = response.Data?.StreamWorkspace?.Documents?.First();
            streamedWorkspace.Should()
                .NotBeNull();
            var workspaceIds = new List<Guid?>
            {
                workspace1.workspaceInputGql.Id.Value,
                workspace2.workspaceInputGql.Id.Value,
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
