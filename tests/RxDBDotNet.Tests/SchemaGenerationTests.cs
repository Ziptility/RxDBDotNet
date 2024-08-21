﻿using System.Text;
using FluentAssertions;
using GraphQlClientGenerator;
using Newtonsoft.Json;
using RxDBDotNet.Tests.Setup;

namespace RxDBDotNet.Tests;

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
        var schemaResponse = await testContext.HttpClient.PostAsync("/graphql", requestContent);

        // Assert
        var schemaString = await schemaResponse.Content.ReadAsStringAsync();

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
