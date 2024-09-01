using System.Diagnostics;
using RxDBDotNet.Tests.Model;
using static RxDBDotNet.Tests.Helpers.Strings;

namespace RxDBDotNet.Tests.Helpers;

internal static class TestUtils
{
    public static async Task<(WorkspaceInputGql workspaceInputGql, GqlMutationResponse response)> CreateWorkspaceAsync(
        this HttpClient httpClient,
        CancellationToken cancellationToken,
        string? jwtAccessToken = null)
    {
        var workspaceId = Provider.Sql.Create();

        var newWorkspace = new WorkspaceInputGql
        {
            Id = workspaceId,
            Name = CreateString(),
            UpdatedAt = DateTimeOffset.UtcNow,
            IsDeleted = false,
            Topics = new List<string> { workspaceId.ToString() },
        };

        var workspaceInputPushRowGql = new WorkspaceInputPushRowGql
        {
            AssumedMasterState = null,
            NewDocumentState = newWorkspace,
        };

        var pushWorkspaceInputGql = new PushWorkspaceInputGql
        {
            WorkspacePushRow = new List<WorkspaceInputPushRowGql?>
            {
                workspaceInputPushRowGql,
            },
        };

        var createWorkspace = new MutationQueryBuilderGql()
            .WithPushWorkspace(new PushWorkspacePayloadQueryBuilderGql()
                .WithAllFields()
                .WithErrors(new PushWorkspaceErrorQueryBuilderGql()
                    .WithAuthenticationErrorFragment(new AuthenticationErrorQueryBuilderGql()
                        .WithAllFields())), pushWorkspaceInputGql);

        var response = await httpClient.PostGqlMutationAsync(
            createWorkspace,
            cancellationToken,
            jwtAccessToken: jwtAccessToken);

        return (newWorkspace, response);
    }

    public static async Task<WorkspaceGql> UpdateWorkspaceAsync(
        this HttpClient httpClient,
        WorkspaceInputGql workspace,
        CancellationToken cancellationToken)
    {
        var assumedMasterState = new WorkspaceInputGql
        {
            Id = workspace.Id,
            Name = workspace.Name,
            IsDeleted = workspace.IsDeleted,
            UpdatedAt = workspace.UpdatedAt,
            Topics = workspace.Topics,
        };

        var updatedWorkspace = new WorkspaceInputGql
        {
            Id = workspace.Id,
            Name = CreateString(),
            IsDeleted = workspace.IsDeleted,
            UpdatedAt = DateTimeOffset.Now,
            Topics = workspace.Topics,
        };

        var workspaceInputPushRowGql = new WorkspaceInputPushRowGql
        {
            AssumedMasterState = assumedMasterState,
            NewDocumentState = updatedWorkspace,
        };

        var workspaceInputGql = new PushWorkspaceInputGql
        {
            WorkspacePushRow = new List<WorkspaceInputPushRowGql?>
            {
                workspaceInputPushRowGql,
            },
        };

        var updateWorkspace = new MutationQueryBuilderGql()
            .WithPushWorkspace(new PushWorkspacePayloadQueryBuilderGql()
                .WithAllFields(), workspaceInputGql);

        var response = await httpClient.PostGqlMutationAsync(updateWorkspace, cancellationToken);

        response.Errors.Should()
            .BeNullOrEmpty();

        return await httpClient.GetWorkspaceByIdAsync(workspace.Id, cancellationToken);
    }

    public static async Task<WorkspaceGql> UpdateWorkspaceAsync(this HttpClient httpClient, WorkspaceGql workspace, CancellationToken cancellationToken)
    {
        var assumedMasterState = new WorkspaceInputGql
        {
            Id = workspace.Id,
            Name = workspace.Name,
            IsDeleted = workspace.IsDeleted,
            UpdatedAt = workspace.UpdatedAt,
            Topics = (List<string>?)workspace.Topics,
        };

        var updatedWorkspace = new WorkspaceInputGql
        {
            Id = workspace.Id,
            Name = CreateString(),
            IsDeleted = workspace.IsDeleted,
            UpdatedAt = DateTimeOffset.Now,
            Topics = (List<string>?)workspace.Topics,
        };

        var workspaceInputPushRowGql = new WorkspaceInputPushRowGql
        {
            AssumedMasterState = assumedMasterState,
            NewDocumentState = updatedWorkspace,
        };

        var workspaceInputGql = new PushWorkspaceInputGql
        {
            WorkspacePushRow = new List<WorkspaceInputPushRowGql?>
            {
                workspaceInputPushRowGql,
            },
        };

        var updateWorkspace = new MutationQueryBuilderGql()
            .WithPushWorkspace(new PushWorkspacePayloadQueryBuilderGql()
                .WithAllFields(), workspaceInputGql);

        var response = await httpClient.PostGqlMutationAsync(updateWorkspace, cancellationToken);

        response.Errors.Should()
            .BeNullOrEmpty();

        return await httpClient.GetWorkspaceByIdAsync(workspace.Id, cancellationToken);
    }

    public static async Task VerifyWorkspaceAsync(this HttpClient httpClient, WorkspaceInputGql workspaceInput, CancellationToken cancellationToken)
    {
        var query = new QueryQueryBuilderGql().WithPullWorkspace(
            new WorkspacePullBulkQueryBuilderGql().WithAllFields(),
            limit: 20000);

        var response = await httpClient.PostGqlQueryAsync(query, cancellationToken);

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

    public static async Task<WorkspaceGql> GetWorkspaceByIdAsync(this HttpClient httpClient, Guid workspaceId, CancellationToken cancellationToken)
    {
        var query = new QueryQueryBuilderGql().WithPullWorkspace(
            new WorkspacePullBulkQueryBuilderGql().WithAllFields(),
            limit: 1000);

        var response = await httpClient.PostGqlQueryAsync(query, cancellationToken);

        response.Errors.Should()
            .BeNullOrEmpty();

        return response.Data.PullWorkspace?.Documents?.Single(workspace => workspace.Id == workspaceId)
               ?? throw new InvalidOperationException("The workspace must not be null");
    }
}
