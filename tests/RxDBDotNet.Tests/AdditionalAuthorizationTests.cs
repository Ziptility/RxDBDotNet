// tests\RxDBDotNet.Tests\AdditionalAuthorizationTests.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using LiveDocs.GraphQLApi.Models.Replication;
using LiveDocs.GraphQLApi.Security;
using RT.Comb;
using RxDBDotNet.Tests.Model;
using RxDBDotNet.Tests.Setup;
using RxDBDotNet.Tests.Utils;

namespace RxDBDotNet.Tests;

[Collection("DockerSetup")]
public class AdditionalAuthorizationTests : IAsyncLifetime
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
    public async Task UnauthenticatedUser_ShouldNotBeAbleToReadWorkspace()
    {
        // Arrange
        TestContext = new TestScenarioBuilder()
            .WithAuthorization()
            .ConfigureReplicatedDocument<ReplicatedWorkspace>(options => options.Security.RequirePolicyToRead("IsWorkspaceAdmin"))
            .Build();

        await TestContext.CreateWorkspaceAsync(TestContext.CancellationToken);

        // Act
        var query = new QueryQueryBuilderGql().WithPullWorkspace(new WorkspacePullBulkQueryBuilderGql().WithAllFields(), 1000);
        var response = await TestContext.HttpClient.PostGqlQueryAsync(query, TestContext.CancellationToken);

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
    public async Task StandardUser_ShouldNotBeAbleToDeleteWorkspace()
    {
        // Arrange
        TestContext = new TestScenarioBuilder()
            .WithAuthorization()
            .ConfigureReplicatedDocument<ReplicatedWorkspace>(options => options.Security.RequirePolicyToDelete("IsSystemAdmin"))
            .Build();

        var workspace = await TestContext.CreateWorkspaceAsync(TestContext.CancellationToken);
        var standardUser = await TestContext.CreateUserAsync(workspace, UserRole.StandardUser, TestContext.CancellationToken);

        // Act
        var workspaceToDelete = new WorkspaceInputGql
        {
            Id = workspace.ReplicatedDocumentId,
            Name = workspace.Name,
            IsDeleted = true,
            UpdatedAt = DateTimeOffset.UtcNow,
            Topics = workspace.Topics.ConvertAll(t => t.Name)
,
        };

        var workspaceInputPushRowGql = new WorkspaceInputPushRowGql
        {
            AssumedMasterState = new WorkspaceInputGql
            {
                Id = workspace.ReplicatedDocumentId,
                Name = workspace.Name,
                IsDeleted = workspace.IsDeleted,
                UpdatedAt = workspace.UpdatedAt,
                Topics = workspace.Topics.ConvertAll(t => t.Name)
,
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

        var response = await TestContext.HttpClient.PostGqlMutationAsync(deleteWorkspace, TestContext.CancellationToken, standardUser.JwtAccessToken);

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
    public async Task WorkspaceAdmin_ShouldBeAbleToReadAndUpdateWorkspace()
    {
        // Arrange
        TestContext = new TestScenarioBuilder()
            .WithAuthorization()
            .ConfigureReplicatedDocument<ReplicatedWorkspace>(options => options.Security
                .RequirePolicyToRead("IsWorkspaceAdmin")
                .RequirePolicyToUpdate("IsWorkspaceAdmin"))
            .Build();

        var workspace = await TestContext.CreateWorkspaceAsync(TestContext.CancellationToken);
        var workspaceAdmin = await TestContext.CreateUserAsync(workspace, UserRole.WorkspaceAdmin, TestContext.CancellationToken);

        // Act - Read
        var readQuery = new QueryQueryBuilderGql().WithPullWorkspace(new WorkspacePullBulkQueryBuilderGql().WithAllFields(), 1000);
        var readResponse = await TestContext.HttpClient.PostGqlQueryAsync(readQuery, TestContext.CancellationToken, workspaceAdmin.JwtAccessToken);

        // Assert - Read
        readResponse.Errors.Should()
            .BeNullOrEmpty();
        readResponse.Data.PullWorkspace.Should()
            .NotBeNull();
        readResponse.Data.PullWorkspace?.Documents.Should()
            .NotBeEmpty();

        // Act - Update
        var updatedWorkspace = new WorkspaceInputGql
        {
            Id = workspace.ReplicatedDocumentId,
            Name = Strings.CreateString(),
            IsDeleted = workspace.IsDeleted,
            UpdatedAt = DateTimeOffset.UtcNow,
            Topics = workspace.Topics.ConvertAll(t => t.Name)
,
        };

        var workspaceInputPushRowGql = new WorkspaceInputPushRowGql
        {
            AssumedMasterState = new WorkspaceInputGql
            {
                Id = workspace.ReplicatedDocumentId,
                Name = workspace.Name,
                IsDeleted = workspace.IsDeleted,
                UpdatedAt = workspace.UpdatedAt,
                Topics = workspace.Topics.ConvertAll(t => t.Name)
,
            },
            NewDocumentState = updatedWorkspace,
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

        var updateResponse =
            await TestContext.HttpClient.PostGqlMutationAsync(updateWorkspace, TestContext.CancellationToken, workspaceAdmin.JwtAccessToken);

        // Assert - Update
        updateResponse.Errors.Should()
            .BeNullOrEmpty();
        updateResponse.Data.PushWorkspace?.Workspace.Should()
            .BeNullOrEmpty();
        updateResponse.Data.PushWorkspace?.Errors.Should()
            .BeNullOrEmpty();

        // Verify the update
        var verifyQuery = new QueryQueryBuilderGql().WithPullWorkspace(new WorkspacePullBulkQueryBuilderGql().WithAllFields(), 1000);
        var verifyResponse =
            await TestContext.HttpClient.PostGqlQueryAsync(verifyQuery, TestContext.CancellationToken, workspaceAdmin.JwtAccessToken);

        verifyResponse.Errors.Should()
            .BeNullOrEmpty();
        verifyResponse.Data.PullWorkspace?.Documents.Should()
            .Contain(w => w.Name == updatedWorkspace.Name.Value);
    }

    [Fact]
    public async Task SystemAdmin_ShouldHaveFullAccessToWorkspace()
    {
        // Arrange
        TestContext = new TestScenarioBuilder()
            .WithAuthorization()
            .ConfigureReplicatedDocument<ReplicatedWorkspace>(options => options.Security
                .RequirePolicyToRead("IsSystemAdmin")
                .RequirePolicyToCreate("IsSystemAdmin")
                .RequirePolicyToUpdate("IsSystemAdmin")
                .RequirePolicyToDelete("IsSystemAdmin"))
            .Build();

        var workspace = await TestContext.CreateWorkspaceAsync(TestContext.CancellationToken);
        var systemAdmin = await TestContext.CreateUserAsync(workspace, UserRole.SystemAdmin, TestContext.CancellationToken);

        // Act & Assert - Create
        var newWorkspaceInput = new WorkspaceInputGql
        {
            Id = Provider.Sql.Create(),
            Name = Strings.CreateString(),
            UpdatedAt = DateTimeOffset.UtcNow,
            IsDeleted = false,
            Topics = new List<string>
            {
                Provider.Sql.Create()
                    .ToString(),
            },
        };

        var createWorkspaceInputPushRowGql = new WorkspaceInputPushRowGql
        {
            AssumedMasterState = null,
            NewDocumentState = newWorkspaceInput,
        };

        var createWorkspaceInputGql = new PushWorkspaceInputGql
        {
            WorkspacePushRow = new List<WorkspaceInputPushRowGql?>
            {
                createWorkspaceInputPushRowGql,
            },
        };

        var createWorkspaceMutation =
            new MutationQueryBuilderGql().WithPushWorkspace(new PushWorkspacePayloadQueryBuilderGql().WithAllFields(), createWorkspaceInputGql);

        var createResponse =
            await TestContext.HttpClient.PostGqlMutationAsync(createWorkspaceMutation, TestContext.CancellationToken, systemAdmin.JwtAccessToken);

        createResponse.Errors.Should()
            .BeNullOrEmpty();
        createResponse.Data.PushWorkspace?.Workspace.Should()
            .BeNullOrEmpty();
        createResponse.Data.PushWorkspace?.Errors.Should()
            .BeNullOrEmpty();

        // Act & Assert - Read
        var readQuery = new QueryQueryBuilderGql().WithPullWorkspace(new WorkspacePullBulkQueryBuilderGql().WithAllFields(), 1000);
        var readResponse = await TestContext.HttpClient.PostGqlQueryAsync(readQuery, TestContext.CancellationToken, systemAdmin.JwtAccessToken);

        readResponse.Errors.Should()
            .BeNullOrEmpty();
        readResponse.Data.PullWorkspace.Should()
            .NotBeNull();
        readResponse.Data.PullWorkspace?.Documents.Should()
            .Contain(w => w.Name == newWorkspaceInput.Name.Value);

        var newWorkspace = await TestContext.HttpClient.GetWorkspaceByIdAsync(newWorkspaceInput.Id, TestContext.CancellationToken, systemAdmin.JwtAccessToken);

        // Act & Assert - Update
        var updatedWorkspaceInput = new WorkspaceInputGql
        {
            Id = newWorkspaceInput.Id,
            Name = Strings.CreateString(),
            IsDeleted = newWorkspaceInput.IsDeleted,
            UpdatedAt = DateTimeOffset.UtcNow,
            Topics = newWorkspaceInput.Topics,
        };

        var updateWorkspaceInputPushRowGql = new WorkspaceInputPushRowGql
        {
            AssumedMasterState = newWorkspace.ToWorkspaceInputGql(),
            NewDocumentState = updatedWorkspaceInput,
        };

        var updateWorkspaceInputGql = new PushWorkspaceInputGql
        {
            WorkspacePushRow = new List<WorkspaceInputPushRowGql?>
            {
                updateWorkspaceInputPushRowGql,
            },
        };

        var updateWorkspaceMutation =
            new MutationQueryBuilderGql().WithPushWorkspace(new PushWorkspacePayloadQueryBuilderGql().WithAllFields(), updateWorkspaceInputGql);

        var updateResponse =
            await TestContext.HttpClient.PostGqlMutationAsync(updateWorkspaceMutation, TestContext.CancellationToken, systemAdmin.JwtAccessToken);

        updateResponse.Errors.Should()
            .BeNullOrEmpty();
        updateResponse.Data.PushWorkspace?.Workspace.Should()
            .BeNullOrEmpty();
        updateResponse.Data.PushWorkspace?.Errors.Should()
            .BeNullOrEmpty();

        // Act & Assert - Delete
        var updatedWorkspace = await TestContext.HttpClient.GetWorkspaceByIdAsync(newWorkspaceInput.Id, TestContext.CancellationToken, systemAdmin.JwtAccessToken);

        var deleteWorkspace = new WorkspaceInputGql
        {
            Id = updatedWorkspaceInput.Id,
            Name = updatedWorkspaceInput.Name,
            IsDeleted = true,
            UpdatedAt = DateTimeOffset.UtcNow,
            Topics = updatedWorkspaceInput.Topics,
        };

        var deleteWorkspaceInputPushRowGql = new WorkspaceInputPushRowGql
        {
            AssumedMasterState = updatedWorkspace.ToWorkspaceInputGql(),
            NewDocumentState = deleteWorkspace,
        };

        var deleteWorkspaceInputGql = new PushWorkspaceInputGql
        {
            WorkspacePushRow = new List<WorkspaceInputPushRowGql?>
            {
                deleteWorkspaceInputPushRowGql,
            },
        };

        var deleteWorkspaceMutation =
            new MutationQueryBuilderGql().WithPushWorkspace(new PushWorkspacePayloadQueryBuilderGql().WithAllFields(), deleteWorkspaceInputGql);

        var deleteResponse =
            await TestContext.HttpClient.PostGqlMutationAsync(deleteWorkspaceMutation, TestContext.CancellationToken, systemAdmin.JwtAccessToken);

        deleteResponse.Errors.Should()
            .BeNullOrEmpty();
        deleteResponse.Data.PushWorkspace?.Workspace.Should()
            .BeNullOrEmpty();
        deleteResponse.Data.PushWorkspace?.Errors.Should()
            .BeNullOrEmpty();

        // Verify deletion
        var verifyQuery = new QueryQueryBuilderGql().WithPullWorkspace(new WorkspacePullBulkQueryBuilderGql().WithAllFields(), 1000);
        var verifyResponse = await TestContext.HttpClient.PostGqlQueryAsync(verifyQuery, TestContext.CancellationToken, systemAdmin.JwtAccessToken);

        verifyResponse.Errors.Should()
            .BeNullOrEmpty();
        verifyResponse.Data.PullWorkspace?.Documents.Should()
            .Contain(w => w.Id == deleteWorkspace.Id.Value && w.IsDeleted);
    }
}
