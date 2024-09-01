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

            var createWorkspace = new MutationQueryBuilderGql()
                .WithPushWorkspace(new PushWorkspacePayloadQueryBuilderGql()
                    .WithAllFields(), workspaceInputGql);

            // Act
            var response = await testContext.HttpClient.PostGqlMutationAsync(createWorkspace, testContext.CancellationToken);

            // Assert
            response.Errors.Should()
                .BeNullOrEmpty();
            response.Data.PushWorkspace?.Errors.Should()
                .BeNullOrEmpty();
            response.Data.PushWorkspace?.Workspace.Should().BeNullOrEmpty();

            await testContext.HttpClient.VerifyWorkspaceAsync(workspaceInput, testContext.CancellationToken);
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

            var workspace1 = await testContext.HttpClient.CreateWorkspaceAsync(testContext.CancellationToken);
            await testContext.HttpClient.CreateWorkspaceAsync(testContext.CancellationToken);

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
            var response = await testContext.HttpClient.PostGqlQueryAsync(query, testContext.CancellationToken);

            // Assert
            response.Errors.Should()
                .BeNullOrEmpty();

            response.Data.PullWorkspace?.Documents.Should()
                .HaveCount(1);
            response.Data.PullWorkspace?.Documents.Should()
                .ContainSingle(workspace => workspace.Id == workspace1.workspaceInputGql.Id);
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
            var workspace1 = await testContext.HttpClient.CreateWorkspaceAsync(testContext.CancellationToken);
            var workspace2 = await testContext.HttpClient.CreateWorkspaceAsync(testContext.CancellationToken);

            var query = new QueryQueryBuilderGql().WithPullWorkspace(new WorkspacePullBulkQueryBuilderGql().WithAllFields()
                .WithDocuments(new WorkspaceQueryBuilderGql().WithAllFields()), 1000);

            // Act
            var response = await testContext.HttpClient.PostGqlQueryAsync(query, testContext.CancellationToken);

            // Assert
            response.Errors.Should()
                .BeNullOrEmpty();

            response.Data.PullWorkspace?.Documents.Should()
                .ContainSingle(workspace => workspace.Id == workspace1.workspaceInputGql.Id);
            response.Data.PullWorkspace?.Documents.Should()
                .ContainSingle(workspace => workspace.Id == workspace2.workspaceInputGql.Id);
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
