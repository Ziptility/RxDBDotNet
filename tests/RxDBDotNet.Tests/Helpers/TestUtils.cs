using System.Diagnostics;
using FluentAssertions;
using RT.Comb;
using RxDBDotNet.Tests.Model;
using static RxDBDotNet.Tests.Helpers.Strings;

namespace RxDBDotNet.Tests.Helpers;

internal static class TestUtils
{
    public static async Task<WorkspaceInputGql> CreateNewWorkspaceAsync(this HttpClient httpClient)
    {
        var workspaceId = Provider.Sql.Create();

        var newWorkspace = new WorkspaceInputGql
        {
            Id = workspaceId,
            Name = CreateString(),
            UpdatedAt = DateTimeOffset.UtcNow,
            IsDeleted = false,
            Topics = new List<string>{ workspaceId.ToString() },
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

    public static async Task<WorkspaceGql> UpdateWorkspaceAsync(this HttpClient httpClient, WorkspaceInputGql workspace)
    {
        var assumedMasterState = new WorkspaceInputGql
        {
            Id = workspace.Id,
            Name = workspace.Name,
            IsDeleted = workspace.IsDeleted,
            UpdatedAt = workspace.UpdatedAt,
            Topics = workspace.Topics,
        };

        var newDocumentState = new WorkspaceInputGql
        {
            Id = workspace.Id,
            Name = CreateString(),
            IsDeleted = workspace.IsDeleted,
            UpdatedAt = DateTimeOffset.Now,
            Topics = workspace.Topics,
        };

        var pushWorkspace = new List<WorkspaceInputPushRowGql?>
        {
            new()
            {
                AssumedMasterState = assumedMasterState,
                NewDocumentState = newDocumentState,
            },
        };

        var updateWorkspace = new MutationQueryBuilderGql().WithPushWorkspace(
            new WorkspaceQueryBuilderGql().WithAllFields(),
            pushWorkspace);

        var response = await httpClient.PostGqlMutationAsync(updateWorkspace);

        response.Errors.Should()
            .BeNullOrEmpty();

        response.Data.PushWorkspace.Should()
            .BeEmpty();

        return await httpClient.GetWorkspaceByIdAsync(workspace.Id);
    }

    public static async Task<WorkspaceGql> UpdateWorkspaceAsync(this HttpClient httpClient, WorkspaceGql workspace)
    {
        var assumedMasterState = new WorkspaceInputGql
        {
            Id = workspace.Id,
            Name = workspace.Name,
            IsDeleted = workspace.IsDeleted,
            UpdatedAt = workspace.UpdatedAt,
            Topics = workspace.Topics == null ? null : (List<string>)workspace.Topics,
        };

        var newDocumentState = new WorkspaceInputGql
        {
            Id = workspace.Id,
            Name = CreateString(),
            IsDeleted = workspace.IsDeleted,
            UpdatedAt = DateTimeOffset.Now,
            Topics = workspace.Topics == null ? null : (List<string>)workspace.Topics,
        };

        var pushWorkspace = new List<WorkspaceInputPushRowGql?>
        {
            new()
            {
                AssumedMasterState = assumedMasterState,
                NewDocumentState = newDocumentState,
            },
        };

        var updateWorkspace = new MutationQueryBuilderGql().WithPushWorkspace(
            new WorkspaceQueryBuilderGql().WithAllFields(),
            pushWorkspace);

        var response = await httpClient.PostGqlMutationAsync(updateWorkspace);

        response.Errors.Should()
            .BeNullOrEmpty();

        response.Data.PushWorkspace.Should()
            .BeNullOrEmpty();

        return await httpClient.GetWorkspaceByIdAsync(workspace.Id);
    }

    public static async Task VerifyWorkspaceExists(this HttpClient httpClient, WorkspaceInputGql workspaceInput)
    {
        var query = new QueryQueryBuilderGql().WithPullWorkspace(
            new WorkspacePullBulkQueryBuilderGql().WithAllFields(),
            limit: 10);

        var response = await httpClient.PostGqlQueryAsync(query);

        response.Errors.Should()
            .BeNullOrEmpty();

        var existingWorkspace = response.Data.PullWorkspace?.Documents?.SingleOrDefault(workspace => workspace.Id == workspaceInput.Id);

        if (existingWorkspace == null)
        {
            Assert.Fail("The existing workspace must not be null");
        }

        Debug.Assert(workspaceInput.Id != null, "workspaceInput.Id != null");
        existingWorkspace.Id.Should().Be(workspaceInput.Id.Value);
        existingWorkspace.Name.Should().Be(workspaceInput.Name?.Value);
        existingWorkspace.IsDeleted.Should().Be(workspaceInput.IsDeleted?.Value);
        existingWorkspace.UpdatedAt.Should().Be(workspaceInput.UpdatedAt?.Value.StripMicroseconds());
        existingWorkspace.Topics.Should().Equal(workspaceInput.Topics?.Value);
    }

    public static async Task<WorkspaceGql> GetWorkspaceByIdAsync(this HttpClient httpClient, Guid workspaceId)
    {
        var query = new QueryQueryBuilderGql().WithPullWorkspace(
            new WorkspacePullBulkQueryBuilderGql().WithAllFields(),
            limit: 10);

        var response = await httpClient.PostGqlQueryAsync(query);

        response.Errors.Should()
            .BeNullOrEmpty();

        return response.Data.PullWorkspace?.Documents?.Single(workspace => workspace.Id == workspaceId)
               ?? throw new InvalidOperationException("The workspace must not be null");
    }
}
