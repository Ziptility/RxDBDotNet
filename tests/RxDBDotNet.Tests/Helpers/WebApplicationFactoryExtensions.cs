using Microsoft.AspNetCore.Mvc.Testing;

namespace RxDBDotNet.Tests.Helpers
{
    public static class WebApplicationFactoryExtensions
    {
        public static async Task<GraphQLSubscriptionClient> CreateGraphQLSubscriptionClientAsync<TProgram>(
            this WebApplicationFactory<TProgram> factory,
            string subscriptionEndpoint = "/graphql",
            TimeSpan? timeout = null) where TProgram : class
        {
            ArgumentNullException.ThrowIfNull(factory);

            var wsClient = factory.Server.CreateWebSocketClient();
            wsClient.ConfigureRequest = request =>
            {
                request.Headers.SecWebSocketProtocol = "graphql-transport-ws";
                request.Headers.SecWebSocketVersion = "13";
                request.Headers.SecWebSocketExtensions = "permessage-deflate";
                request.Headers.Connection = "keep-alive, Upgrade";
                request.Headers.Upgrade = "websocket";
            };

            var webSocket = await wsClient.ConnectAsync(
                new Uri(factory.Server.BaseAddress, subscriptionEndpoint),
                CancellationToken.None);

            // Pass the timeout to the GraphQLSubscriptionClient
            var client = new GraphQLSubscriptionClient(webSocket, timeout ?? TimeSpan.FromMinutes(30));
            await client.InitializeAsync();
            return client;
        }
    }
}
