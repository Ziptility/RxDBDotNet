using System.Diagnostics;
using FluentAssertions;
using RxDBDotNet.Tests.Helpers;
using RxDBDotNet.Tests.Model;
using Xunit.Abstractions;

namespace RxDBDotNet.Tests
{
    public class SubscriptionTests(ITestOutputHelper output) : TestBase(output)
    {
        private GraphQLSubscriptionClient? _subscriptionClient;

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
#pragma warning disable CA2000
            _subscriptionClient = await Factory.CreateGraphQLSubscriptionClientAsync();
#pragma warning restore CA2000
        }

        public override async Task DisposeAsync()
        {
            if (_subscriptionClient != null)
            {
                await _subscriptionClient.DisposeAsync();
            }
            await base.DisposeAsync();
        }

        [Fact]
        public async Task TestHeroSubscription()
        {
            // Arrange
            var subscriptionQuery = new SubscriptionQueryBuilderGql()
                .WithStreamHero(new HeroPullBulkQueryBuilderGql()
                    .WithDocuments(new HeroQueryBuilderGql().WithAllFields())
                    .WithCheckpoint(new CheckpointQueryBuilderGql().WithAllFields()),
                    new HeroInputHeadersGql { Authorization = "test-auth-token" })
                .Build();

            // Act
            var subscription = await _subscriptionClient!.SubscribeAsync<GqlSubscriptionResponse>(subscriptionQuery);

            // Simulate a change that should trigger the subscription
            await SimulateHeroChange();

            // Assert
            await foreach (var response in subscription)
            {
                response.Errors.Should().BeNullOrEmpty();
                response.Data.Should().NotBeNull();
                response.Data!.StreamHero.Should().NotBeNull();
                response.Data.StreamHero!.Documents.Should().NotBeEmpty();

                Debug.Assert(response.Data.StreamHero.Documents != null, "response.Data.StreamHero.Documents != null");
                var hero = response.Data.StreamHero.Documents.First();
                hero.Name.Should().NotBeNullOrEmpty();
                hero.Color.Should().NotBeNullOrEmpty();
                hero.Id.Should().NotBe(Guid.Empty);
                hero.IsDeleted.Should().BeFalse();
                hero.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));

                // Exit after first received item
                break;
            }
        }

        private async Task SimulateHeroChange()
        {
            var newHero = new HeroInputGql
            {
                Id = Guid.NewGuid(),
                Name = "New Subscription Hero",
                Color = "Cosmic Blue",
                IsDeleted = false,
                UpdatedAt = DateTimeOffset.UtcNow,
            };

            var mutation = new MutationQueryBuilderGql()
                .WithPushHero(new HeroQueryBuilderGql().WithAllFields(),
                    new List<HeroInputPushRowGql?>
                    {
                        new()
                        {
                            NewDocumentState = newHero,
                        },
                    });

            var response = await HttpClient.PostGqlMutationAsync(mutation);

            response.Errors.Should().BeNullOrEmpty();
            response.Data.Should().NotBeNull();
            response.Data!.PushHero.Should().NotBeNull();
            response.Data.PushHero.Should().BeEmpty(); // Assuming no conflicts
        }
    }
}
