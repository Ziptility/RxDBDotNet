using FluentAssertions;
using RT.Comb;
using RxDBDotNet.Tests.Helpers;
using RxDBDotNet.Tests.Model;
using RxDBDotNet.Tests.Setup;
using Xunit.Abstractions;

namespace RxDBDotNet.Tests;

public class BasicDocumentOperationsTests(ITestOutputHelper output) : TestSetupUtil(output)
{
    [Fact]
    public async Task TestCase1_1_PushNewRowShouldCreateSingleDocument()
    {
        // Arrange
        using var testTimeoutTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var testTimeoutToken = testTimeoutTokenSource.Token;

        var workspaceId = Provider.Sql.Create();
        var newWorkspace = new WorkspaceInputGql
        {
            Id = workspaceId,
            Name = Strings.CreateString(),
            UpdatedAt = DateTimeOffset.UtcNow,
            IsDeleted = false,
            Topics = new List<string> { workspaceId.ToString() },
        };

        var workspaceInput = new WorkspaceInputPushRowGql
        {
            AssumedMasterState = null,
            NewDocumentState = newWorkspace,
        };

        var pushWorkspaceInputGql = new List<WorkspaceInputPushRowGql?>
        {
            workspaceInput,
        };

        var createWorkspace =
            new MutationQueryBuilderGql().WithPushWorkspace(new WorkspaceQueryBuilderGql().WithAllFields(), pushWorkspaceInputGql);

        // Act
        var response = await HttpClient.PostGqlMutationAsync(createWorkspace, testTimeoutToken);

        // Assert
        response.Errors.Should()
            .BeNullOrEmpty();
        response.Data.PushWorkspace?.Should()
            .BeNullOrEmpty();

        // Verify the workspace exists in the database
        await HttpClient.VerifyWorkspaceExists(newWorkspace, testTimeoutToken);
    }

    [Fact]
    public async Task TestCase1_2_PullBulkByDocumentIdShouldReturnSingleDocument()
    {
        // Arrange
        using var testTimeoutTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var testTimeoutToken = testTimeoutTokenSource.Token;

        var workspace1 = await HttpClient.CreateNewWorkspaceAsync(testTimeoutToken);
        await HttpClient.CreateNewWorkspaceAsync(testTimeoutToken);

        var workspaceById = new WorkspaceFilterInputGql
        {
            Id = new UuidOperationFilterInputGql
            {
                Eq = workspace1.Id?.Value,
            },
        };
        var query = new QueryQueryBuilderGql().WithPullWorkspace(new WorkspacePullBulkQueryBuilderGql().WithAllFields()
            .WithDocuments(new WorkspaceQueryBuilderGql().WithAllFields()), limit: 1000, where: workspaceById);

        // Act
        var response = await HttpClient.PostGqlQueryAsync(query, testTimeoutToken);

        // Assert
        response.Errors.Should()
            .BeNullOrEmpty();

        response.Data.PullWorkspace?.Documents.Should()
            .HaveCount(1);
        response.Data.PullWorkspace?.Documents.Should()
            .ContainSingle(workspace => workspace.Id == workspace1.Id);
    }

    [Fact]
    public async Task PullBulkShouldReturnAllDocuments()
    {
        // Arrange
        using var testTimeoutTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var testTimeoutToken = testTimeoutTokenSource.Token;
        var workspace1 = await HttpClient.CreateNewWorkspaceAsync(testTimeoutToken);
        var workspace2 = await HttpClient.CreateNewWorkspaceAsync(testTimeoutToken);

        var query = new QueryQueryBuilderGql().WithPullWorkspace(new WorkspacePullBulkQueryBuilderGql().WithAllFields()
            .WithDocuments(new WorkspaceQueryBuilderGql().WithAllFields()), limit: 1000);

        // Act
        var response = await HttpClient.PostGqlQueryAsync(query, testTimeoutToken);

        // Assert
        response.Errors.Should()
            .BeNullOrEmpty();

        response.Data.PullWorkspace?.Documents.Should()
            .ContainSingle(workspace => workspace.Id == workspace1.Id);
        response.Data.PullWorkspace?.Documents.Should()
            .ContainSingle(workspace => workspace.Id == workspace2.Id);
    }

    [Fact]
    public async Task ItShouldHandleMultiplePullsFollowedByAPush()
    {
        // Act
        using var testTimeoutTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var testTimeoutToken = testTimeoutTokenSource.Token;
        var response = await PushAndPullDocumentAsync(testTimeoutToken);
        var response2 = await PushAndPullDocumentAsync(testTimeoutToken);
        var response3 = await PushAndPullDocumentAsync(testTimeoutToken);

        // Assert
        response.Errors.Should()
            .BeNullOrEmpty();
        response2.Errors.Should()
            .BeNullOrEmpty();
        response3.Errors.Should()
            .BeNullOrEmpty();
    }

    private async Task<GqlQueryResponse> PushAndPullDocumentAsync(CancellationToken cancellationToken)
    {
        // Push a document
        var newWorkspace = await HttpClient.CreateNewWorkspaceAsync(cancellationToken);

        // Pull a document
        var workspaceById = new WorkspaceFilterInputGql
        {
            Id = new UuidOperationFilterInputGql
            {
                Eq = newWorkspace.Id?.Value,
            },
        };
        var query = new QueryQueryBuilderGql().WithPullWorkspace(new WorkspacePullBulkQueryBuilderGql().WithAllFields()
            .WithDocuments(new WorkspaceQueryBuilderGql().WithAllFields()), limit: 1000, where: workspaceById);

        return await HttpClient.PostGqlQueryAsync(query, cancellationToken);
    }
}
