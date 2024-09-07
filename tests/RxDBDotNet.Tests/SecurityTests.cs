﻿using RxDBDotNet.Tests.Model;
using RxDBDotNet.Tests.Utils;

namespace RxDBDotNet.Tests;

[Collection("DockerSetup")]
public class SecurityTests : IAsyncLifetime
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
    public async Task AWorkspaceAdminShouldBeAbleToCreateAWorkspace()
    {
        // Arrange
        TestContext = TestSetupUtil.SetupWithDefaultsAndCustomConfig(setupAuthorization: true,
            configureWorkspaceSecurity: options => options.RequirePolicyToCreate("IsWorkspaceAdmin"));

        var workspace = await TestContext.CreateWorkspaceAsync(TestContext.CancellationToken);
        var admin = await TestContext.CreateUserAsync(workspace, UserRole.WorkspaceAdmin, TestContext.CancellationToken);

        // Act
        var response = await TestContext.HttpClient.CreateWorkspaceAsync(TestContext.CancellationToken, admin.JwtAccessToken);

        // Assert
        await TestContext.HttpClient.VerifyWorkspaceAsync(response.workspaceInputGql, TestContext.CancellationToken);
    }

    [Fact]
    public async Task AStandardUserShouldNotBeAbleToCreateAWorkspace()
    {
        // Arrange
        TestContext = TestSetupUtil.SetupWithDefaultsAndCustomConfig(setupAuthorization: true,
            configureWorkspaceSecurity: options => options.RequirePolicyToCreate("IsWorkspaceAdmin"));

        var workspace = await TestContext.CreateWorkspaceAsync(TestContext.CancellationToken);
        var standardUser = await TestContext.CreateUserAsync(workspace, UserRole.StandardUser, TestContext.CancellationToken);

        // Act
        var (_, response) = await TestContext.HttpClient.CreateWorkspaceAsync(TestContext.CancellationToken, standardUser.JwtAccessToken);

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
        TestContext = TestSetupUtil.SetupWithDefaultsAndCustomConfig(setupAuthorization: true,
            configureWorkspaceSecurity: options => options.RequirePolicyToRead("IsSystemAdmin"));

        var workspace = await TestContext.CreateWorkspaceAsync(TestContext.CancellationToken);
        var systemAdmin = await TestContext.CreateUserAsync(workspace, UserRole.SystemAdmin, TestContext.CancellationToken);

        // Act
        var query = new QueryQueryBuilderGql().WithPullWorkspace(new WorkspacePullBulkQueryBuilderGql().WithAllFields(), 1000);
        var response = await TestContext.HttpClient.PostGqlQueryAsync(query, TestContext.CancellationToken, systemAdmin.JwtAccessToken);

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
        TestContext = TestSetupUtil.SetupWithDefaultsAndCustomConfig(setupAuthorization: true,
            configureWorkspaceSecurity: options => options.RequirePolicyToRead("IsSystemAdmin"));

        var workspace = await TestContext.CreateWorkspaceAsync(TestContext.CancellationToken);
        var workspaceAdmin = await TestContext.CreateUserAsync(workspace, UserRole.WorkspaceAdmin, TestContext.CancellationToken);

        // Act
        var query = new QueryQueryBuilderGql().WithPullWorkspace(new WorkspacePullBulkQueryBuilderGql().WithAllFields(), 1000);
        var response = await TestContext.HttpClient.PostGqlQueryAsync(query, TestContext.CancellationToken, workspaceAdmin.JwtAccessToken);

        // Assert
        response.Errors.Should()
            .NotBeNullOrEmpty();
        response.Errors?.FirstOrDefault()
            ?.Message.Should()
            .Be("The current user is not authorized to access this resource.");
        response.Data?.PullWorkspace.Should()
            .BeNull();
    }

    [Fact]
    public async Task AWorkspaceAdminShouldBeAbleToUpdateAWorkspace()
    {
        // Arrange
        TestContext = TestSetupUtil.SetupWithDefaultsAndCustomConfig(setupAuthorization: true,
            configureWorkspaceSecurity: options => options.RequirePolicyToUpdate("IsWorkspaceAdmin"));

        var workspace = await TestContext.CreateWorkspaceAsync(TestContext.CancellationToken);
        var admin = await TestContext.CreateUserAsync(workspace, UserRole.WorkspaceAdmin, TestContext.CancellationToken);

        // Act
        var workspaceToUpdate = new WorkspaceInputGql
        {
            Id = workspace.ReplicatedDocumentId,
            Name = Strings.CreateString(),
            IsDeleted = workspace.IsDeleted,
            UpdatedAt = DateTimeOffset.UtcNow,
            Topics = workspace.Topics?.Select(t => t.Name)
                .ToList(),
        };

        var workspaceInputPushRowGql = new WorkspaceInputPushRowGql
        {
            AssumedMasterState = new WorkspaceInputGql
            {
                Id = workspace.ReplicatedDocumentId,
                Name = workspace.Name,
                IsDeleted = workspace.IsDeleted,
                UpdatedAt = workspace.UpdatedAt,
                Topics = workspace.Topics?.Select(t => t.Name)
                    .ToList(),
            },
            NewDocumentState = workspaceToUpdate,
        };

        var pushWorkspaceInputGql = new PushWorkspaceInputGql
        {
            WorkspacePushRow = new List<WorkspaceInputPushRowGql?>
            {
                workspaceInputPushRowGql,
            },
        };

        var updateWorkspace =
            new MutationQueryBuilderGql().WithPushWorkspace(new PushWorkspacePayloadQueryBuilderGql().WithAllFields(), pushWorkspaceInputGql);

        var response = await TestContext.HttpClient.PostGqlMutationAsync(updateWorkspace, TestContext.CancellationToken, admin.JwtAccessToken);

        // Assert
        response.Errors.Should()
            .BeNullOrEmpty();
        response.Data.PushWorkspace?.Workspace.Should()
            .BeNullOrEmpty();

        var updatedWorkspace = await TestContext.HttpClient.GetWorkspaceByIdAsync(workspace.ReplicatedDocumentId, TestContext.CancellationToken);
        updatedWorkspace.Name.Should()
            .Be(workspaceToUpdate.Name.Value);
    }

    [Fact]
    public async Task AStandardUserShouldNotBeAbleToUpdateAWorkspace()
    {
        // Arrange
        TestContext = TestSetupUtil.SetupWithDefaultsAndCustomConfig(setupAuthorization: true,
            configureWorkspaceSecurity: options => options.RequirePolicyToUpdate("IsWorkspaceAdmin"));

        var workspace = await TestContext.CreateWorkspaceAsync(TestContext.CancellationToken);
        var standardUser = await TestContext.CreateUserAsync(workspace, UserRole.StandardUser, TestContext.CancellationToken);

        // Act
        var workspaceToUpdate = new WorkspaceInputGql
        {
            Id = workspace.ReplicatedDocumentId,
            Name = Strings.CreateString(),
            IsDeleted = workspace.IsDeleted,
            UpdatedAt = DateTimeOffset.UtcNow,
            Topics = workspace.Topics?.Select(t => t.Name)
                .ToList(),
        };

        var workspaceInputPushRowGql = new WorkspaceInputPushRowGql
        {
            AssumedMasterState = new WorkspaceInputGql
            {
                Id = workspace.ReplicatedDocumentId,
                Name = workspace.Name,
                IsDeleted = workspace.IsDeleted,
                UpdatedAt = workspace.UpdatedAt,
                Topics = workspace.Topics?.Select(t => t.Name)
                    .ToList(),
            },
            NewDocumentState = workspaceToUpdate,
        };

        var pushWorkspaceInputGql = new PushWorkspaceInputGql
        {
            WorkspacePushRow = new List<WorkspaceInputPushRowGql?>
            {
                workspaceInputPushRowGql,
            },
        };

        var updateWorkspace =
            new MutationQueryBuilderGql().WithPushWorkspace(new PushWorkspacePayloadQueryBuilderGql().WithAllFields(), pushWorkspaceInputGql);

        var response = await TestContext.HttpClient.PostGqlMutationAsync(updateWorkspace, TestContext.CancellationToken, standardUser.JwtAccessToken);

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
    public async Task ASystemAdminShouldBeAbleToDeleteAWorkspace()
    {
        // Arrange
        TestContext = TestSetupUtil.SetupWithDefaultsAndCustomConfig(setupAuthorization: true,
            configureWorkspaceSecurity: options => options.RequirePolicyToDelete("IsSystemAdmin"));

        var workspace = await TestContext.CreateWorkspaceAsync(TestContext.CancellationToken);
        var systemAdmin = await TestContext.CreateUserAsync(workspace, UserRole.SystemAdmin, TestContext.CancellationToken);

        // Act
        var workspaceToDelete = new WorkspaceInputGql
        {
            Id = workspace.ReplicatedDocumentId,
            Name = workspace.Name,
            IsDeleted = true,
            UpdatedAt = DateTimeOffset.UtcNow,
            Topics = workspace.Topics?.Select(t => t.Name)
                .ToList(),
        };

        var workspaceInputPushRowGql = new WorkspaceInputPushRowGql
        {
            AssumedMasterState = new WorkspaceInputGql
            {
                Id = workspace.ReplicatedDocumentId,
                Name = workspace.Name,
                IsDeleted = workspace.IsDeleted,
                UpdatedAt = workspace.UpdatedAt,
                Topics = workspace.Topics?.Select(t => t.Name)
                    .ToList(),
            },
            NewDocumentState = workspaceToDelete,
        };

        var pushWorkspaceInputGql = new PushWorkspaceInputGql
        {
            WorkspacePushRow = new List<WorkspaceInputPushRowGql?>
            {
                workspaceInputPushRowGql,
            },
        };

        var deleteWorkspace =
            new MutationQueryBuilderGql().WithPushWorkspace(new PushWorkspacePayloadQueryBuilderGql().WithAllFields(), pushWorkspaceInputGql);

        var response = await TestContext.HttpClient.PostGqlMutationAsync(deleteWorkspace, TestContext.CancellationToken, systemAdmin.JwtAccessToken);

        // Assert
        response.Errors.Should()
            .BeNullOrEmpty();
        response.Data.PushWorkspace?.Workspace.Should()
            .BeNullOrEmpty();

        var deletedWorkspace = await TestContext.HttpClient.GetWorkspaceByIdAsync(workspace.ReplicatedDocumentId, TestContext.CancellationToken);
        deletedWorkspace.IsDeleted.Should()
            .BeTrue();
    }

    [Fact]
    public async Task AWorkspaceAdminShouldNotBeAbleToDeleteAWorkspaceWhenSystemAdminIsRequired()
    {
        // Arrange
        TestContext = TestSetupUtil.SetupWithDefaultsAndCustomConfig(setupAuthorization: true,
            configureWorkspaceSecurity: options => options.RequirePolicyToDelete("IsSystemAdmin"));

        var workspace = await TestContext.CreateWorkspaceAsync(TestContext.CancellationToken);
        var workspaceAdmin = await TestContext.CreateUserAsync(workspace, UserRole.WorkspaceAdmin, TestContext.CancellationToken);

        // Act
        var deletedWorkspace = new WorkspaceInputGql
        {
            Id = workspace.ReplicatedDocumentId,
            Name = workspace.Name,
            IsDeleted = true,
            UpdatedAt = DateTimeOffset.UtcNow,
            Topics = workspace.Topics?.Select(t => t.Name)
                .ToList(),
        };

        var workspaceInputPushRowGql = new WorkspaceInputPushRowGql
        {
            AssumedMasterState = new WorkspaceInputGql
            {
                Id = workspace.ReplicatedDocumentId,
                Name = workspace.Name,
                IsDeleted = workspace.IsDeleted,
                UpdatedAt = workspace.UpdatedAt,
                Topics = workspace.Topics?.Select(t => t.Name)
                    .ToList(),
            },
            NewDocumentState = deletedWorkspace,
        };

        var pushWorkspaceInputGql = new PushWorkspaceInputGql
        {
            WorkspacePushRow = new List<WorkspaceInputPushRowGql?>
            {
                workspaceInputPushRowGql,
            },
        };

        var deleteWorkspace =
            new MutationQueryBuilderGql().WithPushWorkspace(new PushWorkspacePayloadQueryBuilderGql().WithAllFields(), pushWorkspaceInputGql);

        var response = await TestContext.HttpClient.PostGqlMutationAsync(deleteWorkspace, TestContext.CancellationToken,
            workspaceAdmin.JwtAccessToken);

        // Assert
        response.Data.PushWorkspace?.Workspace.Should()
            .BeNullOrEmpty();
        response.Data.PushWorkspace?.Errors.Should()
            .HaveCount(1);
        response.Data.PushWorkspace?.Errors?.Single()
            .Should()
            .BeOfType<UnauthorizedAccessErrorGql>();
    }
}
