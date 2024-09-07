using RxDBDotNet.Tests.Model;
using RxDBDotNet.Tests.Utils;

namespace RxDBDotNet.Tests;

[Collection("DockerSetup")]
public class BasicDocumentOperationsTests : IAsyncLifetime
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
    public async Task PushNewRowShouldCreateSingleDocument()
    {
        // Arrange
        TestContext = TestSetupUtil.Setup();
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
        var response = await TestContext.HttpClient.PostGqlMutationAsync(createWorkspace, TestContext.CancellationToken);

        // Assert
        response.Errors.Should()
            .BeNullOrEmpty();
        response.Data.PushWorkspace?.Errors.Should()
            .BeNullOrEmpty();
        response.Data.PushWorkspace?.Workspace.Should()
            .BeNullOrEmpty();

        await TestContext.HttpClient.VerifyWorkspaceAsync(workspaceInput, TestContext.CancellationToken);
    }

    [Fact]
    public async Task PullBulkByDocumentIdShouldReturnSingleDocument()
    {
        // Arrange
        TestContext = TestSetupUtil.Setup();
        var workspace1 = await TestContext.HttpClient.CreateWorkspaceAsync(TestContext.CancellationToken);
        await TestContext.HttpClient.CreateWorkspaceAsync(TestContext.CancellationToken);

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
        var response = await TestContext.HttpClient.PostGqlQueryAsync(query, TestContext.CancellationToken);

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
        TestContext = TestSetupUtil.Setup();
        var workspace1 = await TestContext.HttpClient.CreateWorkspaceAsync(TestContext.CancellationToken);
        var workspace2 = await TestContext.HttpClient.CreateWorkspaceAsync(TestContext.CancellationToken);

        var query = new QueryQueryBuilderGql().WithPullWorkspace(new WorkspacePullBulkQueryBuilderGql().WithAllFields()
            .WithDocuments(new WorkspaceQueryBuilderGql().WithAllFields()), 1000);

        // Act
        var response = await TestContext.HttpClient.PostGqlQueryAsync(query, TestContext.CancellationToken);

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
        TestContext = TestSetupUtil.Setup();

        // Act
        var response = await PushAndPullDocumentAsync(TestContext);
        var response2 = await PushAndPullDocumentAsync(TestContext);
        var response3 = await PushAndPullDocumentAsync(TestContext);

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
