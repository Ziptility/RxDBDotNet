using GraphQlClientGenerator;
using Newtonsoft.Json;

namespace RxDBDotNet.Tests.MutationConventions;

[Collection("DockerSetup")]
public class MutationConventionTests : IAsyncLifetime
{
    private TestContext _testContext = null!;

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _testContext.DisposeAsync();
    }

    [Fact]
    public async Task AddReplicationServer_ShouldNotOverrideExistingMutationConventionOptions()
    {
        // Arrange
        _testContext = TestSetupUtil.SetupWithDefaultsAndCustomConfig(configureGraphQL: graphQLBuilder =>
        {
            // Simulate a library user who has already configured mutation conventions
            // via the serviceCollection (i.e., outside of RxDBDotNet)
            graphQLBuilder.AddMutationConventions(false);

            // And who has also added a mutation to the schema
            graphQLBuilder.AddTypeExtension<BookMutations>();
        });

        using var requestContent = new StringContent(JsonConvert.SerializeObject(new
        {
            query = IntrospectionQuery.Text,
        }), Encoding.UTF8, "application/json");

        // Act
        var schemaResponse = await _testContext.HttpClient.PostAsync("/graphql", requestContent, _testContext.CancellationToken);

        // Assert
        var schemaString = await schemaResponse.Content.ReadAsStringAsync();
        schemaString.Should()
            .Contain("CreateWorkspace");
    }
}
