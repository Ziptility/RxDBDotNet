using FluentAssertions;
using RT.Comb;
using RxDBDotNet.Tests.Helpers;
using RxDBDotNet.Tests.Model;
using Xunit.Abstractions;

namespace RxDBDotNet.Tests;

public class BasicDocumentOperationsTests(ITestOutputHelper output) : TestBase(output)
{
    [Fact]
    public async Task TestCase1_1_PushNewRowShouldCreateSingleDocument()
    {
        // Arrange
        var newWorkspace = new WorkspaceInputGql
        {
            Id = Provider.Sql.Create(),
            Name = Strings.CreateString(),
            UpdatedAt = DateTimeOffset.UtcNow,
            IsDeleted = false,
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
        var response = await HttpClient.PostGqlMutationAsync(createWorkspace);

        // Assert
        response.Errors.Should()
            .BeNullOrEmpty();
        response.Data.PushWorkspace?.Should()
            .BeNullOrEmpty();

        // Verify the workspace exists in the database
        await HttpClient.VerifyWorkspaceExists(newWorkspace);
    }

    [Fact]
    public async Task TestCase1_2_PullBulkByDocumentIdShouldReturnSingleDocument()
    {
        // Arrange
        var workspace1 = await HttpClient.CreateNewWorkspaceAsync();
        await HttpClient.CreateNewWorkspaceAsync();

        var workspaceById = new WorkspaceFilterInputGql
        {
            Id = new UuidOperationFilterInputGql
            {
                Eq = workspace1.Id?.Value,
            },
        };
        var query = new QueryQueryBuilderGql().WithPullWorkspace(new WorkspacePullBulkQueryBuilderGql().WithAllFields()
            .WithDocuments(new WorkspaceQueryBuilderGql().WithAllFields()), 10, where: workspaceById);

        // Act
        var response = await HttpClient.PostGqlQueryAsync(query);

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
        var workspace1 = await HttpClient.CreateNewWorkspaceAsync();
        var workspace2 = await HttpClient.CreateNewWorkspaceAsync();

        var query = new QueryQueryBuilderGql().WithPullWorkspace(new WorkspacePullBulkQueryBuilderGql().WithAllFields()
            .WithDocuments(new WorkspaceQueryBuilderGql().WithAllFields()), 10);

        // Act
        var response = await HttpClient.PostGqlQueryAsync(query);

        // Assert
        response.Errors.Should()
            .BeNullOrEmpty();

        response.Data.PullWorkspace?.Documents.Should()
            .HaveCount(3, "Two were created in the test, and one is created via seed data");
        response.Data.PullWorkspace?.Documents.Should()
            .ContainSingle(workspace => workspace.Id == workspace1.Id);
        response.Data.PullWorkspace?.Documents.Should()
            .ContainSingle(workspace => workspace.Id == workspace2.Id);
    }

    [Fact]
    public async Task ItShouldHandleMultiplePullsFollowedByAPush()
    {
        // Act
        var response = await PushAndPullDocumentAsync();
        var response2 = await PushAndPullDocumentAsync();
        var response3 = await PushAndPullDocumentAsync();

        // Assert
        response.Errors.Should()
            .BeNullOrEmpty();
        response2.Errors.Should()
            .BeNullOrEmpty();
        response3.Errors.Should()
            .BeNullOrEmpty();
    }

    private async Task<GqlQueryResponse> PushAndPullDocumentAsync()
    {
        // Push a document
        var newWorkspace = await HttpClient.CreateNewWorkspaceAsync();

        // Pull a document
        var workspaceById = new WorkspaceFilterInputGql
        {
            Id = new UuidOperationFilterInputGql
            {
                Eq = newWorkspace.Id?.Value,
            },
        };
        var query = new QueryQueryBuilderGql().WithPullWorkspace(new WorkspacePullBulkQueryBuilderGql().WithAllFields()
            .WithDocuments(new WorkspaceQueryBuilderGql().WithAllFields()), 10, where: workspaceById);

        return await HttpClient.PostGqlQueryAsync(query);
    }
}
