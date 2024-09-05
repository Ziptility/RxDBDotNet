using RxDBDotNet.Tests.Model;
using RxDBDotNet.Tests.Utils;

namespace RxDBDotNet.Tests;

[Collection("DockerSetup")]
public class PullDocumentsTests : IAsyncLifetime
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

    private async Task<(WorkspaceInputGql workspace, UserInputGql user, LiveDocInputGql liveDoc)> CreateLiveDocAsync()
    {
        var (workspaceInputGql, _) = await _testContext.HttpClient.CreateWorkspaceAsync(_testContext.CancellationToken);
        var user = await _testContext.HttpClient.CreateUserAsync(workspaceInputGql, _testContext.CancellationToken);
        var liveDoc = await _testContext.HttpClient.CreateLiveDocAsync(workspaceInputGql, user, _testContext.CancellationToken);
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
        var (workspace, user, liveDoc1) = await CreateLiveDocAsync();
        var liveDoc2 = await _testContext.HttpClient.CreateLiveDocAsync(workspace, user, _testContext.CancellationToken);

        var query = new QueryQueryBuilderGql().WithPullLiveDoc(new LiveDocPullBulkQueryBuilderGql().WithAllFields()
            .WithDocuments(new LiveDocQueryBuilderGql().WithAllFields())
            .WithCheckpoint(new CheckpointQueryBuilderGql().WithAllFields()), 1000, where: CreateWorkspaceFilter(workspace.Id!.Value));

        // Act
        var response = await _testContext.HttpClient.PostGqlQueryAsync(query, _testContext.CancellationToken);

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
        var (workspace, user, liveDoc1) = await CreateLiveDocAsync();

        // Get initial checkpoint
        var initialQuery = new QueryQueryBuilderGql().WithPullLiveDoc(new LiveDocPullBulkQueryBuilderGql().WithAllFields()
            .WithDocuments(new LiveDocQueryBuilderGql().WithAllFields())
            .WithCheckpoint(new CheckpointQueryBuilderGql().WithAllFields()), 1000, where: CreateWorkspaceFilter(workspace.Id!.Value));

        var initialResponse = await _testContext.HttpClient.PostGqlQueryAsync(initialQuery, _testContext.CancellationToken);
        var checkpoint = initialResponse.Data.PullLiveDoc?.Checkpoint;

        // Create a new LiveDoc after the initial checkpoint
        var liveDoc2 = await _testContext.HttpClient.CreateLiveDocAsync(workspace, user, _testContext.CancellationToken);

        var query = new QueryQueryBuilderGql().WithPullLiveDoc(new LiveDocPullBulkQueryBuilderGql().WithAllFields()
            .WithDocuments(new LiveDocQueryBuilderGql().WithAllFields())
            .WithCheckpoint(new CheckpointQueryBuilderGql().WithAllFields()), 1000, new LiveDocInputCheckpointGql
        {
            LastDocumentId = checkpoint?.LastDocumentId,
            UpdatedAt = checkpoint?.UpdatedAt,
        }, CreateWorkspaceFilter(workspace.Id!.Value));

        // Act
        var response = await _testContext.HttpClient.PostGqlQueryAsync(query, _testContext.CancellationToken);

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
        var (workspace, user, _) = await CreateLiveDocAsync();
        await _testContext.HttpClient.CreateLiveDocAsync(workspace, user, _testContext.CancellationToken);
        await _testContext.HttpClient.CreateLiveDocAsync(workspace, user, _testContext.CancellationToken);

        var query = new QueryQueryBuilderGql().WithPullLiveDoc(new LiveDocPullBulkQueryBuilderGql().WithAllFields()
            .WithDocuments(new LiveDocQueryBuilderGql().WithAllFields())
            .WithCheckpoint(new CheckpointQueryBuilderGql().WithAllFields()), 2, where: CreateWorkspaceFilter(workspace.Id!.Value));

        // Act
        var response = await _testContext.HttpClient.PostGqlQueryAsync(query, _testContext.CancellationToken);

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
        var (workspace, user, liveDoc1) = await CreateLiveDocAsync();
        var liveDoc2 = await _testContext.HttpClient.CreateLiveDocAsync(workspace, user, _testContext.CancellationToken);

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
        var response = await _testContext.HttpClient.PostGqlQueryAsync(query, _testContext.CancellationToken);

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
        var (workspace, _, _) = await CreateLiveDocAsync();

        var initialQuery = new QueryQueryBuilderGql().WithPullLiveDoc(new LiveDocPullBulkQueryBuilderGql().WithAllFields()
            .WithDocuments(new LiveDocQueryBuilderGql().WithAllFields())
            .WithCheckpoint(new CheckpointQueryBuilderGql().WithAllFields()), 1000, where: CreateWorkspaceFilter(workspace.Id!.Value));

        var initialResponse = await _testContext.HttpClient.PostGqlQueryAsync(initialQuery, _testContext.CancellationToken);
        var checkpoint = initialResponse.Data.PullLiveDoc?.Checkpoint;

        var query = new QueryQueryBuilderGql().WithPullLiveDoc(new LiveDocPullBulkQueryBuilderGql().WithAllFields()
            .WithDocuments(new LiveDocQueryBuilderGql().WithAllFields())
            .WithCheckpoint(new CheckpointQueryBuilderGql().WithAllFields()), 1000, new LiveDocInputCheckpointGql
        {
            LastDocumentId = checkpoint?.LastDocumentId,
            UpdatedAt = checkpoint?.UpdatedAt,
        }, CreateWorkspaceFilter(workspace.Id!.Value));

        // Act
        var response = await _testContext.HttpClient.PostGqlQueryAsync(query, _testContext.CancellationToken);

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
        var (workspace, _, liveDoc) = await CreateLiveDocAsync();
        var updatedLiveDoc = await _testContext.HttpClient.UpdateLiveDocAsync(liveDoc, _testContext.CancellationToken);

        var query = new QueryQueryBuilderGql().WithPullLiveDoc(new LiveDocPullBulkQueryBuilderGql().WithAllFields()
            .WithDocuments(new LiveDocQueryBuilderGql().WithAllFields())
            .WithCheckpoint(new CheckpointQueryBuilderGql().WithAllFields()), 1000, where: CreateWorkspaceFilter(workspace.Id!.Value));

        // Act
        var response = await _testContext.HttpClient.PostGqlQueryAsync(query, _testContext.CancellationToken);

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
        var (workspace, user1, liveDoc1) = await CreateLiveDocAsync();
        var user2 = await _testContext.HttpClient.CreateUserAsync(workspace, _testContext.CancellationToken);
        var liveDoc2 = await _testContext.HttpClient.CreateLiveDocAsync(workspace, user2, _testContext.CancellationToken);

        var query = new QueryQueryBuilderGql().WithPullLiveDoc(new LiveDocPullBulkQueryBuilderGql().WithAllFields()
            .WithDocuments(new LiveDocQueryBuilderGql().WithAllFields())
            .WithCheckpoint(new CheckpointQueryBuilderGql().WithAllFields()), 1000, where: CreateWorkspaceFilter(workspace.Id!.Value));

        // Act
        var response = await _testContext.HttpClient.PostGqlQueryAsync(query, _testContext.CancellationToken);

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
