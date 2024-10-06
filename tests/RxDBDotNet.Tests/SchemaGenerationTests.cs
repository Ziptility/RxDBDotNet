// tests\RxDBDotNet.Tests\SchemaGenerationTests.cs

using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using GraphQlClientGenerator;
using Newtonsoft.Json;
using RxDBDotNet.Tests.Setup;

namespace RxDBDotNet.Tests;

[Collection("DockerSetup")]
public class SchemaGenerationTests : IAsyncLifetime
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
    public async Task GeneratedSchemaForADocumentShouldReflectTheNameDefinedInTheGraphQLNameAttribute()
    {
        // Arrange
        TestContext = new TestScenarioBuilder().Build();
        using var requestContent = new StringContent(JsonConvert.SerializeObject(new
        {
            query = IntrospectionQuery.Text,
        }), Encoding.UTF8, "application/json");

        // Act
        var schemaResponse = await TestContext.HttpClient.PostAsync("/graphql", requestContent, TestContext.CancellationToken);

        // Assert
        var schemaString = await schemaResponse.Content.ReadAsStringAsync(TestContext.CancellationToken);

        schemaString.Should()
            .NotContain("ReplicatedWorkspace");

        schemaString.Should()
            .Contain("Workspace");
    }
}
