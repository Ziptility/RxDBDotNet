
using System.Net;

namespace RxDBDotNet.Tests
{
    public class UserMutationTests
    {
        [Fact]
        public async Task TestMethod1()
        {
            // Arrange
            var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Example_AppHost>();
            await using var app = await appHost.BuildAsync();
            await app.StartAsync();

            // Act
            using var httpClient = app.CreateHttpClient("graphqlapi");
            using var response = await httpClient.GetAsync(new Uri("/graphql", UriKind.Relative));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
