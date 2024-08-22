using RxDBDotNet.Tests.Setup;

namespace RxDBDotNet.Tests;

[Collection("Docker collection")]
public class SecurityTests
{
    private readonly DockerSetupFixture _fixture;

    public SecurityTests(DockerSetupFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AStandardUserShouldNotBeAbleToCreateAWorkspace()
    {
        await _fixture.InitializeAsync();

        TestContext? testContext = null;

        try
        {
            // Arrange
            testContext = await TestSetupUtil.SetupAsync();

            // Configure security for this test

            // As a system admin user:
            // 1. Create a workspace

            // 2. Create a standard user within the workspace

            // As a standard user:

            // Attempt to create a workspace

            // Act

            // Assert
            // The response should contain an unauthorized error
        }
        finally
        {
            if (testContext != null)
            {
                await testContext.DisposeAsync();
            }
        }
    }
}
