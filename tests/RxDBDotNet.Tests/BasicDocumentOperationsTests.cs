using RxDBDotNet.Tests.Helpers;
using RxDBDotNet.Tests.Model;

namespace RxDBDotNet.Tests;

[Collection("DockerSetup")]
public class BasicDocumentOperationsTests
{
    [Fact]
    public async Task TestCase1_1_PushNewRowShouldCreateSingleDocument()
    {
        TestContext? testContext = null;

        try
        {
            // Arrange
            testContext = await TestSetupUtil.SetupAsync();

            var workspaceId = Provider.Sql.Create();
            var newWorkspace = new WorkspaceInputGql
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
            var response = await testContext.HttpClient.PostGqlMutationAsync(createWorkspace, testContext.CancellationToken);

            // Assert
            response.Errors.Should()
                .BeNullOrEmpty();
            response.Data.PushWorkspace?.Should()
                .BeNullOrEmpty();

            // Verify the workspace exists in the database
            await testContext.HttpClient.VerifyWorkspaceExists(newWorkspace, testContext.CancellationToken);
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
    public async Task TestCase1_2_PullBulkByDocumentIdShouldReturnSingleDocument()
    {
        TestContext? testContext = null;

        try
        {
            // Arrange
            testContext = await TestSetupUtil.SetupAsync();

            var workspace1 = await testContext.HttpClient.CreateNewWorkspaceAsync(testContext.CancellationToken);
            await testContext.HttpClient.CreateNewWorkspaceAsync(testContext.CancellationToken);

            var workspaceById = new WorkspaceFilterInputGql
            {
                Id = new UuidOperationFilterInputGql
                {
                    Eq = workspace1.Id?.Value,
                },
            };
            var query = new QueryQueryBuilderGql().WithPullWorkspace(new WorkspacePullBulkQueryBuilderGql().WithAllFields()
                .WithDocuments(new WorkspaceQueryBuilderGql().WithAllFields()), 1000, where: workspaceById);

            // Act
            var response = await testContext.HttpClient.PostGqlQueryAsync(query, testContext.CancellationToken);

            // Assert
            response.Errors.Should()
                .BeNullOrEmpty();

            response.Data.PullWorkspace?.Documents.Should()
                .HaveCount(1);
            response.Data.PullWorkspace?.Documents.Should()
                .ContainSingle(workspace => workspace.Id == workspace1.Id);
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
    public async Task PullBulkShouldReturnAllDocuments()
    {
        TestContext? testContext = null;

        try
        {
            // Arrange
            testContext = await TestSetupUtil.SetupAsync();
            var workspace1 = await testContext.HttpClient.CreateNewWorkspaceAsync(testContext.CancellationToken);
            var workspace2 = await testContext.HttpClient.CreateNewWorkspaceAsync(testContext.CancellationToken);

            var query = new QueryQueryBuilderGql().WithPullWorkspace(new WorkspacePullBulkQueryBuilderGql().WithAllFields()
                .WithDocuments(new WorkspaceQueryBuilderGql().WithAllFields()), 1000);

            // Act
            var response = await testContext.HttpClient.PostGqlQueryAsync(query, testContext.CancellationToken);

            // Assert
            response.Errors.Should()
                .BeNullOrEmpty();

            response.Data.PullWorkspace?.Documents.Should()
                .ContainSingle(workspace => workspace.Id == workspace1.Id);
            response.Data.PullWorkspace?.Documents.Should()
                .ContainSingle(workspace => workspace.Id == workspace2.Id);
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
    public async Task ItShouldHandleMultiplePullsFollowedByAPush()
    {
        TestContext? testContext = null;

        try
        {
            // Arrange
            testContext = await TestSetupUtil.SetupAsync();
            // Act
            var response = await PushAndPullDocumentAsync(testContext);
            var response2 = await PushAndPullDocumentAsync(testContext);
            var response3 = await PushAndPullDocumentAsync(testContext);

            // Assert
            response.Errors.Should()
                .BeNullOrEmpty();
            response2.Errors.Should()
                .BeNullOrEmpty();
            response3.Errors.Should()
                .BeNullOrEmpty();
        }
        finally
        {
            if (testContext != null)
            {
                await testContext.DisposeAsync();
            }
        }
    }

    private static async Task<GqlQueryResponse> PushAndPullDocumentAsync(TestContext testContext)
    {
        // Push a document
        var newWorkspace = await testContext.HttpClient.CreateNewWorkspaceAsync(testContext.CancellationToken);

        // Pull a document
        var workspaceById = new WorkspaceFilterInputGql
        {
            Id = new UuidOperationFilterInputGql
            {
                Eq = newWorkspace.Id?.Value,
            },
        };
        var query = new QueryQueryBuilderGql().WithPullWorkspace(new WorkspacePullBulkQueryBuilderGql().WithAllFields()
            .WithDocuments(new WorkspaceQueryBuilderGql().WithAllFields()), 1000, where: workspaceById);

        return await testContext.HttpClient.PostGqlQueryAsync(query, testContext.CancellationToken);
    }
}
