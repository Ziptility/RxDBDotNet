using System.Net.Http.Headers;
using Newtonsoft.Json;
using RxDBDotNet.Tests.Model;
using static RxDBDotNet.Tests.Utils.SerializationUtils;
using Formatting = RxDBDotNet.Tests.Model.Formatting;
using GraphQLRequest = RxDBDotNet.Tests.Model.GraphQLRequest;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace RxDBDotNet.Tests.Utils;

internal static class HttpClientExtensions
{
    /// <summary>
    ///     Send a POST request to the specified Uri as an asynchronous operation.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="queryBuilder">The GraphQL string.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="jwtAccessToken">The JWT access token, if required.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">queryBuilder</exception>
    public static Task<GqlQueryResponse> PostGqlQueryAsync(
        this HttpClient httpClient,
        QueryQueryBuilderGql queryBuilder,
        CancellationToken cancellationToken,
        string? jwtAccessToken = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(queryBuilder);

        return PostGqlQueryInternalAsync(httpClient, queryBuilder, cancellationToken, jwtAccessToken);
    }

    private static async Task<GqlQueryResponse> PostGqlQueryInternalAsync(
        HttpClient httpClient,
        IGraphQlQueryBuilder queryBuilder,
        CancellationToken cancellationToken,
        string? jwtAccessToken = null)
    {
        var response = await PostGqlQueryAsync(httpClient, queryBuilder, cancellationToken, jwtAccessToken);

        return await response.DeserializeHttpResponseAsync<GqlQueryResponse>();
    }

    private static async Task<HttpResponseMessage> PostGqlQueryAsync(
        this HttpClient httpClient,
        IGraphQlQueryBuilder queryBuilder,
        CancellationToken cancellationToken,
        string? jwtAccessToken = null)
    {
        var queryJson = queryBuilder.Build();

        var graphQlRequest = new GraphQLRequest
        {
            Query = queryJson,
        };

        var json = JsonSerializer.Serialize(graphQlRequest, GetJsonSerializerOptions());

        using var jsonContent = new StringContent(json, Encoding.UTF8, "application/json");

        // Set the Authorization header if the JWT access token is provided
        if (!string.IsNullOrWhiteSpace(jwtAccessToken))
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtAccessToken);
        }

        return await httpClient.PostAsync("/graphql", jsonContent, cancellationToken);
    }

    /// <summary>
    ///     Sends a GraphQL mutation request to the specified Uri.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="mutationBuilder">The GraphQL mutation to execute.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="jwtAccessToken">The JWT access token, if required.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="httpClient" /> or
    ///     <paramref name="mutationBuilder" /> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="httpClient" /> is in an invalid state.</exception>
    public static Task<GqlMutationResponse> PostGqlMutationAsync(
        this HttpClient httpClient,
        MutationQueryBuilderGql mutationBuilder,
        CancellationToken cancellationToken,
        string? jwtAccessToken = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        ArgumentNullException.ThrowIfNull(mutationBuilder);

        return PostGqlMutationInternalAsync(httpClient, mutationBuilder, cancellationToken, jwtAccessToken);
    }

    private static async Task<GqlMutationResponse> PostGqlMutationInternalAsync(
        HttpClient httpClient,
        MutationQueryBuilderGql mutationBuilder,
        CancellationToken cancellationToken,
        string? jwtAccessToken = null)
    {
        var graphQlRequest = new GraphQLRequest
        {
            Query = mutationBuilder.Build(Formatting.Indented),
        };

        var json = JsonSerializer.Serialize(graphQlRequest, GetJsonSerializerOptions());

        using var stringContent = new StringContent(json, Encoding.UTF8, "application/json");

        // Set the Authorization header if the JWT access token is provided
        if (!string.IsNullOrWhiteSpace(jwtAccessToken))
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtAccessToken);
        }

        var response = await httpClient.PostAsync("graphql", stringContent, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(responseContent);
        }

        return await response.DeserializeHttpResponseAsync<GqlMutationResponse>();
    }

    private static async Task<T> DeserializeHttpResponseAsync<T>(this HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();

        return JsonConvert.DeserializeObject<T>(json, GetJsonSerializerSettings())
               ?? throw new InvalidOperationException($"The response of type {typeof(T).Name} was null.");
    }
}
