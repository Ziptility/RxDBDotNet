using RxDBDotNet.Tests.Model;
using RxDBDotNet.Tests.Utils;

namespace RxDBDotNet.Tests;

[Collection("DockerSetup")]
public class SecurityTests : IAsyncLifetime
{
    private TestContext _testContext = null!;

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _testContext.DisposeAsync();
    }

    [Fact]
    public async Task AWorkspaceAdminShouldBeAbleToCreateAWorkspace()
    {
        // Arrange
        _testContext = TestSetupUtil.Setup(setupAuthorization: true,
            configureWorkspaceSecurity: options => options.RequirePolicyToCreate("IsWorkspaceAdmin"));

        var workspace = await _testContext.CreateWorkspaceAsync(_testContext.CancellationToken);
        var admin = await _testContext.CreateUserAsync(workspace, UserRole.WorkspaceAdmin, _testContext.CancellationToken);

        // Act
        var response = await _testContext.HttpClient.CreateWorkspaceAsync(_testContext.CancellationToken, admin.JwtAccessToken);

        // Assert
        await _testContext.HttpClient.VerifyWorkspaceAsync(response.workspaceInputGql, _testContext.CancellationToken);
    }

    [Fact]
    public async Task AStandardUserShouldNotBeAbleToCreateAWorkspace()
    {
        // Arrange
        _testContext = TestSetupUtil.Setup(setupAuthorization: true,
            configureWorkspaceSecurity: options => options.RequirePolicyToCreate("IsWorkspaceAdmin"));

        var workspace = await _testContext.CreateWorkspaceAsync(_testContext.CancellationToken);
        var standardUser = await _testContext.CreateUserAsync(workspace, UserRole.StandardUser, _testContext.CancellationToken);

        // Act
        var (_, response) = await _testContext.HttpClient.CreateWorkspaceAsync(_testContext.CancellationToken, standardUser.JwtAccessToken);

        // Assert
        response.Data.PushWorkspace?.Workspace.Should()
            .BeNullOrEmpty();
        response.Data.PushWorkspace?.Errors.Should()
            .HaveCount(1);
        response.Data.PushWorkspace?.Errors?.Single()
            .Should()
            .BeOfType<UnauthorizedAccessErrorGql>();
    }

    [Fact]
    public async Task ASystemAdminShouldBeAbleToReadAWorkspace()
    {
        // Arrange
        _testContext = TestSetupUtil.Setup(setupAuthorization: true,
            configureWorkspaceSecurity: options => options.RequirePolicyToRead("IsSystemAdmin"));

        var workspace = await _testContext.CreateWorkspaceAsync(_testContext.CancellationToken);
        var systemAdmin = await _testContext.CreateUserAsync(workspace, UserRole.SystemAdmin, _testContext.CancellationToken);

        // Act
        var query = new QueryQueryBuilderGql().WithPullWorkspace(new WorkspacePullBulkQueryBuilderGql().WithAllFields(), 1000);
        var response = await _testContext.HttpClient.PostGqlQueryAsync(query, _testContext.CancellationToken, systemAdmin.JwtAccessToken);

        // Assert
        response.Errors.Should()
            .BeNullOrEmpty();
        response.Data.PullWorkspace.Should()
            .NotBeNull();
        response.Data.PullWorkspace?.Documents.Should()
            .NotBeEmpty();
    }

    [Fact]
    public async Task AWorkspaceAdminShouldNotBeAbleToReadAWorkspaceWhenSystemAdminIsRequired()
    {
        // Arrange
        _testContext = TestSetupUtil.Setup(setupAuthorization: true,
            configureWorkspaceSecurity: options => options.RequirePolicyToRead("IsSystemAdmin"));

        var workspace = await _testContext.CreateWorkspaceAsync(_testContext.CancellationToken);
        var workspaceAdmin = await _testContext.CreateUserAsync(workspace, UserRole.WorkspaceAdmin, _testContext.CancellationToken);

        // Act
        var query = new QueryQueryBuilderGql().WithPullWorkspace(new WorkspacePullBulkQueryBuilderGql().WithAllFields(), 1000);
        var response = await _testContext.HttpClient.PostGqlQueryAsync(query, _testContext.CancellationToken, workspaceAdmin.JwtAccessToken);

        // Assert
        response.Errors.Should()
            .NotBeNullOrEmpty();
        response.Errors?.FirstOrDefault()?.Message.Should().Be("The current user is not authorized to access this resource.");
        response.Data?.PullWorkspace.Should()
            .BeNull();
    }

    [Fact]
    public async Task AWorkspaceAdminShouldBeAbleToUpdateAWorkspace()
    {
        // Arrange
        _testContext = TestSetupUtil.Setup(setupAuthorization: true,
            configureWorkspaceSecurity: options => options.RequirePolicyToUpdate("IsWorkspaceAdmin"));

        var workspace = await _testContext.CreateWorkspaceAsync(_testContext.CancellationToken);
        var admin = await _testContext.CreateUserAsync(workspace, UserRole.WorkspaceAdmin, _testContext.CancellationToken);

        // Act
        var workspaceToUpdate = new WorkspaceInputGql
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
            NewDocumentState = workspaceToUpdate,
        };

        var pushWorkspaceInputGql = new PushWorkspaceInputGql
        {
            WorkspacePushRow = new List<WorkspaceInputPushRowGql?> { workspaceInputPushRowGql },
        };

        var updateWorkspace = new MutationQueryBuilderGql()
            .WithPushWorkspace(new PushWorkspacePayloadQueryBuilderGql().WithAllFields(), pushWorkspaceInputGql);

        var response = await _testContext.HttpClient.PostGqlMutationAsync(updateWorkspace, _testContext.CancellationToken, admin.JwtAccessToken);

        // Assert
        response.Errors.Should().BeNullOrEmpty();
        response.Data.PushWorkspace?.Workspace.Should()
            .BeNullOrEmpty();

        var updatedWorkspace = await _testContext.HttpClient.GetWorkspaceByIdAsync(workspace.ReplicatedDocumentId, _testContext.CancellationToken);
        updatedWorkspace.Name.Should()
            .Be(workspaceToUpdate.Name.Value);
    }

    [Fact]
    public async Task AStandardUserShouldNotBeAbleToUpdateAWorkspace()
    {
        // Arrange
        _testContext = TestSetupUtil.Setup(setupAuthorization: true,
            configureWorkspaceSecurity: options => options.RequirePolicyToUpdate("IsWorkspaceAdmin"));

        var workspace = await _testContext.CreateWorkspaceAsync(_testContext.CancellationToken);
        var standardUser = await _testContext.CreateUserAsync(workspace, UserRole.StandardUser, _testContext.CancellationToken);

        // Act
        var workspaceToUpdate = new WorkspaceInputGql
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
            NewDocumentState = workspaceToUpdate,
        };

        var pushWorkspaceInputGql = new PushWorkspaceInputGql
        {
            WorkspacePushRow = new List<WorkspaceInputPushRowGql?> { workspaceInputPushRowGql },
        };

        var updateWorkspace = new MutationQueryBuilderGql()
            .WithPushWorkspace(new PushWorkspacePayloadQueryBuilderGql().WithAllFields(), pushWorkspaceInputGql);

        var response = await _testContext.HttpClient.PostGqlMutationAsync(updateWorkspace, _testContext.CancellationToken, standardUser.JwtAccessToken);

        // Assert
        response.Data.PushWorkspace?.Workspace.Should().BeNullOrEmpty();
        response.Data.PushWorkspace?.Errors.Should().HaveCount(1);
        response.Data.PushWorkspace?.Errors?.Single().Should().BeOfType<UnauthorizedAccessErrorGql>();
    }

    [Fact]
    public async Task ASystemAdminShouldBeAbleToDeleteAWorkspace()
    {
        // Arrange
        _testContext = TestSetupUtil.Setup(setupAuthorization: true,
            configureWorkspaceSecurity: options => options.RequirePolicyToDelete("IsSystemAdmin"));

        var workspace = await _testContext.CreateWorkspaceAsync(_testContext.CancellationToken);
        var systemAdmin = await _testContext.CreateUserAsync(workspace, UserRole.SystemAdmin, _testContext.CancellationToken);

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

        var response = await _testContext.HttpClient.PostGqlMutationAsync(deleteWorkspace, _testContext.CancellationToken, systemAdmin.JwtAccessToken);

        // Assert
        response.Errors.Should().BeNullOrEmpty();
        response.Data.PushWorkspace?.Workspace.Should().BeNullOrEmpty();

        var deletedWorkspace = await _testContext.HttpClient.GetWorkspaceByIdAsync(workspace.ReplicatedDocumentId, _testContext.CancellationToken);
        deletedWorkspace.IsDeleted.Should()
            .BeTrue();
    }

    [Fact]
    public async Task AWorkspaceAdminShouldNotBeAbleToDeleteAWorkspaceWhenSystemAdminIsRequired()
    {
        // Arrange
        _testContext = TestSetupUtil.Setup(setupAuthorization: true,
            configureWorkspaceSecurity: options => options.RequirePolicyToDelete("IsSystemAdmin"));

        var workspace = await _testContext.CreateWorkspaceAsync(_testContext.CancellationToken);
        var workspaceAdmin = await _testContext.CreateUserAsync(workspace, UserRole.WorkspaceAdmin, _testContext.CancellationToken);

        // Act
        var deletedWorkspace = new WorkspaceInputGql
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
            NewDocumentState = deletedWorkspace,
        };

        var pushWorkspaceInputGql = new PushWorkspaceInputGql
        {
            WorkspacePushRow = new List<WorkspaceInputPushRowGql?> { workspaceInputPushRowGql },
        };

        var deleteWorkspace = new MutationQueryBuilderGql()
            .WithPushWorkspace(new PushWorkspacePayloadQueryBuilderGql().WithAllFields(), pushWorkspaceInputGql);

        var response = await _testContext.HttpClient.PostGqlMutationAsync(deleteWorkspace, _testContext.CancellationToken, workspaceAdmin.JwtAccessToken);

        // Assert
        response.Data.PushWorkspace?.Workspace.Should().BeNullOrEmpty();
        response.Data.PushWorkspace?.Errors.Should().HaveCount(1);
        response.Data.PushWorkspace?.Errors?.Single().Should().BeOfType<UnauthorizedAccessErrorGql>();
    }
}
