using RxDBDotNet.Tests.Setup;
using Xunit.Abstractions;

namespace RxDBDotNet.Tests;

public class SecurityTests(ITestOutputHelper output) : TestSetupUtil(output)
{
    [Fact]
    public void AStandardUserShouldNotBeAbleToCreateAWorkspace()
    {
        // Arrange
        using var testTimeoutTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var testTimeoutToken = testTimeoutTokenSource.Token;

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
}
