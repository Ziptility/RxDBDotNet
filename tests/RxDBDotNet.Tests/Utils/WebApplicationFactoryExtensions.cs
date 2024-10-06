// tests\RxDBDotNet.Tests\Utils\WebApplicationFactoryExtensions.cs

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;

namespace RxDBDotNet.Tests.Utils;

public static class WebApplicationFactoryExtensions
{
    public static async Task<GraphQLSubscriptionClient> CreateGraphQLSubscriptionClientAsync<TProgram>(
        this WebApplicationFactory<TProgram> factory,
        CancellationToken cancellationToken,
        string? bearerToken = null) where TProgram : class
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

        var webSocket = await wsClient.ConnectAsync(new Uri(factory.Server.BaseAddress, "/graphql"), cancellationToken);

        var client = new GraphQLSubscriptionClient(webSocket, bearerToken);
        await client.InitializeAsync(cancellationToken);
        return client;
    }
}
