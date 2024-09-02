using GraphQlClientGenerator;
using Newtonsoft.Json;

namespace RxDBDotNet.Tests;

[Collection("DockerSetup")]
public class SchemaGenerationTests : IAsyncLifetime
{
    private TestContext _testContext = null!;

    public Task InitializeAsync()
    {
        _testContext = TestSetupUtil.Setup();

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _testContext.DisposeAsync();
    }

    [Fact]
    public async Task GeneratedSchemaForADocumentShouldReflectTheNameDefinedInTheGraphQLNameAttribute()
    {
        // Arrange
        using var requestContent = new StringContent(JsonConvert.SerializeObject(new
        {
            query = IntrospectionQuery.Text,
        }), Encoding.UTF8, "application/json");

        // Act
        var schemaResponse = await _testContext.HttpClient.PostAsync("/graphql", requestContent, _testContext.CancellationToken);

        // Assert
        var schemaString = await schemaResponse.Content.ReadAsStringAsync(_testContext.CancellationToken);

        schemaString.Should()
            .NotContain("ReplicatedWorkspace");

        schemaString.Should()
            .Contain("Workspace");
    }
}
