using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Newtonsoft.Json.Linq;

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

        /// <summary>
        /// Initializes a new instance of the GraphQLSubscriptionClient class.
        /// </summary>
        /// <param name="webSocket">An already connected WebSocket instance.</param>
        public GraphQLSubscriptionClient(WebSocket webSocket)
        {
            _webSocket = webSocket ?? throw new ArgumentNullException(nameof(webSocket));
            _cts = new CancellationTokenSource();
        }

        /// <summary>
        /// Initializes the GraphQL subscription connection.
        /// This method should be called immediately after creating the client instance.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the connection initialization fails.</exception>
        public async Task InitializeAsync()
        {
            var initMessage = new
            {
                type = "connection_init",
            };
            await SendMessageAsync(initMessage);

            var response = await ReceiveMessageAsync();
            if (!string.Equals(response.Type, "connection_ack", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Failed to initialize GraphQL subscription connection. Received message type: {response.Type}");
            }
        }

        /// <summary>
        /// Subscribes to a GraphQL subscription.
        /// </summary>
        /// <typeparam name="TResponse">The type of the subscription response.</typeparam>
        /// <param name="subscriptionQuery">The GraphQL subscription query.</param>
        /// <param name="variables">Optional variables for the subscription query.</param>
        /// <returns>An IAsyncEnumerable of subscription responses.</returns>
        public async Task<IAsyncEnumerable<TResponse>> SubscribeAsync<TResponse>(string subscriptionQuery, object? variables = null)
        {
            var subscribeMessage = new
            {
                id = Guid.NewGuid().ToString(),
                type = "subscribe",
                payload = new
                {
                    query = subscriptionQuery,
                    variables,
                },
            };

            await SendMessageAsync(subscribeMessage);

            return new AsyncSubscriptionEnumerable<TResponse>(_webSocket, _cts.Token);
        }

        private async Task SendMessageAsync(object message)
        {
            var json = JsonSerializer.Serialize(message);
            var buffer = Encoding.UTF8.GetBytes(json);
            await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, _cts.Token);
        }

        private async Task<(string Type, JObject? Payload)> ReceiveMessageAsync()
        {
            var buffer = new byte[4096];
            var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                throw new InvalidOperationException("WebSocket connection closed unexpectedly");
            }

            var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
            var message = JObject.Parse(json);

            return (message["type"]?.Value<string>() ?? "", message["payload"] as JObject);
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

        private sealed class AsyncSubscriptionEnumerable<T> : IAsyncEnumerable<T>
        {
            private readonly WebSocket _webSocket;
            private readonly CancellationToken _cancellationToken;

            public AsyncSubscriptionEnumerable(WebSocket webSocket, CancellationToken cancellationToken)
            {
                _webSocket = webSocket;
                _cancellationToken = cancellationToken;
            }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken, cancellationToken);
                var combinedToken = cancellationTokenSource.Token;
                return new AsyncSubscriptionEnumerator<T>(_webSocket, combinedToken);
            }
        }

        private sealed class AsyncSubscriptionEnumerator<T> : IAsyncEnumerator<T>
        {
            private readonly WebSocket _webSocket;
            private readonly CancellationToken _cancellationToken;

            public AsyncSubscriptionEnumerator(WebSocket webSocket, CancellationToken cancellationToken)
            {
                _webSocket = webSocket;
                _cancellationToken = cancellationToken;
            }

            public T Current { get; private set; } = default!;

            public async ValueTask<bool> MoveNextAsync()
            {
                var buffer = new byte[4096];
                WebSocketReceiveResult result;

                do
                {
                    result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationToken);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        return false;
                    }

                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var message = JObject.Parse(json);

                    if (string.Equals(message["type"]?.Value<string>(), "data", StringComparison.OrdinalIgnoreCase))
                    {
                        var data = message["payload"]?["data"];
                        if (data != null)
                        {
                            Current = data.ToObject<T>() ?? throw new InvalidOperationException("Failed to deserialize subscription data");
                            return true;
                        }
                    }
                    else if (string.Equals(message["type"]?.Value<string>(), "complete", StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                } while (!result.EndOfMessage);

                return false;
            }

            public ValueTask DisposeAsync()
            {
                return ValueTask.CompletedTask;
            }
        }
    }
}
