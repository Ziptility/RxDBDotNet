using FluentAssertions;
using RxDBDotNet.Tests.Helpers;
using RxDBDotNet.Tests.Model;
using Xunit.Abstractions;

namespace RxDBDotNet.Tests
{
    public class SubscriptionTests(ITestOutputHelper output) : TestBase(output)
    {
        [Fact]
        public async Task TestHeroSubscription()
        {
            // Arrange
            await using var subscriptionClient = await Factory.CreateGraphQLSubscriptionClientAsync();

            var subscriptionQuery = new SubscriptionQueryBuilderGql()
                .WithStreamHero(new HeroPullBulkQueryBuilderGql()
                    .WithDocuments(new HeroQueryBuilderGql().WithAllFields())
                    .WithCheckpoint(new CheckpointQueryBuilderGql().WithAllFields()),
                    new HeroInputHeadersGql { Authorization = "test-auth-token" })
                .Build();

            var subscription = subscriptionClient.SubscribeAsync<GqlSubscriptionResponse>(subscriptionQuery);

            // Act
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(600));

            // Start listening for subscription data
            var subscriptionTask = ListenForSubscriptionDataAsync(subscription, cts.Token);

            // Ensure the subscription is established before simulating the change
            await Task.Delay(1000, cts.Token);

            // Simulate hero change
            await SimulateHeroChangeAsync();

            // Wait for the subscription data or timeout
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(600), cts.Token);
            var completedTask = await Task.WhenAny(subscriptionTask, timeoutTask);

            // Assert
            if (completedTask == timeoutTask)
            {
                throw new TimeoutException("Subscription data was not received within the expected timeframe.");
            }

            var receivedData = await subscriptionTask;
            receivedData.Should().NotBeNull("Subscription data should not be null.");
            receivedData.Errors.Should().BeNullOrEmpty();
            receivedData.Data.Should().NotBeNull();
            receivedData.Data?.StreamHero.Should().NotBeNull();
            receivedData.Data?.StreamHero?.Documents.Should().NotBeEmpty();

            var hero = receivedData.Data?.StreamHero?.Documents?.First();
            hero.Should().NotBeNull();
            hero?.Name.Should().NotBeNullOrEmpty();
            hero?.Color.Should().NotBeNullOrEmpty();
            hero?.Id.Should().NotBe(Guid.Empty);
            hero?.IsDeleted.Should().BeFalse();
            hero?.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        }

        private static async Task<GqlSubscriptionResponse> ListenForSubscriptionDataAsync(
            IAsyncEnumerable<GqlSubscriptionResponse> subscription,
            CancellationToken cancellationToken)
        {
            await foreach (var response in subscription.WithCancellation(cancellationToken))
            {
                return response;
            }
            throw new OperationCanceledException("Subscription was cancelled before receiving any data.");
        }

        private async Task SimulateHeroChangeAsync()
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
            response.Data.PushHero.Should().NotBeNull();
            response.Data.PushHero.Should().BeEmpty();
        }
    }
}
