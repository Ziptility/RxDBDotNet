using System.Diagnostics;
using FluentAssertions;
using RT.Comb;
using RxDBDotNet.Tests.Model;

namespace RxDBDotNet.Tests.Utils;

internal static class TestUtils
{
    public static async Task<WorkspaceInputGql> CreateNewWorkspaceAsync(this HttpClient httpClient)
    {
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
        var response = await httpClient.PostGqlMutationAsync(createWorkspace);

        // Assert
        response.Errors.Should()
            .BeNullOrEmpty();
        response.Data.PushWorkspace?.Should()
            .BeNullOrEmpty();

        return newWorkspace;
    }

    public static async Task VerifyWorkspaceExists(this HttpClient httpClient, WorkspaceInputGql workspaceInput)
    {
        var query = new QueryQueryBuilderGql().WithPullWorkspace(
            new WorkspacePullBulkQueryBuilderGql().WithAllFields(),
            limit: 10);

        var response = await httpClient.PostGqlQueryAsync(query);

        response.Errors.Should()
            .BeNullOrEmpty();

        response.Data.PullWorkspace?.Documents?.Should()
            .ContainSingle(workspace => workspace.Id == workspaceInput.Id
                                        && workspace.Name == workspaceInput.Name
                                        && workspace.IsDeleted == workspaceInput.IsDeleted);

        var existingWorkspace = response.Data.PullWorkspace?.Documents?.Single(workspace => workspace.Id == workspaceInput.Id);

        Debug.Assert(existingWorkspace != null, nameof(existingWorkspace) + " != null");
        workspaceInput.UpdatedAt?.Value.Should()
            .BeCloseTo(existingWorkspace.UpdatedAt, TimeSpan.FromMilliseconds(2));
    }
}
