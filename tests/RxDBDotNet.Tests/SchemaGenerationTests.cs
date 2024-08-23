using GraphQlClientGenerator;
using Newtonsoft.Json;

namespace RxDBDotNet.Tests;

[Collection("DockerSetup")]
public class SchemaGenerationTests
{
    [Fact]
    public async Task GeneratedSchemaForADocumentShouldReflectTheNameDefinedInTheGraphQLNameAttribute()
    {
        TestContext? testContext = null;

        try
        {
            // Arrange
            testContext = await TestSetupUtil.SetupAsync();
            using var requestContent = new StringContent(JsonConvert.SerializeObject(new
        {
            query = IntrospectionQuery.Text,
        }), Encoding.UTF8, "application/json");

        // Act
        var schemaResponse = await testContext.HttpClient.PostAsync("/graphql", requestContent, testContext.CancellationToken);

        // Assert
        var schemaString = await schemaResponse.Content.ReadAsStringAsync(testContext.CancellationToken);

        schemaString.Should()
            .NotContain("ReplicatedWorkspace");

        schemaString.Should()
            .Contain("Workspace");
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
