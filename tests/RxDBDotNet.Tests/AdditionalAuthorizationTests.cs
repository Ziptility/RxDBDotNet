using RxDBDotNet.Tests.Model;
using RxDBDotNet.Tests.Utils;

namespace RxDBDotNet.Tests;

[Collection("DockerSetup")]
public class AdditionalAuthorizationTests : IAsyncLifetime
{
    private TestContext _testContext = null!;

    public Task InitializeAsync()
    {
        _testContext = TestSetupUtil.Setup(setupAuthorization: true);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _testContext.DisposeAsync();
    }

    [Fact]
    public async Task UnauthenticatedUser_ShouldNotBeAbleToReadWorkspace()
    {
        // Arrange
        _testContext = TestSetupUtil.Setup(setupAuthorization: true,
            configureWorkspaceSecurity: options => options.RequirePolicyToRead("IsWorkspaceAdmin"));

        var workspace = await _testContext.CreateWorkspaceAsync(_testContext.CancellationToken);

        // Act
        var query = new QueryQueryBuilderGql().WithPullWorkspace(new WorkspacePullBulkQueryBuilderGql().WithAllFields(), 1000);
        var response = await _testContext.HttpClient.PostGqlQueryAsync(query, _testContext.CancellationToken);

        // Assert
        response.Errors.Should().NotBeNullOrEmpty();
        response.Errors?.FirstOrDefault()?.Message.Should().Be("The current user is not authorized to access this resource.");
        response.Data?.PullWorkspace.Should().BeNull();
    }

    [Fact]
    public async Task StandardUser_ShouldNotBeAbleToDeleteWorkspace()
    {
        // Arrange
        _testContext = TestSetupUtil.Setup(setupAuthorization: true,
            configureWorkspaceSecurity: options => options.RequirePolicyToDelete("IsSystemAdmin"));

        var workspace = await _testContext.CreateWorkspaceAsync(_testContext.CancellationToken);
        var standardUser = await _testContext.CreateUserAsync(workspace, UserRole.StandardUser, _testContext.CancellationToken);

        // Act
        var workspaceToDelete = new WorkspaceInputGql
        {
            Id = workspace.ReplicatedDocumentId,
            Name = workspace.Name,
            IsDeleted = true,
            UpdatedAt = DateTimeOffset.UtcNow,
            Topics = workspace.Topics?.Select(t => t.Name).ToList(),
        };

        var workspaceInputPushRowGql = new WorkspaceInputPushRowGql
        {
            AssumedMasterState = new WorkspaceInputGql
            {
                Id = workspace.ReplicatedDocumentId,
                Name = workspace.Name,
                IsDeleted = workspace.IsDeleted,
                UpdatedAt = workspace.UpdatedAt,
                Topics = workspace.Topics?.Select(t => t.Name).ToList(),
            },
            NewDocumentState = workspaceToDelete,
        };

        var pushWorkspaceInputGql = new PushWorkspaceInputGql
        {
            WorkspacePushRow = new List<WorkspaceInputPushRowGql?> { workspaceInputPushRowGql },
        };

        var deleteWorkspace = new MutationQueryBuilderGql()
            .WithPushWorkspace(new PushWorkspacePayloadQueryBuilderGql().WithAllFields(), pushWorkspaceInputGql);

        var response = await _testContext.HttpClient.PostGqlMutationAsync(deleteWorkspace, _testContext.CancellationToken, standardUser.JwtAccessToken);

        // Assert
        response.Data.PushWorkspace?.Workspace.Should().BeNullOrEmpty();
        response.Data.PushWorkspace?.Errors.Should().HaveCount(1);
        response.Data.PushWorkspace?.Errors?.Single().Should().BeOfType<UnauthorizedAccessErrorGql>();
    }

    [Fact]
    public async Task WorkspaceAdmin_ShouldBeAbleToReadAndUpdateWorkspace()
    {
        // Arrange
        _testContext = TestSetupUtil.Setup(setupAuthorization: true,
            configureWorkspaceSecurity: options => options
                .RequirePolicyToRead("IsWorkspaceAdmin")
                .RequirePolicyToUpdate("IsWorkspaceAdmin"));

        var workspace = await _testContext.CreateWorkspaceAsync(_testContext.CancellationToken);
        var workspaceAdmin = await _testContext.CreateUserAsync(workspace, UserRole.WorkspaceAdmin, _testContext.CancellationToken);

        // Act - Read
        var readQuery = new QueryQueryBuilderGql().WithPullWorkspace(new WorkspacePullBulkQueryBuilderGql().WithAllFields(), 1000);
        var readResponse = await _testContext.HttpClient.PostGqlQueryAsync(readQuery, _testContext.CancellationToken, workspaceAdmin.JwtAccessToken);

        // Assert - Read
        readResponse.Errors.Should().BeNullOrEmpty();
        readResponse.Data.PullWorkspace.Should().NotBeNull();
        readResponse.Data.PullWorkspace?.Documents.Should().NotBeEmpty();

        // Act - Update
        var updatedWorkspace = new WorkspaceInputGql
        {
            Id = workspace.ReplicatedDocumentId,
            Name = Strings.CreateString(),
            IsDeleted = workspace.IsDeleted,
            UpdatedAt = DateTimeOffset.UtcNow,
            Topics = workspace.Topics?.Select(t => t.Name).ToList(),
        };

        var workspaceInputPushRowGql = new WorkspaceInputPushRowGql
        {
            AssumedMasterState = new WorkspaceInputGql
            {
                Id = workspace.ReplicatedDocumentId,
                Name = workspace.Name,
                IsDeleted = workspace.IsDeleted,
                UpdatedAt = workspace.UpdatedAt,
                Topics = workspace.Topics?.Select(t => t.Name).ToList(),
            },
            NewDocumentState = updatedWorkspace,
        };

        var pushWorkspaceInputGql = new PushWorkspaceInputGql
        {
            WorkspacePushRow = new List<WorkspaceInputPushRowGql?> { workspaceInputPushRowGql },
        };

        var updateWorkspace = new MutationQueryBuilderGql()
            .WithPushWorkspace(new PushWorkspacePayloadQueryBuilderGql().WithAllFields(), pushWorkspaceInputGql);

        var updateResponse = await _testContext.HttpClient.PostGqlMutationAsync(updateWorkspace, _testContext.CancellationToken, workspaceAdmin.JwtAccessToken);

        // Assert - Update
        updateResponse.Errors.Should().BeNullOrEmpty();
        updateResponse.Data.PushWorkspace?.Workspace.Should().BeNullOrEmpty();
        updateResponse.Data.PushWorkspace?.Errors.Should().BeNullOrEmpty();

        // Verify the update
        var verifyQuery = new QueryQueryBuilderGql().WithPullWorkspace(new WorkspacePullBulkQueryBuilderGql().WithAllFields(), 1000);
        var verifyResponse = await _testContext.HttpClient.PostGqlQueryAsync(verifyQuery, _testContext.CancellationToken, workspaceAdmin.JwtAccessToken);

        verifyResponse.Errors.Should().BeNullOrEmpty();
        verifyResponse.Data.PullWorkspace?.Documents.Should().Contain(w => w.Name == updatedWorkspace.Name!.Value);
    }

    [Fact]
    public async Task SystemAdmin_ShouldHaveFullAccessToWorkspace()
    {
        // Arrange
        _testContext = TestSetupUtil.Setup(setupAuthorization: true,
            configureWorkspaceSecurity: options => options
                .RequirePolicyToRead("IsSystemAdmin")
                .RequirePolicyToCreate("IsSystemAdmin")
                .RequirePolicyToUpdate("IsSystemAdmin")
                .RequirePolicyToDelete("IsSystemAdmin"));

        var workspace = await _testContext.CreateWorkspaceAsync(_testContext.CancellationToken);
        var systemAdmin = await _testContext.CreateUserAsync(workspace, UserRole.SystemAdmin, _testContext.CancellationToken);

        // Act & Assert - Create
        var newWorkspace = new WorkspaceInputGql
        {
            Id = Provider.Sql.Create(),
            Name = Strings.CreateString(),
            UpdatedAt = DateTimeOffset.UtcNow,
            IsDeleted = false,
            Topics = new List<string> { Provider.Sql.Create().ToString() },
        };

        var createWorkspaceInputPushRowGql = new WorkspaceInputPushRowGql
        {
            AssumedMasterState = null,
            NewDocumentState = newWorkspace,
        };

        var createWorkspaceInputGql = new PushWorkspaceInputGql
        {
            WorkspacePushRow = new List<WorkspaceInputPushRowGql?> { createWorkspaceInputPushRowGql },
        };

        var createWorkspaceMutation = new MutationQueryBuilderGql()
            .WithPushWorkspace(new PushWorkspacePayloadQueryBuilderGql().WithAllFields(), createWorkspaceInputGql);

        var createResponse = await _testContext.HttpClient.PostGqlMutationAsync(createWorkspaceMutation, _testContext.CancellationToken, systemAdmin.JwtAccessToken);

        createResponse.Errors.Should().BeNullOrEmpty();
        createResponse.Data.PushWorkspace?.Workspace.Should().BeNullOrEmpty();
        createResponse.Data.PushWorkspace?.Errors.Should().BeNullOrEmpty();

        // Act & Assert - Read
        var readQuery = new QueryQueryBuilderGql().WithPullWorkspace(new WorkspacePullBulkQueryBuilderGql().WithAllFields(), 1000);
        var readResponse = await _testContext.HttpClient.PostGqlQueryAsync(readQuery, _testContext.CancellationToken, systemAdmin.JwtAccessToken);

        readResponse.Errors.Should().BeNullOrEmpty();
        readResponse.Data.PullWorkspace.Should().NotBeNull();
        readResponse.Data.PullWorkspace?.Documents.Should().Contain(w => w.Name == newWorkspace.Name!.Value);

        // Act & Assert - Update
        var updatedWorkspace = new WorkspaceInputGql
        {
            Id = newWorkspace.Id,
            Name = Strings.CreateString(),
            IsDeleted = newWorkspace.IsDeleted,
            UpdatedAt = DateTimeOffset.UtcNow,
            Topics = newWorkspace.Topics,
        };

        var updateWorkspaceInputPushRowGql = new WorkspaceInputPushRowGql
        {
            AssumedMasterState = newWorkspace,
            NewDocumentState = updatedWorkspace,
        };

        var updateWorkspaceInputGql = new PushWorkspaceInputGql
        {
            WorkspacePushRow = new List<WorkspaceInputPushRowGql?> { updateWorkspaceInputPushRowGql },
        };

        var updateWorkspaceMutation = new MutationQueryBuilderGql()
            .WithPushWorkspace(new PushWorkspacePayloadQueryBuilderGql().WithAllFields(), updateWorkspaceInputGql);

        var updateResponse = await _testContext.HttpClient.PostGqlMutationAsync(updateWorkspaceMutation, _testContext.CancellationToken, systemAdmin.JwtAccessToken);

        updateResponse.Errors.Should().BeNullOrEmpty();
        updateResponse.Data.PushWorkspace?.Workspace.Should().BeNullOrEmpty();
        updateResponse.Data.PushWorkspace?.Errors.Should().BeNullOrEmpty();

        // Act & Assert - Delete
        var deleteWorkspace = new WorkspaceInputGql
        {
            Id = updatedWorkspace.Id,
            Name = updatedWorkspace.Name,
            IsDeleted = true,
            UpdatedAt = DateTimeOffset.UtcNow,
            Topics = updatedWorkspace.Topics,
        };

        var deleteWorkspaceInputPushRowGql = new WorkspaceInputPushRowGql
        {
            AssumedMasterState = updatedWorkspace,
            NewDocumentState = deleteWorkspace,
        };

        var deleteWorkspaceInputGql = new PushWorkspaceInputGql
        {
            WorkspacePushRow = new List<WorkspaceInputPushRowGql?> { deleteWorkspaceInputPushRowGql },
        };

        var deleteWorkspaceMutation = new MutationQueryBuilderGql()
            .WithPushWorkspace(new PushWorkspacePayloadQueryBuilderGql().WithAllFields(), deleteWorkspaceInputGql);

        var deleteResponse = await _testContext.HttpClient.PostGqlMutationAsync(deleteWorkspaceMutation, _testContext.CancellationToken, systemAdmin.JwtAccessToken);

        deleteResponse.Errors.Should().BeNullOrEmpty();
        deleteResponse.Data.PushWorkspace?.Workspace.Should().BeNullOrEmpty();
        deleteResponse.Data.PushWorkspace?.Errors.Should().BeNullOrEmpty();

        // Verify deletion
        var verifyQuery = new QueryQueryBuilderGql().WithPullWorkspace(new WorkspacePullBulkQueryBuilderGql().WithAllFields(), 1000);
        var verifyResponse = await _testContext.HttpClient.PostGqlQueryAsync(verifyQuery, _testContext.CancellationToken, systemAdmin.JwtAccessToken);

        verifyResponse.Errors.Should().BeNullOrEmpty();
        verifyResponse.Data.PullWorkspace?.Documents.Should().Contain(w => w.Id == deleteWorkspace.Id!.Value && w.IsDeleted);
    }
}
