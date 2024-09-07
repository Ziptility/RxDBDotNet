using RxDBDotNet.Tests.Model;
using RxDBDotNet.Tests.Utils;

namespace RxDBDotNet.Tests;

[Collection("DockerSetup")]
public class PushDocumentsTests : IAsyncLifetime
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
    public async Task PushDocuments_WithNullDocumentList_ShouldReturnEmptyResult()
    {
        // Arrange
        TestContext = TestSetupUtil.Setup();
        var pushWorkspaceInputGql = new PushWorkspaceInputGql
        {
            WorkspacePushRow = null,
        };

        var pushWorkspaceMutation =
            new MutationQueryBuilderGql().WithPushWorkspace(new PushWorkspacePayloadQueryBuilderGql().WithAllFields(), pushWorkspaceInputGql);

        // Act
        var response = await TestContext.HttpClient.PostGqlMutationAsync(pushWorkspaceMutation, TestContext.CancellationToken);

        // Assert
        // When pushing a null document list, the system should return an empty result without any errors
        response.Errors.Should()
            .BeNullOrEmpty();
        response.Data.PushWorkspace.Should()
            .NotBeNull();
        response.Data.PushWorkspace?.Workspace.Should()
            .BeNullOrEmpty();
        response.Data.PushWorkspace?.Errors.Should()
            .BeNullOrEmpty();
    }

    [Fact]
    public async Task PushDocuments_WithListContainingNullDocument_ShouldSkipNullAndProcessValid()
    {
        // Arrange
        TestContext = TestSetupUtil.Setup();
        var (existingWorkspace, _) = await TestContext.HttpClient.CreateWorkspaceAsync(TestContext.CancellationToken);

        var updatedWorkspace = new WorkspaceInputGql
        {
            Id = existingWorkspace.Id,
            Name = Strings.CreateString(),
            UpdatedAt = DateTimeOffset.UtcNow,
            IsDeleted = existingWorkspace.IsDeleted,
            Topics = existingWorkspace.Topics,
        };

        var pushWorkspaceInputGql = new PushWorkspaceInputGql
        {
            WorkspacePushRow = new List<WorkspaceInputPushRowGql?>
            {
                null,
                new()
                {
                    AssumedMasterState = existingWorkspace,
                    NewDocumentState = updatedWorkspace,
                },
            },
        };

        var pushWorkspaceMutation =
            new MutationQueryBuilderGql().WithPushWorkspace(new PushWorkspacePayloadQueryBuilderGql().WithAllFields(), pushWorkspaceInputGql);

        // Act
        var response = await TestContext.HttpClient.PostGqlMutationAsync(pushWorkspaceMutation, TestContext.CancellationToken);

        // Assert
        // When pushing a list containing a null document, the system should skip the null entry and process the valid ones
        response.Errors.Should()
            .BeNullOrEmpty();
        response.Data.PushWorkspace.Should()
            .NotBeNull();
        response.Data.PushWorkspace?.Workspace.Should()
            .BeNullOrEmpty();
        response.Data.PushWorkspace?.Errors.Should()
            .BeNullOrEmpty();

        // Verify the valid document was processed
        var updatedWorkspaceFromDb = await TestContext.HttpClient.GetWorkspaceByIdAsync(existingWorkspace.Id!.Value, TestContext.CancellationToken);
        updatedWorkspaceFromDb.Should()
            .NotBeNull();
        updatedWorkspaceFromDb.Name.Should()
            .Be(updatedWorkspace.Name.Value);
    }

    [Fact]
    public async Task PushDocuments_WithNewDocument_ShouldCreateDocument()
    {
        // Arrange
        TestContext = TestSetupUtil.Setup();
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

        var pushWorkspaceMutation =
            new MutationQueryBuilderGql().WithPushWorkspace(new PushWorkspacePayloadQueryBuilderGql().WithAllFields(), pushWorkspaceInputGql);

        // Act
        var response = await TestContext.HttpClient.PostGqlMutationAsync(pushWorkspaceMutation, TestContext.CancellationToken);

        // Assert
        response.Errors.Should()
            .BeNullOrEmpty();
        response.Data.PushWorkspace.Should()
            .NotBeNull();
        response.Data.PushWorkspace?.Workspace.Should()
            .BeNullOrEmpty();
        response.Data.PushWorkspace?.Errors.Should()
            .BeNullOrEmpty();

        // Verify the document was created
        var createdWorkspace = await TestContext.HttpClient.GetWorkspaceByIdAsync(workspaceId, TestContext.CancellationToken);
        createdWorkspace.Should()
            .NotBeNull();
        createdWorkspace.Id.Should()
            .Be(workspaceId);
        createdWorkspace.Name.Should()
            .Be(newWorkspace.Name.Value);
        createdWorkspace.UpdatedAt.Should()
            .BeCloseTo(newWorkspace.UpdatedAt!.Value, TimeSpan.FromSeconds(1));
        createdWorkspace.IsDeleted.Should()
            .Be(newWorkspace.IsDeleted);
    }

    [Fact]
    public async Task PushDocuments_WithExistingDocument_ShouldUpdateDocument()
    {
        // Arrange
        TestContext = TestSetupUtil.Setup();
        var (existingWorkspace, _) = await TestContext.HttpClient.CreateWorkspaceAsync(TestContext.CancellationToken);

        var updatedWorkspace = new WorkspaceInputGql
        {
            Id = existingWorkspace.Id,
            Name = Strings.CreateString(),
            UpdatedAt = DateTimeOffset.UtcNow,
            IsDeleted = existingWorkspace.IsDeleted,
            Topics = existingWorkspace.Topics,
        };

        var workspaceInputPushRowGql = new WorkspaceInputPushRowGql
        {
            AssumedMasterState = existingWorkspace,
            NewDocumentState = updatedWorkspace,
        };

        var pushWorkspaceInputGql = new PushWorkspaceInputGql
        {
            WorkspacePushRow = new List<WorkspaceInputPushRowGql?>
            {
                workspaceInputPushRowGql,
            },
        };

        var pushWorkspaceMutation =
            new MutationQueryBuilderGql().WithPushWorkspace(new PushWorkspacePayloadQueryBuilderGql().WithAllFields(), pushWorkspaceInputGql);

        // Act
        var response = await TestContext.HttpClient.PostGqlMutationAsync(pushWorkspaceMutation, TestContext.CancellationToken);

        // Assert
        response.Errors.Should()
            .BeNullOrEmpty();
        response.Data.PushWorkspace.Should()
            .NotBeNull();
        response.Data.PushWorkspace?.Workspace.Should()
            .BeNullOrEmpty();
        response.Data.PushWorkspace?.Errors.Should()
            .BeNullOrEmpty();

        // Verify the document was updated
        var updatedWorkspaceFromDb = await TestContext.HttpClient.GetWorkspaceByIdAsync(existingWorkspace.Id!.Value, TestContext.CancellationToken);
        updatedWorkspaceFromDb.Should()
            .NotBeNull();
        updatedWorkspaceFromDb.Id.Should()
            .Be(existingWorkspace.Id);
        updatedWorkspaceFromDb.Name.Should()
            .Be(updatedWorkspace.Name.Value);
        updatedWorkspaceFromDb.UpdatedAt.Should()
            .BeCloseTo(updatedWorkspace.UpdatedAt!.Value, TimeSpan.FromSeconds(1));
        updatedWorkspaceFromDb.IsDeleted.Should()
            .Be(updatedWorkspace.IsDeleted);
    }

    [Fact]
    public async Task PushDocuments_WithConflict_ShouldReturnConflict()
    {
        // Arrange
        TestContext = TestSetupUtil.Setup();
        var (existingWorkspace, _) = await TestContext.HttpClient.CreateWorkspaceAsync(TestContext.CancellationToken);

        // Simulate a conflict by changing the workspace on the server
        var conflictingWorkspace = await TestContext.HttpClient.UpdateWorkspaceAsync(existingWorkspace, TestContext.CancellationToken);

        var updatedWorkspace = new WorkspaceInputGql
        {
            Id = existingWorkspace.Id,
            Name = Strings.CreateString(),
            UpdatedAt = DateTimeOffset.UtcNow,
            IsDeleted = existingWorkspace.IsDeleted,
            Topics = existingWorkspace.Topics,
        };

        var workspaceInputPushRowGql = new WorkspaceInputPushRowGql
        {
            AssumedMasterState = existingWorkspace,
            NewDocumentState = updatedWorkspace,
        };

        var pushWorkspaceInputGql = new PushWorkspaceInputGql
        {
            WorkspacePushRow = new List<WorkspaceInputPushRowGql?>
            {
                workspaceInputPushRowGql,
            },
        };

        var pushWorkspaceMutation =
            new MutationQueryBuilderGql().WithPushWorkspace(new PushWorkspacePayloadQueryBuilderGql().WithAllFields(), pushWorkspaceInputGql);

        // Act
        var response = await TestContext.HttpClient.PostGqlMutationAsync(pushWorkspaceMutation, TestContext.CancellationToken);

        // Assert
        response.Errors.Should()
            .BeNullOrEmpty();
        response.Data.PushWorkspace.Should()
            .NotBeNull();
        response.Data.PushWorkspace?.Workspace.Should()
            .HaveCount(1);
        response.Data.PushWorkspace?.Workspace?.First()
            .Id.Should()
            .Be(conflictingWorkspace.Id);
        response.Data.PushWorkspace?.Workspace?.First()
            .Name.Should()
            .Be(conflictingWorkspace.Name);
        response.Data.PushWorkspace?.Errors.Should()
            .BeNullOrEmpty();
    }

    [Fact]
    public async Task PushDocuments_WithMultipleDocuments_ShouldHandleAllDocuments()
    {
        // Arrange
        TestContext = TestSetupUtil.Setup();
        var (existingWorkspace1, _) = await TestContext.HttpClient.CreateWorkspaceAsync(TestContext.CancellationToken);
        var (existingWorkspace2, _) = await TestContext.HttpClient.CreateWorkspaceAsync(TestContext.CancellationToken);

        var newWorkspace = new WorkspaceInputGql
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

        var updatedWorkspace1 = new WorkspaceInputGql
        {
            Id = existingWorkspace1.Id,
            Name = Strings.CreateString(),
            UpdatedAt = DateTimeOffset.UtcNow,
            IsDeleted = existingWorkspace1.IsDeleted,
            Topics = existingWorkspace1.Topics,
        };

        var updatedWorkspace2 = new WorkspaceInputGql
        {
            Id = existingWorkspace2.Id,
            Name = Strings.CreateString(),
            UpdatedAt = DateTimeOffset.UtcNow,
            IsDeleted = existingWorkspace2.IsDeleted,
            Topics = existingWorkspace2.Topics,
        };

        var pushWorkspaceInputGql = new PushWorkspaceInputGql
        {
            WorkspacePushRow = new List<WorkspaceInputPushRowGql?>
            {
                new()
                {
                    AssumedMasterState = null,
                    NewDocumentState = newWorkspace,
                },
                new()
                {
                    AssumedMasterState = existingWorkspace1,
                    NewDocumentState = updatedWorkspace1,
                },
                new()
                {
                    AssumedMasterState = existingWorkspace2,
                    NewDocumentState = updatedWorkspace2,
                },
            },
        };

        var pushWorkspaceMutation =
            new MutationQueryBuilderGql().WithPushWorkspace(new PushWorkspacePayloadQueryBuilderGql().WithAllFields(), pushWorkspaceInputGql);

        // Act
        var response = await TestContext.HttpClient.PostGqlMutationAsync(pushWorkspaceMutation, TestContext.CancellationToken);

        // Assert
        response.Errors.Should()
            .BeNullOrEmpty();
        response.Data.PushWorkspace.Should()
            .NotBeNull();
        response.Data.PushWorkspace?.Workspace.Should()
            .BeNullOrEmpty();
        response.Data.PushWorkspace?.Errors.Should()
            .BeNullOrEmpty();

        // Verify all documents were created/updated
        var createdWorkspace = await TestContext.HttpClient.GetWorkspaceByIdAsync(newWorkspace.Id!.Value, TestContext.CancellationToken);
        createdWorkspace.Should()
            .NotBeNull();
        createdWorkspace.Name.Should()
            .Be(newWorkspace.Name.Value);

        var updatedWorkspace1FromDb = await TestContext.HttpClient.GetWorkspaceByIdAsync(existingWorkspace1.Id!.Value, TestContext.CancellationToken);
        updatedWorkspace1FromDb.Should()
            .NotBeNull();
        updatedWorkspace1FromDb.Name.Should()
            .Be(updatedWorkspace1.Name.Value);

        var updatedWorkspace2FromDb = await TestContext.HttpClient.GetWorkspaceByIdAsync(existingWorkspace2.Id!.Value, TestContext.CancellationToken);
        updatedWorkspace2FromDb.Should()
            .NotBeNull();
        updatedWorkspace2FromDb.Name.Should()
            .Be(updatedWorkspace2.Name.Value);
    }

    [Fact]
    public async Task PushDocuments_WithDeletedDocument_ShouldMarkDocumentAsDeleted()
    {
        // Arrange
        TestContext = TestSetupUtil.Setup();
        var (existingWorkspace, _) = await TestContext.HttpClient.CreateWorkspaceAsync(TestContext.CancellationToken);

        var deletedWorkspace = new WorkspaceInputGql
        {
            Id = existingWorkspace.Id,
            Name = existingWorkspace.Name,
            UpdatedAt = DateTimeOffset.UtcNow,
            IsDeleted = true,
            Topics = existingWorkspace.Topics,
        };

        var workspaceInputPushRowGql = new WorkspaceInputPushRowGql
        {
            AssumedMasterState = existingWorkspace,
            NewDocumentState = deletedWorkspace,
        };

        var pushWorkspaceInputGql = new PushWorkspaceInputGql
        {
            WorkspacePushRow = new List<WorkspaceInputPushRowGql?>
            {
                workspaceInputPushRowGql,
            },
        };

        var pushWorkspaceMutation =
            new MutationQueryBuilderGql().WithPushWorkspace(new PushWorkspacePayloadQueryBuilderGql().WithAllFields(), pushWorkspaceInputGql);

        // Act
        var response = await TestContext.HttpClient.PostGqlMutationAsync(pushWorkspaceMutation, TestContext.CancellationToken);

        // Assert
        response.Errors.Should()
            .BeNullOrEmpty();
        response.Data.PushWorkspace.Should()
            .NotBeNull();
        response.Data.PushWorkspace?.Workspace.Should()
            .BeNullOrEmpty();
        response.Data.PushWorkspace?.Errors.Should()
            .BeNullOrEmpty();

        // Verify the document was marked as deleted
        var deletedWorkspaceFromDb = await TestContext.HttpClient.GetWorkspaceByIdAsync(existingWorkspace.Id!.Value, TestContext.CancellationToken);
        deletedWorkspaceFromDb.Should()
            .NotBeNull();
        deletedWorkspaceFromDb.IsDeleted.Should()
            .BeTrue();
    }

    [Fact]
    public async Task PushDocuments_WithNonExistentAssumedMasterState_ShouldHandleConflict()
    {
        // Arrange
        TestContext = TestSetupUtil.Setup();
        var nonExistentWorkspace = new WorkspaceInputGql
        {
            Id = Provider.Sql.Create(),
            Name = Strings.CreateString(),
            UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1),
            IsDeleted = false,
            Topics = new List<string>
            {
                Provider.Sql.Create()
                    .ToString(),
            },
        };

        var newWorkspaceState = new WorkspaceInputGql
        {
            Id = nonExistentWorkspace.Id,
            Name = Strings.CreateString(),
            UpdatedAt = DateTimeOffset.UtcNow,
            IsDeleted = false,
            Topics = nonExistentWorkspace.Topics,
        };

        var workspaceInputPushRowGql = new WorkspaceInputPushRowGql
        {
            AssumedMasterState = nonExistentWorkspace,
            NewDocumentState = newWorkspaceState,
        };

        var pushWorkspaceInputGql = new PushWorkspaceInputGql
        {
            WorkspacePushRow = new List<WorkspaceInputPushRowGql?>
            {
                workspaceInputPushRowGql,
            },
        };

        var pushWorkspaceMutation =
            new MutationQueryBuilderGql().WithPushWorkspace(new PushWorkspacePayloadQueryBuilderGql().WithAllFields(), pushWorkspaceInputGql);

        // Act
        var response = await TestContext.HttpClient.PostGqlMutationAsync(pushWorkspaceMutation, TestContext.CancellationToken);

        // Assert
        // When pushing a document with a non-existent assumed master state,
        // the system treats this as a conflict and returns the assumed state
        // without creating a new document
        response.Errors.Should()
            .BeNullOrEmpty();
        response.Data.PushWorkspace.Should()
            .NotBeNull();
        response.Data.PushWorkspace?.Workspace.Should()
            .NotBeNullOrEmpty();
        response.Data.PushWorkspace?.Workspace.Should()
            .HaveCount(1);
        response.Data.PushWorkspace?.Errors.Should()
            .BeNullOrEmpty();

        var returnedWorkspace = response.Data.PushWorkspace?.Workspace?.First();
        returnedWorkspace.Should()
            .NotBeNull();
        returnedWorkspace?.Id.Should()
            .Be(nonExistentWorkspace.Id);
        returnedWorkspace?.Name.Should()
            .Be(nonExistentWorkspace.Name);
        returnedWorkspace?.IsDeleted.Should()
            .Be(nonExistentWorkspace.IsDeleted);

        // Verify the document was not created
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await TestContext.HttpClient.GetWorkspaceByIdAsync(nonExistentWorkspace.Id!.Value, TestContext.CancellationToken));
    }

    [Fact]
    public async Task PushDocuments_WithEmptyList_ShouldReturnEmptyResult()
    {
        // Arrange
        TestContext = TestSetupUtil.Setup();
        var pushWorkspaceInputGql = new PushWorkspaceInputGql
        {
            WorkspacePushRow = new List<WorkspaceInputPushRowGql?>(),
        };

        var pushWorkspaceMutation =
            new MutationQueryBuilderGql().WithPushWorkspace(new PushWorkspacePayloadQueryBuilderGql().WithAllFields(), pushWorkspaceInputGql);

        // Act
        var response = await TestContext.HttpClient.PostGqlMutationAsync(pushWorkspaceMutation, TestContext.CancellationToken);

        // Assert
        response.Errors.Should()
            .BeNullOrEmpty();
        response.Data.PushWorkspace.Should()
            .NotBeNull();
        response.Data.PushWorkspace?.Workspace.Should()
            .BeNullOrEmpty();
        response.Data.PushWorkspace?.Errors.Should()
            .BeNullOrEmpty();
    }

    [Fact]
    public async Task PushDocuments_WithMixOfNewAndExistingDocuments_ShouldHandleCorrectly()
    {
        // Arrange
        TestContext = TestSetupUtil.Setup();
        var (existingWorkspace, _) = await TestContext.HttpClient.CreateWorkspaceAsync(TestContext.CancellationToken);

        var newWorkspace = new WorkspaceInputGql
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

        var updatedExistingWorkspace = new WorkspaceInputGql
        {
            Id = existingWorkspace.Id,
            Name = Strings.CreateString(),
            UpdatedAt = DateTimeOffset.UtcNow,
            IsDeleted = existingWorkspace.IsDeleted,
            Topics = existingWorkspace.Topics,
        };

        var pushWorkspaceInputGql = new PushWorkspaceInputGql
        {
            WorkspacePushRow = new List<WorkspaceInputPushRowGql?>
            {
                new()
                {
                    AssumedMasterState = null,
                    NewDocumentState = newWorkspace,
                },
                new()
                {
                    AssumedMasterState = existingWorkspace,
                    NewDocumentState = updatedExistingWorkspace,
                },
            },
        };

        var pushWorkspaceMutation =
            new MutationQueryBuilderGql().WithPushWorkspace(new PushWorkspacePayloadQueryBuilderGql().WithAllFields(), pushWorkspaceInputGql);

        // Act
        var response = await TestContext.HttpClient.PostGqlMutationAsync(pushWorkspaceMutation, TestContext.CancellationToken);

        // Assert
        response.Errors.Should()
            .BeNullOrEmpty();
        response.Data.PushWorkspace.Should()
            .NotBeNull();
        response.Data.PushWorkspace?.Workspace.Should()
            .BeNullOrEmpty();
        response.Data.PushWorkspace?.Errors.Should()
            .BeNullOrEmpty();

        // Verify new document was created
        var createdWorkspace = await TestContext.HttpClient.GetWorkspaceByIdAsync(newWorkspace.Id!.Value, TestContext.CancellationToken);
        createdWorkspace.Should()
            .NotBeNull();
        createdWorkspace.Name.Should()
            .Be(newWorkspace.Name.Value);

        // Verify existing document was updated
        var updatedWorkspace = await TestContext.HttpClient.GetWorkspaceByIdAsync(existingWorkspace.Id!.Value, TestContext.CancellationToken);
        updatedWorkspace.Should()
            .NotBeNull();
        updatedWorkspace.Name.Should()
            .Be(updatedExistingWorkspace.Name.Value);
    }

    [Fact]
    public async Task PushDocuments_WithConflictInBatch_ShouldHandleCorrectly()
    {
        // Arrange
        TestContext = TestSetupUtil.Setup();
        var (existingWorkspace1, _) = await TestContext.HttpClient.CreateWorkspaceAsync(TestContext.CancellationToken);
        var (existingWorkspace2, _) = await TestContext.HttpClient.CreateWorkspaceAsync(TestContext.CancellationToken);

        // Simulate a conflict by updating existingWorkspace1
        var conflictingWorkspace = await TestContext.HttpClient.UpdateWorkspaceAsync(existingWorkspace1, TestContext.CancellationToken);

        var updatedWorkspace1 = new WorkspaceInputGql
        {
            Id = existingWorkspace1.Id,
            Name = Strings.CreateString(),
            UpdatedAt = DateTimeOffset.UtcNow,
            IsDeleted = existingWorkspace1.IsDeleted,
            Topics = existingWorkspace1.Topics,
        };

        var updatedWorkspace2 = new WorkspaceInputGql
        {
            Id = existingWorkspace2.Id,
            Name = Strings.CreateString(),
            UpdatedAt = DateTimeOffset.UtcNow,
            IsDeleted = existingWorkspace2.IsDeleted,
            Topics = existingWorkspace2.Topics,
        };

        var pushWorkspaceInputGql = new PushWorkspaceInputGql
        {
            WorkspacePushRow = new List<WorkspaceInputPushRowGql?>
            {
                new()
                {
                    AssumedMasterState = existingWorkspace1,
                    NewDocumentState = updatedWorkspace1,
                },
                new()
                {
                    AssumedMasterState = existingWorkspace2,
                    NewDocumentState = updatedWorkspace2,
                },
            },
        };

        var pushWorkspaceMutation =
            new MutationQueryBuilderGql().WithPushWorkspace(new PushWorkspacePayloadQueryBuilderGql().WithAllFields(), pushWorkspaceInputGql);

        // Act
        var response = await TestContext.HttpClient.PostGqlMutationAsync(pushWorkspaceMutation, TestContext.CancellationToken);

        // Assert
        response.Errors.Should()
            .BeNullOrEmpty();
        response.Data.PushWorkspace.Should()
            .NotBeNull();
        response.Data.PushWorkspace?.Workspace.Should()
            .HaveCount(1);
        response.Data.PushWorkspace?.Workspace?.First()
            .Id.Should()
            .Be(conflictingWorkspace.Id);
        response.Data.PushWorkspace?.Errors.Should()
            .BeNullOrEmpty();

        // When a conflict is detected in a batch, the entire batch is rejected to maintain consistency

        // Verify existingWorkspace1 was not updated due to conflict
        var workspace1FromDb = await TestContext.HttpClient.GetWorkspaceByIdAsync(existingWorkspace1.Id!.Value, TestContext.CancellationToken);
        workspace1FromDb.Should()
            .NotBeNull();
        workspace1FromDb.Name.Should()
            .Be(conflictingWorkspace.Name);

        // Verify existingWorkspace2 was not updated due to the conflict in the batch
        var workspace2FromDb = await TestContext.HttpClient.GetWorkspaceByIdAsync(existingWorkspace2.Id!.Value, TestContext.CancellationToken);
        workspace2FromDb.Should()
            .NotBeNull();
        workspace2FromDb.Name.Should()
            .Be(existingWorkspace2.Name!.Value);
    }
}
