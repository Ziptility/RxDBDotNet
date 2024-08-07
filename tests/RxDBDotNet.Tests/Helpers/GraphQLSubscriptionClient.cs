using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace RxDBDotNet.Tests.Helpers
{
    /// <summary>
    /// Provides a client for GraphQL subscriptions over WebSocket connections.
    /// This client is designed to work with Hot Chocolate GraphQL server and follows the graphql-ws protocol.
    /// </summary>
    public sealed class GraphQLSubscriptionClient : IAsyncDisposable
    {
        private readonly WebSocket _webSocket;
        private readonly CancellationTokenSource _cts;
        private readonly TimeSpan _timeout;

        /// <summary>
        /// Initializes a new instance of the GraphQLSubscriptionClient class for use in test scenarios.
        /// </summary>
        /// <param name="webSocket">
        /// A WebSocket instance created by the test server, already connected to the GraphQL endpoint.</param>
        /// <param name="timeout">
        /// The maximum duration to wait for responses from the server.
        /// This timeout applies to individual operations such as receiving messages.
        /// If not specified, a default timeout of 30 minutes is used.
        /// </param>
        /// <remarks>
        /// <para>
        /// This constructor initializes a new GraphQLSubscriptionClient with the provided WebSocket connection.
        /// The client uses the graphql-transport-ws protocol for communication with a GraphQL server that supports subscriptions.
        /// </para>
        /// <para>
        /// The timeout parameter is particularly useful for testing and debugging scenarios where operations
        /// might take longer than usual, allowing for extended debugging sessions without connection timeouts.
        /// </para>
        /// <para>
        /// After creating an instance of GraphQLSubscriptionClient, you must call InitializeAsync()
        /// before attempting to use the client for subscriptions.
        /// </para>
        /// </remarks>
        /// <example>
        /// This example shows how to create and initialize a GraphQLSubscriptionClient in a test context:
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
        /// <exception cref="ArgumentNullException">Thrown if the webSocket parameter is null.</exception>
        public GraphQLSubscriptionClient(WebSocket webSocket, TimeSpan? timeout = null)
        {
            _webSocket = webSocket ?? throw new ArgumentNullException(nameof(webSocket));
            _cts = new CancellationTokenSource();
            _timeout = timeout ?? TimeSpan.FromMinutes(30); // Default 30-minute timeout
        }

        /// <summary>
        /// Initializes the GraphQL subscription connection.
        /// This method should be called immediately after creating the client instance.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the connection initialization fails.</exception>
        public async Task InitializeAsync()
        {
            var initMessage = new { type = "connection_init" };
            await SendMessageAsync(initMessage);

            var response = await ReceiveMessageAsync();
            if (!string.Equals(response.Type, "connection_ack", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Failed to initialize GraphQL subscription connection. Received message type: {response.Type}");
            }
        }

        /// <summary>
        /// Creates an IAsyncEnumerable for a GraphQL subscription.
        /// </summary>
        /// <typeparam name="TResponse">The type of the subscription response.</typeparam>
        /// <param name="subscriptionQuery">The GraphQL subscription query.</param>
        /// <returns>An IAsyncEnumerable of subscription responses.</returns>
        /// <remarks>
        /// This method does not immediately send the subscription request.
        /// The actual subscription is initiated when the returned IAsyncEnumerable is enumerated.
        /// </remarks>
        public IAsyncEnumerable<TResponse> SubscribeAsync<TResponse>(string subscriptionQuery)
        {
            return new AsyncSubscriptionEnumerable<TResponse>(this, subscriptionQuery, _cts.Token);
        }

        /// <summary>
        /// Sends a message to the GraphQL server over the WebSocket connection.
        /// </summary>
        /// <param name="message">The message object to be sent.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task SendMessageAsync(object message)
        {
            var json = JsonSerializer.Serialize(message, SerializationUtils.GetJsonSerializerOptions());
            var buffer = Encoding.UTF8.GetBytes(json);
            await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, _cts.Token);
        }

        /// <summary>
        /// Receives a message from the GraphQL server over the WebSocket connection.
        /// </summary>
        /// <returns>A tuple containing the message type and payload.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the WebSocket connection closes unexpectedly.</exception>
        /// <exception cref="TimeoutException">Thrown when the receive operation times out.</exception>
        private async Task<(string Type, JsonNode? Payload)> ReceiveMessageAsync()
        {
            var buffer = new byte[4096];
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
            timeoutCts.CancelAfter(_timeout);

            try
            {
                while (true)
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), timeoutCts.Token);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        throw new InvalidOperationException("WebSocket connection closed unexpectedly");
                    }

                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var message = JsonNode.Parse(json);
                    var messageType = message?["type"]?.GetValue<string>() ?? "";

                    if (messageType.Equals("ping", StringComparison.OrdinalIgnoreCase))
                    {
                        await SendMessageAsync(new { type = "pong" });
                        continue;
                    }

                    return (messageType, message?["payload"]);
                }
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
            {
                throw new TimeoutException($"WebSocket receive operation timed out after {_timeout.TotalSeconds} seconds.");
            }
        }

        /// <summary>
        /// Releases the resources used by the GraphQLSubscriptionClient.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            await _cts.CancelAsync();
            _cts.Dispose();
            if (_webSocket.State == WebSocketState.Open)
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }

            _webSocket.Dispose();
        }

        /// <summary>
        /// Represents an asynchronous enumerable for GraphQL subscription responses.
        /// </summary>
        /// <typeparam name="T">The type of the subscription response.</typeparam>
        private sealed class AsyncSubscriptionEnumerable<T> : IAsyncEnumerable<T>
        {
            private readonly GraphQLSubscriptionClient _client;
            private readonly string _subscriptionQuery;
            private readonly CancellationToken _cancellationToken;

            public AsyncSubscriptionEnumerable(GraphQLSubscriptionClient client, string subscriptionQuery, CancellationToken cancellationToken)
            {
                _client = client;
                _subscriptionQuery = subscriptionQuery;
                _cancellationToken = cancellationToken;
            }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
#pragma warning disable CA2000
                var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken, cancellationToken);
#pragma warning restore CA2000
                return new AsyncSubscriptionEnumerator<T>(_client, _subscriptionQuery, combinedCts.Token);
            }
        }

        /// <summary>
        /// Represents an asynchronous enumerator for GraphQL subscription responses.
        /// </summary>
        /// <typeparam name="T">The type of the subscription response.</typeparam>
        private sealed class AsyncSubscriptionEnumerator<T> : IAsyncEnumerator<T>
        {
            private readonly GraphQLSubscriptionClient _client;
            private readonly string _subscriptionQuery;
            private readonly CancellationToken _cancellationToken;
            private bool _isSubscribed;

            public AsyncSubscriptionEnumerator(GraphQLSubscriptionClient client, string subscriptionQuery, CancellationToken cancellationToken)
            {
                _client = client;
                _subscriptionQuery = subscriptionQuery;
                _cancellationToken = cancellationToken;
            }

            /// <summary>
            /// Gets the current element in the iteration.
            /// </summary>
            public T Current { get; private set; } = default!;

            /// <summary>
            /// Advances the enumerator asynchronously to the next element of the collection.
            /// </summary>
            /// <returns>
            /// A ValueTask that will complete with a result of true if the enumerator successfully advanced
            /// to the next element, or false if the enumerator has passed the end of the collection.
            /// </returns>
            /// <exception cref="InvalidOperationException">Failed to deserialize subscription data</exception>
            public async ValueTask<bool> MoveNextAsync()
            {
                if (!_isSubscribed)
                {
                    await SubscribeAsync();
                    _isSubscribed = true;
                }

                while (!_cancellationToken.IsCancellationRequested)
                {
                    var (messageType, payload) = await _client.ReceiveMessageAsync();

                    switch (messageType.ToLowerInvariant())
                    {
                        case "next":
                            if (payload != null)
                            {
                                Current = JsonSerializer.Deserialize<T>(payload.ToJsonString(),
                                              SerializationUtils.GetJsonSerializerOptions())
                                          ?? throw new InvalidOperationException("Failed to deserialize subscription data");
                                return true;
                            }
                            break;
                        case "complete":
                            return false;
                        case "error":
                            throw new InvalidOperationException($"Subscription error: {payload?.ToJsonString()}");
                    }
                }

                return false;
            }

            /// <summary>
            /// Sends the subscription request to the GraphQL server.
            /// </summary>
            private Task SubscribeAsync()
            {
                var subscribeMessage = new
                {
                    id = Guid.NewGuid().ToString(),
                    type = "subscribe",
                    payload = new { query = _subscriptionQuery },
                };

                return _client.SendMessageAsync(subscribeMessage);
            }

            /// <summary>
            /// Disposes the enumerator.
            /// </summary>
            /// <remarks>
            /// This method does not dispose the underlying GraphQLSubscriptionClient,
            /// as it may be shared across multiple enumerators.
            /// </remarks>
            public ValueTask DisposeAsync()
            {
                return ValueTask.CompletedTask;
            }
        }
    }
}
