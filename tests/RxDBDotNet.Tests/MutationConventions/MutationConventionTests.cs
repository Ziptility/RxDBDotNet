using GraphQlClientGenerator;
using HotChocolate.Subscriptions;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace RxDBDotNet.Tests.MutationConventions;

[Collection("DockerSetup")]
public class MutationConventionTests : IAsyncLifetime
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
    public async Task AddReplicationServer_ShouldNotOverrideExistingMutationConventionOptions()
    {
        // Arrange
        TestContext = new TestScenarioBuilder(configureGraphQLDefaults: false)
            .ConfigureGraphQL(builder =>
            {
                // Simulate an RxDBDotNet library user who has already configured mutation conventions applyToAllMutations: true
                builder.AddMutationConventions(applyToAllMutations: true);

                // And who has also added a mutation to the schema
                builder.AddTypeExtension<BookMutations>();

                builder.ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                    // prior to the fix, this was always overriding AddMutationConventions to applyToAllMutations: false
                    .AddReplication()
                    .AddRedisSubscriptions(provider => provider.GetRequiredService<IConnectionMultiplexer>(), new SubscriptionOptions
                    {
                        TopicPrefix = Guid.NewGuid()
                            .ToString(),
                    })
                    .AddSubscriptionDiagnostics();
            })
            .Build();

        using var requestContent = new StringContent(JsonConvert.SerializeObject(new
        {
            query = IntrospectionQuery.Text,
        }), Encoding.UTF8, "application/json");

        // Act
        var schemaResponse = await TestContext.HttpClient.PostAsync("/graphql", requestContent, TestContext.CancellationToken);

        // Assert
        var schemaString = await schemaResponse.Content.ReadAsStringAsync(TestContext.CancellationToken);
        schemaString.Should()
            .Contain("AddBookPayload");
    }
}
