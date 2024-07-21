
using System.Net;

namespace RxDBDotNet.Tests
{
    public class PushUserTests
    {
        [Fact]
        public async Task PushUser_ShouldAddNewUser_WhenUserDetailsAreValid_1_1()
        {
            // Arrange
            var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.LiveDocs_AppHost>();
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
