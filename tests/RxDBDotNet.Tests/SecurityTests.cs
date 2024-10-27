// tests\RxDBDotNet.Tests\SecurityTests.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using LiveDocs.GraphQLApi.Models.Replication;
using LiveDocs.GraphQLApi.Security;
using RT.Comb;
using RxDBDotNet.Configuration;
using RxDBDotNet.Security;
using RxDBDotNet.Tests.Model;
using RxDBDotNet.Tests.Setup;
using RxDBDotNet.Tests.Utils;
using static RxDBDotNet.Tests.Setup.Strings;

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
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (TestContext != null)
        {
            await TestContext.DisposeAsync();
        }
    }

    [Fact]
    public void RequirePolicy_WithNoOperations_ShouldThrowArgumentException()
    {
        // Arrange
        var securityOptions = new DocumentSecurityOptions<ReplicatedWorkspace>();

        // Act & Assert
        var action = () => securityOptions.RequirePolicy(Operation.None, "SomePolicy");
        action.Should().Throw<ArgumentException>().WithMessage("At least one operation must be specified. (Parameter 'operations')");
    }

    [Fact]
    public void RequirePolicy_WithNullOrEmptyPolicy_ShouldThrowArgumentException()
    {
        // Arrange
        var securityOptions = new DocumentSecurityOptions<ReplicatedWorkspace>();

        // Act & Assert
        var actionWithNull = () => securityOptions.RequirePolicy(Operation.Read, null!);
        actionWithNull.Should().Throw<ArgumentException>().WithMessage("Policy cannot be null or whitespace. (Parameter 'policy')");

        var actionWithEmpty = () => securityOptions.RequirePolicy(Operation.Read, "");
        actionWithEmpty.Should().Throw<ArgumentException>().WithMessage("Policy cannot be null or whitespace. (Parameter 'policy')");

        var actionWithWhitespace = () => securityOptions.RequirePolicy(Operation.Read, "   ");
        actionWithWhitespace.Should().Throw<ArgumentException>().WithMessage("Policy cannot be null or whitespace. (Parameter 'policy')");
    }

    [Fact]
    public async Task AWorkspaceAdminShouldBeAbleToCreateAWorkspace()
    {
        // Arrange
        TestContext = new TestScenarioBuilder()
            .WithAuthorization()
            .ConfigureReplicatedDocument<ReplicatedWorkspace>(options => options.Security.RequirePolicyToCreate("IsWorkspaceAdmin"))
            .Build();

        var workspace = await TestContext.CreateWorkspaceAsync(TestContext.CancellationToken);
        var workspaceAdmin = await TestContext.CreateUserAsync(workspace, UserRole.WorkspaceAdmin, TestContext.CancellationToken);

        var workspaceId = Provider.Sql.Create();

        var workspaceInputGql = new WorkspaceInputGql
        {
            Id = workspaceId,
            Name = CreateString(),
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
            NewDocumentState = workspaceInputGql,
        };

        var pushWorkspaceInputGql = new PushWorkspaceInputGql
        {
            WorkspacePushRow = new List<WorkspaceInputPushRowGql?>
            {
                workspaceInputPushRowGql,
            },
        };

        var createWorkspace = new MutationQueryBuilderGql().WithPushWorkspace(new PushWorkspacePayloadQueryBuilderGql().WithAllFields()
            .WithErrors(new PushWorkspaceErrorQueryBuilderGql().WithAuthenticationErrorFragment(
                new AuthenticationErrorQueryBuilderGql().WithAllFields())), pushWorkspaceInputGql);

        // Act
        await TestContext.HttpClient.PostGqlMutationAsync(createWorkspace, TestContext.CancellationToken, workspaceAdmin.JwtAccessToken);

        // Assert
        await TestContext.HttpClient.VerifyWorkspaceAsync(workspaceInputGql, TestContext.CancellationToken);
    }

    [Fact]
    public async Task AStandardUserShouldNotBeAbleToCreateAWorkspace()
    {
        // Arrange
        TestContext = new TestScenarioBuilder()
            .WithAuthorization()
            .ConfigureReplicatedDocument<ReplicatedWorkspace>(options => options.Security.RequirePolicyToCreate("IsWorkspaceAdmin"))
            .Build();

        var workspace = await TestContext.CreateWorkspaceAsync(TestContext.CancellationToken);

        var standardUser = await TestContext.CreateUserAsync(workspace, UserRole.StandardUser, TestContext.CancellationToken);

        var workspaceId = Provider.Sql.Create();

        var newWorkspace = new WorkspaceInputGql
        {
            Id = workspaceId,
            Name = CreateString(),
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
            NewDocumentState = newWorkspace,
        };

        var pushWorkspaceInputGql = new PushWorkspaceInputGql
        {
            WorkspacePushRow = new List<WorkspaceInputPushRowGql?>
            {
                workspaceInputPushRowGql,
            },
        };

        var createWorkspace = new MutationQueryBuilderGql().WithPushWorkspace(new PushWorkspacePayloadQueryBuilderGql().WithAllFields()
            .WithErrors(new PushWorkspaceErrorQueryBuilderGql().WithAuthenticationErrorFragment(
                new AuthenticationErrorQueryBuilderGql().WithAllFields())), pushWorkspaceInputGql);

        // Act
        var response = await TestContext.HttpClient.PostGqlMutationAsync(
            createWorkspace,
            TestContext.CancellationToken,
            standardUser.JwtAccessToken);

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
        TestContext = new TestScenarioBuilder()
            .WithAuthorization()
            .ConfigureReplicatedDocument<ReplicatedWorkspace>(options => options.Security.RequirePolicyToRead("IsSystemAdmin"))
            .Build();

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
        TestContext = new TestScenarioBuilder()
            .WithAuthorization()
            .ConfigureReplicatedDocument<ReplicatedWorkspace>(options => options.Security.RequirePolicyToRead("IsSystemAdmin"))
            .Build();

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
        TestContext = new TestScenarioBuilder()
            .WithAuthorization()
            .ConfigureReplicatedDocument<ReplicatedWorkspace>(options => options.Security.RequirePolicyToUpdate("IsWorkspaceAdmin"))
            .Build();

        var workspace = await TestContext.CreateWorkspaceAsync(TestContext.CancellationToken);
        var workspaceAdmin = await TestContext.CreateUserAsync(workspace, UserRole.WorkspaceAdmin, TestContext.CancellationToken);

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

        var response = await TestContext.HttpClient.PostGqlMutationAsync(updateWorkspace, TestContext.CancellationToken, workspaceAdmin.JwtAccessToken);

        // Assert
        response.Errors.Should()
            .BeNullOrEmpty();
        response.Data.PushWorkspace?.Workspace.Should()
            .BeNullOrEmpty();

        var updatedWorkspace = await TestContext.HttpClient.GetWorkspaceByIdAsync(
            workspace.ReplicatedDocumentId,
            TestContext.CancellationToken,
            workspaceAdmin.JwtAccessToken);

        updatedWorkspace.Name.Should()
            .Be(workspaceToUpdate.Name.Value);
    }

    [Fact]
    public async Task AStandardUserShouldNotBeAbleToUpdateAWorkspace()
    {
        // Arrange
        TestContext = new TestScenarioBuilder()
            .WithAuthorization()
            .ConfigureReplicatedDocument<ReplicatedWorkspace>(options => options.Security.RequirePolicyToUpdate("IsWorkspaceAdmin"))
            .Build();

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
        TestContext = new TestScenarioBuilder()
            .WithAuthorization()
            .ConfigureReplicatedDocument<ReplicatedWorkspace>(options => options.Security.RequirePolicyToDelete("IsSystemAdmin"))
            .Build();

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

        var deletedWorkspace = await TestContext.HttpClient.GetWorkspaceByIdAsync(
            workspace.ReplicatedDocumentId,
            TestContext.CancellationToken,
            systemAdmin.JwtAccessToken);

        deletedWorkspace.IsDeleted.Should()
            .BeTrue();
    }

    [Fact]
    public async Task AWorkspaceAdminShouldNotBeAbleToDeleteAWorkspaceWhenSystemAdminIsRequired()
    {
        // Arrange
        TestContext = new TestScenarioBuilder()
            .WithAuthorization()
            .ConfigureReplicatedDocument<ReplicatedWorkspace>(options => options.Security.RequirePolicyToDelete("IsSystemAdmin"))
            .Build();

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
