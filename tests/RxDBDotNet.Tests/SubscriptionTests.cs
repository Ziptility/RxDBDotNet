using System.Diagnostics;
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
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            // Start listening for subscription data
            var subscriptionTask = ListenForSubscriptionDataAsync(subscription, cts.Token);

            // Ensure the subscription is established before simulating the change
            await Task.Delay(1000, cts.Token);

            // Simulate hero change
            var newHero = await SimulateHeroChangeAsync();

            // Wait for the subscription data or timeout
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10), cts.Token);
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

            var streamedHero = receivedData.Data?.StreamHero?.Documents?.First();
            streamedHero.Should().NotBeNull();

            // Assert that the streamed hero properties match the newHero properties
            streamedHero?.Id.Should().Be(newHero.Id, "The streamed hero ID should match the created hero ID");
            streamedHero?.Name.Should().Be(newHero.Name?.Value, "The streamed hero name should match the created hero name");
            streamedHero?.Color.Should().Be(newHero.Color?.Value, "The streamed hero color should match the created hero color");
            streamedHero?.IsDeleted.Should().Be(newHero.IsDeleted?.Value, "The streamed hero IsDeleted status should match the created hero");
            streamedHero?.UpdatedAt.Should().BeCloseTo(newHero.UpdatedAt?.Value ?? DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5), "The streamed hero UpdatedAt should be close to the created hero's timestamp");

            // Assert on the checkpoint
            receivedData.Data?.StreamHero?.Checkpoint.Should().NotBeNull("The checkpoint should be present");
            receivedData.Data?.StreamHero?.Checkpoint?.LastDocumentId.Should().Be(newHero.Id?.Value, "The checkpoint's LastDocumentId should match the new hero's ID");
            Debug.Assert(newHero.UpdatedAt != null, "newHero.UpdatedAt != null");
            receivedData.Data?.StreamHero?.Checkpoint?.UpdatedAt.Should().BeCloseTo(newHero.UpdatedAt.Value, TimeSpan.FromSeconds(5), "The checkpoint's UpdatedAt should be close to the new hero's timestamp");
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

        private async Task<HeroInputGql> SimulateHeroChangeAsync()
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

            return newHero;
        }
    }
}
