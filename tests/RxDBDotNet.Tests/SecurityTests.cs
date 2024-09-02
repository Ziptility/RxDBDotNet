using RxDBDotNet.Tests.Model;
using RxDBDotNet.Tests.Utils;

namespace RxDBDotNet.Tests;

[Collection("DockerSetup")]
public class SecurityTests : IAsyncLifetime
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
    public async Task AWorkspaceAdminShouldBeAbleToCreateAWorkspace()
    {
        // Arrange
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
}
