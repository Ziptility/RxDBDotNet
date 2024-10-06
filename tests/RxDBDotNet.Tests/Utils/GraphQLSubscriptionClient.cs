// tests\RxDBDotNet.Tests\Utils\GraphQLSubscriptionClient.cs
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Nodes;
using RxDBDotNet.Security;

namespace RxDBDotNet.Tests.Utils;

/// <summary>
///     Provides a client for GraphQL subscriptions over WebSocket connections.
///     This client is designed to work with Hot Chocolate GraphQL server and follows the graphql-ws protocol.
/// </summary>
public sealed class GraphQLSubscriptionClient : IAsyncDisposable
{
    private readonly WebSocket _webSocket;
    private readonly string? _bearerToken;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphQLSubscriptionClient"/> class for use in test scenarios.
    /// </summary>
    /// <param name="webSocket">
    /// A <see cref="WebSocket"/> instance created by the test server, already connected to the GraphQL endpoint.
    /// </param>
    /// <param name="bearerToken">
    /// An optional bearer token for authentication.
    /// </param>
    /// <remarks>
    /// <para>
    /// This constructor initializes a new <see cref="GraphQLSubscriptionClient"/> with the provided WebSocket connection.
    /// The client uses the graphql-transport-ws protocol for communication with a GraphQL server that supports
    /// subscriptions.
    /// </para>
    /// <para>
    /// The timeout parameter is particularly useful for testing and debugging scenarios where operations
    /// might take longer than usual, allowing for extended debugging sessions without connection timeouts.
    /// </para>
    /// <para>
    /// After creating an instance of <see cref="GraphQLSubscriptionClient"/>, you must call <see cref="InitializeAsync"/>
    /// before attempting to use the client for subscriptions.
    /// </para>
    /// </remarks>
    /// <example>
    /// This example shows how to create and initialize a <see cref="GraphQLSubscriptionClient"/> in a test context:
    /// <code>
    /// // Assuming 'factory' is a WebApplicationFactory&lt;TProgram&gt; instance
    /// var wsClient = factory.Server.CreateWebSocketClient();
    /// var webSocket = await wsClient.ConnectAsync(
    ///     new Uri(factory.Server.BaseAddress, "/graphql"),
    ///     CancellationToken.None);
    /// var client = new GraphQLSubscriptionClient(webSocket, TimeSpan.FromMinutes(5));
    /// await client.InitializeAsync();
    /// // The client is now ready for use in tests
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="webSocket"/> parameter is null.</exception>
    public GraphQLSubscriptionClient(WebSocket webSocket, string? bearerToken = null)
    {
        _webSocket = webSocket ?? throw new ArgumentNullException(nameof(webSocket));
        _bearerToken = bearerToken;
    }

    /// <summary>
    ///     Releases the resources used by the GraphQLSubscriptionClient.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        if (_webSocket.State == WebSocketState.Open)
        {
            try
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
            catch
            {
                // Ignore exceptions during closing, as the connection might already be closed
            }
        }

        _webSocket.Dispose();
    }

    /// <summary>
    ///     Initializes the GraphQL subscription connection.
    ///     This method should be called immediately after creating the client instance.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the initialization.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the connection initialization fails.</exception>
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        object initMessage;

        if (!string.IsNullOrEmpty(_bearerToken))
        {
            initMessage = new
            {
                type = "connection_init",
                payload = new SocketConnectPayload
                {
                    Headers = new Headers
                    {
                        Authorization = $"Bearer {_bearerToken}",
                    },
                },
            };
        }
        else
        {
            initMessage = new
            {
                type = "connection_init",
            };
        }

        await SendMessageAsync(initMessage, cancellationToken);

        var response = await ReceiveMessageAsync(cancellationToken);

        if (!string.Equals(response.Type, "connection_ack", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Failed to initialize GraphQL subscription connection. Received message type: {response.Type}");
        }
    }

    /// <summary>
    ///     Creates an IAsyncEnumerable for a GraphQL subscription that allows collecting all responses.
    /// </summary>
    /// <typeparam name="TResponse">The type of the subscription response.</typeparam>
    /// <param name="subscriptionQuery">The GraphQL subscription query to execute.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the subscription.</param>
    /// <returns>An IAsyncEnumerable of subscription responses.</returns>
    /// <remarks>
    ///     <para>
    ///         This method sends the subscription request immediately and starts yielding responses as they arrive.
    ///         It will continue to yield responses until the subscription is completed, an error occurs, or the operation is
    ///         cancelled.
    ///     </para>
    ///     <para>
    ///         The method handles the following GraphQL over WebSocket protocol message types:
    ///         - "next": Yields the payload as a deserialized response.
    ///         - "complete": Ends the enumeration.
    ///         - "error": Throws an InvalidOperationException with the error details.
    ///     </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when:
    ///     - The GraphQL server returns an error message.
    ///     - The subscription payload cannot be deserialized to the specified TResponse type.
    /// </exception>
    /// <exception cref="IOException">
    ///     Thrown when:
    ///     - The WebSocket connection is closed unexpectedly.
    ///     - The WebSocket connection is closed prematurely.
    /// </exception>
    /// <exception cref="TimeoutException">
    ///     Thrown when a receive operation on the WebSocket connection times out.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    ///     Thrown when this method is called after the GraphQLSubscriptionClient has been disposed.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    ///     Thrown when the operation is cancelled via the provided cancellationToken.
    /// </exception>
    public async IAsyncEnumerable<TResponse> SubscribeAndCollectAsync<TResponse>(
        string subscriptionQuery,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await InitializeSubscriptionAsync(subscriptionQuery, cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            var (messageType, payload) = await ReceiveMessageAsync(cancellationToken);

            switch (messageType.ToLowerInvariant())
            {
                case "next":
                    if (payload != null)
                    {
                        var response = JsonSerializer.Deserialize<TResponse>(payload.ToJsonString(), SerializationUtils.GetJsonSerializerOptions());
                        if (!EqualityComparer<TResponse?>.Default.Equals(response, default))
                        {
                            yield return response
                                         ?? throw new InvalidOperationException(
                                             $"The subscription payload cannot be deserialized to the specified {typeof(TResponse).Name} type");
                        }
                    }

                    break;
                case "complete":
                    yield break;
                case "error":
                    throw new InvalidOperationException($"Subscription error: {payload?.ToJsonString()}");
            }
        }
    }

    private Task InitializeSubscriptionAsync(string subscriptionQuery, CancellationToken cancellationToken)
    {
        var subscribeMessage = new
        {
            id = Guid.NewGuid()
                .ToString(),
            type = "subscribe",
            payload = new
            {
                query = subscriptionQuery,
            },
        };

        return SendMessageAsync(subscribeMessage, cancellationToken);
    }

    /// <summary>
    ///     Sends a message to the GraphQL server over the WebSocket connection.
    /// </summary>
    /// <param name="message">The message object to be sent.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private Task SendMessageAsync(object message, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        var json = JsonSerializer.Serialize(message, SerializationUtils.GetJsonSerializerOptions());

        var buffer = Encoding.UTF8.GetBytes(json);

        return _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, cancellationToken);
    }

    /// <summary>
    ///     Receives a message from the GraphQL server over the WebSocket connection.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A tuple containing the message type and payload.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the WebSocket connection closes unexpectedly.</exception>
    /// <exception cref="TimeoutException">Thrown when the receive operation times out.</exception>
    /// <exception cref="IOException">The WebSocket connection was closed prematurely or unexpectedly.</exception>
    private async Task<(string Type, JsonNode? Payload)> ReceiveMessageAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];

        while (true)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);

            WebSocketReceiveResult result;
            try
            {
                result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
            }
            catch (WebSocketException ex) when (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
            {
                throw new IOException("The WebSocket connection was closed prematurely.", ex);
            }

            if (result.MessageType == WebSocketMessageType.Close)
            {
                throw new IOException("WebSocket connection closed unexpectedly");
            }

            var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
            var message = JsonNode.Parse(json);
            var messageType = message?["type"]
                                  ?.GetValue<string>()
                              ?? "";

            if (messageType.Equals("ping", StringComparison.OrdinalIgnoreCase))
            {
                await SendMessageAsync(new
                {
                    type = "pong",
                }, cancellationToken);
                continue;
            }

            return (messageType, message?["payload"]);
        }
    }
}
