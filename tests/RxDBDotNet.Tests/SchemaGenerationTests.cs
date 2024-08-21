using System.Text;
using FluentAssertions;
using GraphQlClientGenerator;
using Newtonsoft.Json;
using RxDBDotNet.Tests.Setup;
using Xunit.Abstractions;

namespace RxDBDotNet.Tests;

public class SchemaGenerationTests(ITestOutputHelper output) : TestSetupUtil(output)
{
    [Fact]
    public async Task GeneratedSchemaForADocumentShouldReflectTheNameDefinedInTheGraphQLNameAttribute()
    {
        // Arrange
        using var requestContent = new StringContent(JsonConvert.SerializeObject(new
        {
            query = IntrospectionQuery.Text,
        }), Encoding.UTF8, "application/json");

        // Act
        var schemaResponse = await HttpClient.PostAsync("/graphql", requestContent);

        // Assert
        var schemaString = await schemaResponse.Content.ReadAsStringAsync();

        schemaString.Should()
            .NotContain("ReplicatedWorkspace");

        schemaString.Should()
            .Contain("Workspace");
    }
}
