using RxDBDotNet.Tests.Model;
using RxDBDotNet.Tests.Utils;

namespace RxDBDotNet.Tests;

[Collection("DockerSetup")]
public class PullDocumentsTests : IAsyncLifetime
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

    private async Task<(WorkspaceInputGql workspace, UserInputGql user, LiveDocInputGql liveDoc)> CreateLiveDocAsync()
    {
        var (workspaceInputGql, _) = await TestContext.HttpClient.CreateWorkspaceAsync(TestContext.CancellationToken);
        var user = await TestContext.HttpClient.CreateUserAsync(workspaceInputGql, TestContext.CancellationToken);
        var liveDoc = await TestContext.HttpClient.CreateLiveDocAsync(workspaceInputGql, user, TestContext.CancellationToken);
        return (workspaceInputGql, user, liveDoc);
    }

    private static LiveDocFilterInputGql CreateWorkspaceFilter(Guid workspaceId)
    {
        return new LiveDocFilterInputGql
        {
            WorkspaceId = new UuidOperationFilterInputGql
            {
                Eq = workspaceId,
            },
        };
    }

    [Fact]
    public async Task PullDocuments_WithNoCheckpoint_ShouldReturnAllDocuments()
    {
        // Arrange
        TestContext = new TestScenarioBuilder().Build();
        var (workspace, user, liveDoc1) = await CreateLiveDocAsync();
        var liveDoc2 = await TestContext.HttpClient.CreateLiveDocAsync(workspace, user, TestContext.CancellationToken);

        var query = new QueryQueryBuilderGql().WithPullLiveDoc(new LiveDocPullBulkQueryBuilderGql().WithAllFields()
            .WithDocuments(new LiveDocQueryBuilderGql().WithAllFields())
            .WithCheckpoint(new CheckpointQueryBuilderGql().WithAllFields()), 1000, where: CreateWorkspaceFilter(workspace.Id!.Value));

        // Act
        var response = await TestContext.HttpClient.PostGqlQueryAsync(query, TestContext.CancellationToken);

        // Assert
        response.Errors.Should()
            .BeNullOrEmpty();
        response.Data.PullLiveDoc.Should()
            .NotBeNull();
        response.Data.PullLiveDoc?.Documents.Should()
            .HaveCount(2);
        response.Data.PullLiveDoc?.Documents.Should()
            .Contain(d => d.Id == liveDoc1.Id);
        response.Data.PullLiveDoc?.Documents.Should()
            .Contain(d => d.Id == liveDoc2.Id);
        response.Data.PullLiveDoc?.Documents.Should()
            .AllSatisfy(d => d.OwnerId.Should()
                .Be(user.Id));
        response.Data.PullLiveDoc?.Checkpoint.Should()
            .NotBeNull();
        response.Data.PullLiveDoc?.Checkpoint?.LastDocumentId.Should()
            .NotBeNull();
        response.Data.PullLiveDoc?.Checkpoint?.UpdatedAt.Should()
            .NotBeNull();
    }

    [Fact]
    public async Task PullDocuments_WithCheckpoint_ShouldReturnOnlyNewDocuments()
    {
        // Arrange
        TestContext = new TestScenarioBuilder().Build();
        var (workspace, user, liveDoc1) = await CreateLiveDocAsync();

        // Get initial checkpoint
        var initialQuery = new QueryQueryBuilderGql().WithPullLiveDoc(new LiveDocPullBulkQueryBuilderGql().WithAllFields()
            .WithDocuments(new LiveDocQueryBuilderGql().WithAllFields())
            .WithCheckpoint(new CheckpointQueryBuilderGql().WithAllFields()), 1000, where: CreateWorkspaceFilter(workspace.Id!.Value));

        var initialResponse = await TestContext.HttpClient.PostGqlQueryAsync(initialQuery, TestContext.CancellationToken);
        var checkpoint = initialResponse.Data.PullLiveDoc?.Checkpoint;

        // Create a new LiveDoc after the initial checkpoint
        var liveDoc2 = await TestContext.HttpClient.CreateLiveDocAsync(workspace, user, TestContext.CancellationToken);

        var query = new QueryQueryBuilderGql().WithPullLiveDoc(new LiveDocPullBulkQueryBuilderGql().WithAllFields()
            .WithDocuments(new LiveDocQueryBuilderGql().WithAllFields())
            .WithCheckpoint(new CheckpointQueryBuilderGql().WithAllFields()), 1000, new LiveDocInputCheckpointGql
        {
            LastDocumentId = checkpoint?.LastDocumentId,
            UpdatedAt = checkpoint?.UpdatedAt,
        }, CreateWorkspaceFilter(workspace.Id!.Value));

        // Act
        var response = await TestContext.HttpClient.PostGqlQueryAsync(query, TestContext.CancellationToken);

        // Assert
        response.Errors.Should()
            .BeNullOrEmpty();
        response.Data.PullLiveDoc.Should()
            .NotBeNull();
        response.Data.PullLiveDoc?.Documents.Should()
            .ContainSingle(d => d.Id == liveDoc2.Id);
        response.Data.PullLiveDoc?.Documents.Should()
            .NotContain(d => d.Id == liveDoc1.Id);
    }

    [Fact]
    public async Task PullDocuments_WithLimit_ShouldReturnLimitedDocuments()
    {
        // Arrange
        TestContext = new TestScenarioBuilder().Build();
        var (workspace, user, _) = await CreateLiveDocAsync();
        await TestContext.HttpClient.CreateLiveDocAsync(workspace, user, TestContext.CancellationToken);
        await TestContext.HttpClient.CreateLiveDocAsync(workspace, user, TestContext.CancellationToken);

        var query = new QueryQueryBuilderGql().WithPullLiveDoc(new LiveDocPullBulkQueryBuilderGql().WithAllFields()
            .WithDocuments(new LiveDocQueryBuilderGql().WithAllFields())
            .WithCheckpoint(new CheckpointQueryBuilderGql().WithAllFields()), 2, where: CreateWorkspaceFilter(workspace.Id!.Value));

        // Act
        var response = await TestContext.HttpClient.PostGqlQueryAsync(query, TestContext.CancellationToken);

        // Assert
        response.Errors.Should()
            .BeNullOrEmpty();
        response.Data.PullLiveDoc.Should()
            .NotBeNull();
        response.Data.PullLiveDoc?.Documents.Should()
            .HaveCount(2);
    }

    [Fact]
    public async Task PullDocuments_WithFilter_ShouldReturnFilteredDocuments()
    {
        // Arrange
        TestContext = new TestScenarioBuilder().Build();
        var (workspace, user, liveDoc1) = await CreateLiveDocAsync();
        var liveDoc2 = await TestContext.HttpClient.CreateLiveDocAsync(workspace, user, TestContext.CancellationToken);

        var filter = new LiveDocFilterInputGql
        {
            WorkspaceId = new UuidOperationFilterInputGql
            {
                Eq = workspace.Id?.Value,
            },
            Id = new UuidOperationFilterInputGql
            {
                Eq = liveDoc1.Id?.Value,
            },
        };

        var query = new QueryQueryBuilderGql().WithPullLiveDoc(new LiveDocPullBulkQueryBuilderGql().WithAllFields()
            .WithDocuments(new LiveDocQueryBuilderGql().WithAllFields())
            .WithCheckpoint(new CheckpointQueryBuilderGql().WithAllFields()), 1000, where: filter);

        // Act
        var response = await TestContext.HttpClient.PostGqlQueryAsync(query, TestContext.CancellationToken);

        // Assert
        response.Errors.Should()
            .BeNullOrEmpty();
        response.Data.PullLiveDoc.Should()
            .NotBeNull();
        response.Data.PullLiveDoc?.Documents.Should()
            .ContainSingle(d => d.Id == liveDoc1.Id);
        response.Data.PullLiveDoc?.Documents.Should()
            .NotContain(d => d.Id == liveDoc2.Id);
    }

    [Fact]
    public async Task PullDocuments_WithNoNewDocuments_ShouldReturnEmptyResultWithSameCheckpoint()
    {
        // Arrange
        TestContext = new TestScenarioBuilder().Build();
        var (workspace, _, _) = await CreateLiveDocAsync();

        var initialQuery = new QueryQueryBuilderGql().WithPullLiveDoc(new LiveDocPullBulkQueryBuilderGql().WithAllFields()
            .WithDocuments(new LiveDocQueryBuilderGql().WithAllFields())
            .WithCheckpoint(new CheckpointQueryBuilderGql().WithAllFields()), 1000, where: CreateWorkspaceFilter(workspace.Id!.Value));

        var initialResponse = await TestContext.HttpClient.PostGqlQueryAsync(initialQuery, TestContext.CancellationToken);
        var checkpoint = initialResponse.Data.PullLiveDoc?.Checkpoint;

        var query = new QueryQueryBuilderGql().WithPullLiveDoc(new LiveDocPullBulkQueryBuilderGql().WithAllFields()
            .WithDocuments(new LiveDocQueryBuilderGql().WithAllFields())
            .WithCheckpoint(new CheckpointQueryBuilderGql().WithAllFields()), 1000, new LiveDocInputCheckpointGql
        {
            LastDocumentId = checkpoint?.LastDocumentId,
            UpdatedAt = checkpoint?.UpdatedAt,
        }, CreateWorkspaceFilter(workspace.Id!.Value));

        // Act
        var response = await TestContext.HttpClient.PostGqlQueryAsync(query, TestContext.CancellationToken);

        // Assert
        response.Errors.Should()
            .BeNullOrEmpty();
        response.Data.PullLiveDoc.Should()
            .NotBeNull();
        response.Data.PullLiveDoc?.Documents.Should()
            .BeEmpty();
        response.Data.PullLiveDoc?.Checkpoint.Should()
            .BeEquivalentTo(checkpoint);
    }

    [Fact]
    public async Task PullDocuments_WithUpdatedDocuments_ShouldReturnUpdatedVersions()
    {
        // Arrange
        TestContext = new TestScenarioBuilder().Build();
        var (workspace, _, liveDoc) = await CreateLiveDocAsync();
        var updatedLiveDoc = await TestContext.HttpClient.UpdateLiveDocAsync(liveDoc, TestContext.CancellationToken);

        var query = new QueryQueryBuilderGql().WithPullLiveDoc(new LiveDocPullBulkQueryBuilderGql().WithAllFields()
            .WithDocuments(new LiveDocQueryBuilderGql().WithAllFields())
            .WithCheckpoint(new CheckpointQueryBuilderGql().WithAllFields()), 1000, where: CreateWorkspaceFilter(workspace.Id!.Value));

        // Act
        var response = await TestContext.HttpClient.PostGqlQueryAsync(query, TestContext.CancellationToken);

        // Assert
        response.Errors.Should()
            .BeNullOrEmpty();
        response.Data.PullLiveDoc.Should()
            .NotBeNull();
        var pulledLiveDoc = response.Data.PullLiveDoc?.Documents.Should()
            .ContainSingle(d => d.Id == updatedLiveDoc.Id)
            .Subject;
        pulledLiveDoc?.Content.Should()
            .Be(updatedLiveDoc.Content);
        pulledLiveDoc?.UpdatedAt.Should()
            .BeCloseTo(updatedLiveDoc.UpdatedAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task PullDocuments_WithMultipleOwners_ShouldReturnCorrectOwners()
    {
        // Arrange
        TestContext = new TestScenarioBuilder().Build();
        var (workspace, user1, liveDoc1) = await CreateLiveDocAsync();
        var user2 = await TestContext.HttpClient.CreateUserAsync(workspace, TestContext.CancellationToken);
        var liveDoc2 = await TestContext.HttpClient.CreateLiveDocAsync(workspace, user2, TestContext.CancellationToken);

        var query = new QueryQueryBuilderGql().WithPullLiveDoc(new LiveDocPullBulkQueryBuilderGql().WithAllFields()
            .WithDocuments(new LiveDocQueryBuilderGql().WithAllFields())
            .WithCheckpoint(new CheckpointQueryBuilderGql().WithAllFields()), 1000, where: CreateWorkspaceFilter(workspace.Id!.Value));

        // Act
        var response = await TestContext.HttpClient.PostGqlQueryAsync(query, TestContext.CancellationToken);

        // Assert
        response.Errors.Should()
            .BeNullOrEmpty();
        response.Data.PullLiveDoc.Should()
            .NotBeNull();
        response.Data.PullLiveDoc?.Documents.Should()
            .HaveCount(2);
        response.Data.PullLiveDoc?.Documents.Should()
            .Contain(d => d.Id == liveDoc1.Id && d.OwnerId == user1.Id);
        response.Data.PullLiveDoc?.Documents.Should()
            .Contain(d => d.Id == liveDoc2.Id && d.OwnerId == user2.Id);
    }
}
