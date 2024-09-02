using RxDBDotNet.Tests.Model;
using RxDBDotNet.Tests.Utils;

namespace RxDBDotNet.Tests;

[Collection("DockerSetup")]
public class BasicDocumentOperationsTests : IAsyncLifetime
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
    public async Task TestCase1_1_PushNewRowShouldCreateSingleDocument()
    {
        // Arrange
        var workspaceId = Provider.Sql.Create();
        var workspaceInput = new WorkspaceInputGql
        {
            Id = workspaceId,
            Name = Strings.CreateString(),
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
            NewDocumentState = workspaceInput,
        };

        var workspaceInputGql = new PushWorkspaceInputGql
        {
            WorkspacePushRow = new List<WorkspaceInputPushRowGql?>
            {
                workspaceInputPushRowGql,
            },
        };

        var createWorkspace =
            new MutationQueryBuilderGql().WithPushWorkspace(new PushWorkspacePayloadQueryBuilderGql().WithAllFields(), workspaceInputGql);

        // Act
        var response = await _testContext.HttpClient.PostGqlMutationAsync(createWorkspace, _testContext.CancellationToken);

        // Assert
        response.Errors.Should()
            .BeNullOrEmpty();
        response.Data.PushWorkspace?.Errors.Should()
            .BeNullOrEmpty();
        response.Data.PushWorkspace?.Workspace.Should()
            .BeNullOrEmpty();

        await _testContext.HttpClient.VerifyWorkspaceAsync(workspaceInput, _testContext.CancellationToken);
    }

    [Fact]
    public async Task TestCase1_2_PullBulkByDocumentIdShouldReturnSingleDocument()
    {
        // Arrange
        var workspace1 = await _testContext.HttpClient.CreateWorkspaceAsync(_testContext.CancellationToken);
        await _testContext.HttpClient.CreateWorkspaceAsync(_testContext.CancellationToken);

        var workspaceById = new WorkspaceFilterInputGql
        {
            Id = new UuidOperationFilterInputGql
            {
                Eq = workspace1.workspaceInputGql.Id?.Value,
            },
        };
        var query = new QueryQueryBuilderGql().WithPullWorkspace(new WorkspacePullBulkQueryBuilderGql().WithAllFields()
            .WithDocuments(new WorkspaceQueryBuilderGql().WithAllFields()), 1000, where: workspaceById);

        // Act
        var response = await _testContext.HttpClient.PostGqlQueryAsync(query, _testContext.CancellationToken);

        // Assert
        response.Errors.Should()
            .BeNullOrEmpty();

        response.Data.PullWorkspace?.Documents.Should()
            .HaveCount(1);
        response.Data.PullWorkspace?.Documents.Should()
            .ContainSingle(workspace => workspace.Id == workspace1.workspaceInputGql.Id);
    }

    [Fact]
    public async Task PullBulkShouldReturnAllDocuments()
    {
        // Arrange
        var workspace1 = await _testContext.HttpClient.CreateWorkspaceAsync(_testContext.CancellationToken);
        var workspace2 = await _testContext.HttpClient.CreateWorkspaceAsync(_testContext.CancellationToken);

        var query = new QueryQueryBuilderGql().WithPullWorkspace(new WorkspacePullBulkQueryBuilderGql().WithAllFields()
            .WithDocuments(new WorkspaceQueryBuilderGql().WithAllFields()), 1000);

        // Act
        var response = await _testContext.HttpClient.PostGqlQueryAsync(query, _testContext.CancellationToken);

        // Assert
        response.Errors.Should()
            .BeNullOrEmpty();

        response.Data.PullWorkspace?.Documents.Should()
            .ContainSingle(workspace => workspace.Id == workspace1.workspaceInputGql.Id);
        response.Data.PullWorkspace?.Documents.Should()
            .ContainSingle(workspace => workspace.Id == workspace2.workspaceInputGql.Id);
    }

    [Fact]
    public async Task ItShouldHandleMultiplePullsFollowedByAPush()
    {
        // Arrange

        // Act
        var response = await PushAndPullDocumentAsync(_testContext);
        var response2 = await PushAndPullDocumentAsync(_testContext);
        var response3 = await PushAndPullDocumentAsync(_testContext);

        // Assert
        response.Errors.Should()
            .BeNullOrEmpty();
        response2.Errors.Should()
            .BeNullOrEmpty();
        response3.Errors.Should()
            .BeNullOrEmpty();
    }

    private static async Task<GqlQueryResponse> PushAndPullDocumentAsync(TestContext testContext)
    {
        // Push a document
        var (workspaceInputGql, _) = await testContext.HttpClient.CreateWorkspaceAsync(testContext.CancellationToken);

        // Pull a document
        var workspaceById = new WorkspaceFilterInputGql
        {
            Id = new UuidOperationFilterInputGql
            {
                Eq = workspaceInputGql.Id?.Value,
            },
        };
        var query = new QueryQueryBuilderGql().WithPullWorkspace(new WorkspacePullBulkQueryBuilderGql().WithAllFields()
            .WithDocuments(new WorkspaceQueryBuilderGql().WithAllFields()), 1000, where: workspaceById);

        return await testContext.HttpClient.PostGqlQueryAsync(query, testContext.CancellationToken);
    }
}
