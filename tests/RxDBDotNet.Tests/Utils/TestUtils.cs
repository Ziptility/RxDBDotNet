using System.Diagnostics;
using LiveDocs.GraphQLApi.Data;
using LiveDocs.GraphQLApi.Models.Entities;
using RxDBDotNet.Tests.Model;
using static RxDBDotNet.Tests.Setup.Strings;

namespace RxDBDotNet.Tests.Utils;

internal static class TestUtils
{
    public static async Task<Workspace> CreateWorkspaceAsync(this TestContext context, CancellationToken cancellationToken)
    {
        var dbContext = context.ServiceProvider.GetRequiredService<LiveDocsDbContext>();
        var workspace = new Workspace
        {
            Id = Provider.Sql.Create(),
            Name = CreateString(),
            Topics = [],
            UpdatedAt = DateTimeOffset.Now,
            ReplicatedDocumentId = Guid.NewGuid(),
            IsDeleted = false,
        };
        await dbContext.Workspaces.AddAsync(workspace, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return workspace;
    }

    public static async Task<User> CreateUserAsync(
        this TestContext context,
        Workspace workspace,
        UserRole role,
        CancellationToken cancellationToken)
    {
        var dbContext = context.ServiceProvider.GetRequiredService<LiveDocsDbContext>();
        var replicatedUser = new ReplicatedUser
        {
            Id = Guid.NewGuid(),
            FirstName = CreateString(),
            LastName = CreateString(),
            Email = $"{CreateString()}@example.com",
            JwtAccessToken = null,
            WorkspaceId = workspace.ReplicatedDocumentId,
            UpdatedAt = DateTimeOffset.UtcNow,
            IsDeleted = false,
        };

        var jwtToken = JwtUtil.GenerateJwtToken(replicatedUser, role);

        var user = new User
        {
            Id = Provider.Sql.Create(),
            FirstName = replicatedUser.FirstName,
            LastName = replicatedUser.LastName,
            Email = replicatedUser.Email,
            JwtAccessToken = jwtToken,
            WorkspaceId = workspace.Id,
            UpdatedAt = replicatedUser.UpdatedAt,
            IsDeleted = replicatedUser.IsDeleted,
            ReplicatedDocumentId = replicatedUser.Id,
        };

        await dbContext.Users.AddAsync(user, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return user;
    }

    /// <summary>
    ///     Creates a new user within the specified workspace.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="workspace">The workspace in which to create the user.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the work.</param>
    /// <returns>The created user input.</returns>
    public static async Task<UserInputGql> CreateUserAsync(
        this HttpClient httpClient,
        WorkspaceInputGql workspace,
        CancellationToken cancellationToken)
    {
        var userId = Provider.Sql.Create();

        var newUser = new UserInputGql
        {
            Id = userId,
            FirstName = CreateString(),
            LastName = CreateString(),
            Email = $"{CreateString()}@example.com",
            WorkspaceId = workspace.Id!.Value,
            UpdatedAt = DateTimeOffset.UtcNow,
            IsDeleted = false,
        };

        var userInputPushRowGql = new UserInputPushRowGql
        {
            AssumedMasterState = null,
            NewDocumentState = newUser,
        };

        var pushUserInputGql = new PushUserInputGql
        {
            UserPushRow = new List<UserInputPushRowGql?>
            {
                userInputPushRowGql,
            },
        };

        var createUser = new MutationQueryBuilderGql().WithPushUser(new PushUserPayloadQueryBuilderGql().WithAllFields(), pushUserInputGql);

        var response = await httpClient.PostGqlMutationAsync(createUser, cancellationToken);

        response.Errors.Should()
            .BeNullOrEmpty();

        return newUser;
    }

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

        var response = await httpClient.PostGqlMutationAsync(createWorkspace, cancellationToken, jwtAccessToken);

        // Get the workspace by id here, since the server has set the workspace.UpdatedAt with the current time.
        // We need to get the latest data so that we have the correct AssumedMasterState for the next update.
        var workspaceFromServer = await httpClient.GetWorkspaceByIdAsync(workspaceId, cancellationToken, jwtAccessToken);

        var updatedWorkspaceInput = new WorkspaceInputGql
        {
            Id = workspaceFromServer.Id,
            Name = workspaceFromServer.Name,
            UpdatedAt = workspaceFromServer.UpdatedAt,
            IsDeleted = workspaceFromServer.IsDeleted,
            Topics = workspaceFromServer.Topics?.ToList(),
        };

        return (updatedWorkspaceInput, response);
    }

    public static async Task<WorkspaceGql> UpdateWorkspaceAsync(
        this HttpClient httpClient,
        WorkspaceInputGql workspace,
        CancellationToken cancellationToken,
        string? jwtAccessToken = null)
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

        var updateWorkspace =
            new MutationQueryBuilderGql().WithPushWorkspace(new PushWorkspacePayloadQueryBuilderGql().WithAllFields(), workspaceInputGql);

        var response = await httpClient.PostGqlMutationAsync(updateWorkspace, cancellationToken);

        response.Errors.Should()
            .BeNullOrEmpty();

        return await httpClient.GetWorkspaceByIdAsync(workspace.Id, cancellationToken, jwtAccessToken);
    }

    public static async Task<WorkspaceGql> UpdateWorkspaceAsync(
        this HttpClient httpClient,
        WorkspaceGql workspace,
        CancellationToken cancellationToken,
        string? jwtAccessToken = null)
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

        var updateWorkspace =
            new MutationQueryBuilderGql().WithPushWorkspace(new PushWorkspacePayloadQueryBuilderGql().WithAllFields(), workspaceInputGql);

        var response = await httpClient.PostGqlMutationAsync(updateWorkspace, cancellationToken);

        response.Errors.Should()
            .BeNullOrEmpty();

        return await httpClient.GetWorkspaceByIdAsync(workspace.Id, cancellationToken, jwtAccessToken);
    }

    public static async Task VerifyWorkspaceAsync(
        this HttpClient httpClient,
        WorkspaceInputGql workspaceInput,
        CancellationToken cancellationToken)
    {
        var query = new QueryQueryBuilderGql().WithPullWorkspace(new WorkspacePullBulkQueryBuilderGql().WithAllFields(), 20000);

        var response = await httpClient.PostGqlQueryAsync(query, cancellationToken);

        response.Errors.Should()
            .BeNullOrEmpty();

        var pulledWorkspace = response.Data.PullWorkspace?.Documents?.SingleOrDefault(workspace => workspace.Id == workspaceInput.Id);

        if (pulledWorkspace == null)
        {
            Assert.Fail("The pulled workspace must not be null");
        }

        Debug.Assert(workspaceInput.Id != null, "workspaceInput.Id != null");
        pulledWorkspace.Id.Should()
            .Be(workspaceInput.Id.Value);
        pulledWorkspace.Name.Should()
            .Be(workspaceInput.Name?.Value);
        pulledWorkspace.IsDeleted.Should()
            .Be(workspaceInput.IsDeleted);
        pulledWorkspace.UpdatedAt.Should()
            .BeAfter(workspaceInput.UpdatedAt, "the backend always updates UpdatedAt to the server datetimeoffset");
        pulledWorkspace.Topics.Should()
            .Equal(workspaceInput.Topics?.Value);
    }

    public static async Task<WorkspaceGql> GetWorkspaceByIdAsync(
        this HttpClient httpClient,
        Guid workspaceId,
        CancellationToken cancellationToken,
        string? jwtAccessToken = null)
    {
        var query = new QueryQueryBuilderGql().WithPullWorkspace(new WorkspacePullBulkQueryBuilderGql().WithAllFields(), 1000);

        var response = await httpClient.PostGqlQueryAsync(query, cancellationToken, jwtAccessToken);

        response.Errors.Should()
            .BeNullOrEmpty();

        return response.Data.PullWorkspace?.Documents?.Single(workspace => workspace.Id == workspaceId)
               ?? throw new InvalidOperationException("The workspace must not be null");
    }

    public static WorkspaceInputGql ToWorkspaceInputGql(this WorkspaceGql workspaceGql)
    {
        return new WorkspaceInputGql
        {
            Name = workspaceGql.Name,
            Id = workspaceGql.Id,
            IsDeleted = workspaceGql.IsDeleted,
            UpdatedAt = workspaceGql.UpdatedAt,
            Topics = workspaceGql.Topics?.ToList(),
        };
    }

    /// <summary>
    ///     Creates a new LiveDoc within the specified workspace, owned by the specified user.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="workspace">The workspace in which to create the LiveDoc.</param>
    /// <param name="owner">The user who will own the LiveDoc.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the work.</param>
    /// <returns>The created LiveDoc input.</returns>
    public static async Task<LiveDocInputGql> CreateLiveDocAsync(
        this HttpClient httpClient,
        WorkspaceInputGql workspace,
        UserInputGql owner,
        CancellationToken cancellationToken)
    {
        var liveDocId = Provider.Sql.Create();

        var newLiveDoc = new LiveDocInputGql
        {
            Id = liveDocId,
            Content = CreateString(),
            OwnerId = owner.Id!.Value,
            WorkspaceId = workspace.Id!.Value,
            UpdatedAt = DateTimeOffset.UtcNow,
            IsDeleted = false,
            Topics = new List<string>
            {
                workspace.Id!.Value.ToString(),
            },
        };

        var liveDocInputPushRowGql = new LiveDocInputPushRowGql
        {
            AssumedMasterState = null,
            NewDocumentState = newLiveDoc,
        };

        var pushLiveDocInputGql = new PushLiveDocInputGql
        {
            LiveDocPushRow = new List<LiveDocInputPushRowGql?>
            {
                liveDocInputPushRowGql,
            },
        };

        var createLiveDoc = new MutationQueryBuilderGql().WithPushLiveDoc(
            new PushLiveDocPayloadQueryBuilderGql().WithAllFields(), pushLiveDocInputGql);

        var response = await httpClient.PostGqlMutationAsync(createLiveDoc, cancellationToken);

        response.Errors.Should()
            .BeNullOrEmpty();

        return newLiveDoc;
    }

    public static async Task<LiveDocGql> UpdateLiveDocAsync(
        this HttpClient httpClient,
        LiveDocInputGql liveDoc,
        CancellationToken cancellationToken)
    {
        var assumedMasterState = new LiveDocInputGql
        {
            Id = liveDoc.Id,
            Content = liveDoc.Content,
            OwnerId = liveDoc.OwnerId,
            WorkspaceId = liveDoc.WorkspaceId,
            IsDeleted = liveDoc.IsDeleted,
            UpdatedAt = liveDoc.UpdatedAt,
            Topics = liveDoc.Topics,
        };

        var updatedLiveDoc = new LiveDocInputGql
        {
            Id = liveDoc.Id,
            Content = CreateString(), // Update the content
            OwnerId = liveDoc.OwnerId,
            WorkspaceId = liveDoc.WorkspaceId,
            IsDeleted = liveDoc.IsDeleted,
            UpdatedAt = DateTimeOffset.UtcNow,
            Topics = liveDoc.Topics,
        };

        var liveDocInputPushRowGql = new LiveDocInputPushRowGql
        {
            AssumedMasterState = assumedMasterState,
            NewDocumentState = updatedLiveDoc,
        };

        var pushLiveDocInputGql = new PushLiveDocInputGql
        {
            LiveDocPushRow = new List<LiveDocInputPushRowGql?>
            {
                liveDocInputPushRowGql,
            },
        };

        var updateLiveDoc = new MutationQueryBuilderGql().WithPushLiveDoc(
            new PushLiveDocPayloadQueryBuilderGql().WithAllFields(), pushLiveDocInputGql);

        var response = await httpClient.PostGqlMutationAsync(updateLiveDoc, cancellationToken);

        response.Errors.Should()
            .BeNullOrEmpty();

        return await httpClient.GetLiveDocByIdAsync(liveDoc.Id!.Value, cancellationToken);
    }

    public static async Task<LiveDocGql> GetLiveDocByIdAsync(
        this HttpClient httpClient,
        Guid liveDocId,
        CancellationToken cancellationToken)
    {
        var query = new QueryQueryBuilderGql().WithPullLiveDoc(new LiveDocPullBulkQueryBuilderGql().WithAllFields()
            .WithDocuments(new LiveDocQueryBuilderGql().WithAllFields()), 1000);

        var response = await httpClient.PostGqlQueryAsync(query, cancellationToken);

        response.Errors.Should()
            .BeNullOrEmpty();

        return response.Data.PullLiveDoc?.Documents?.Single(liveDoc => liveDoc.Id == liveDocId)
               ?? throw new InvalidOperationException("The LiveDoc must not be null");
    }
}
