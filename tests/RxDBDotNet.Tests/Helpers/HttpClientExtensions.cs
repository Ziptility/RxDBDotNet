using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using RxDBDotNet.Tests.Model;
using static RxDBDotNet.Tests.Helpers.SerializationUtils;
using GraphQLRequest = RxDBDotNet.Tests.Model.GraphQLRequest;

namespace RxDBDotNet.Tests.Helpers;

internal static class HttpClientExtensions
{
    /// <summary>
    ///     Send a POST request to the specified Uri as an asynchronous operation.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="queryBuilder">The GraphQL string.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">queryBuilder</exception>
    public static Task<GqlQueryResponse> PostGqlQueryAsync(
        this HttpClient httpClient,
        QueryQueryBuilderGql queryBuilder,
        CancellationToken cancellationToken)
    {
        ThrowIfInvalidParams(
            httpClient,
            Routes.GraphQL);

        ArgumentNullException.ThrowIfNull(queryBuilder);

        return PostGqlQueryInternalAsync(
            httpClient,
            queryBuilder, cancellationToken);
    }

    private static async Task<GqlQueryResponse> PostGqlQueryInternalAsync(
        HttpClient httpClient,
        IGraphQlQueryBuilder queryBuilder,
        CancellationToken cancellationToken)
    {
        var response = await PostGqlQueryAsync(httpClient, queryBuilder, cancellationToken);

        return await response.DeserializeHttpResponseAsync<GqlQueryResponse>();
    }

    private static async Task<HttpResponseMessage> PostGqlQueryAsync(this HttpClient httpClient, IGraphQlQueryBuilder queryBuilder, CancellationToken cancellationToken)
    {
        var queryJson = queryBuilder.Build();

        var graphQlRequest = new GraphQLRequest
        {
            Query = queryJson,
        };

        var json = JsonSerializer.Serialize(graphQlRequest, GetJsonSerializerOptions());

        using var jsonContent = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync("/graphql", jsonContent, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(responseContent);
        }

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return response;
    }

    /// <summary>
    ///     Send a GraphQL mutation request to the specified Uri.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="mutationBuilder">The GraphQL mutation to execute.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="expectSuccess">Whether the request is expected to be successful.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">httpClient</exception>
    /// <exception cref="ArgumentNullException">mutationBuilder</exception>
    /// <exception cref="InvalidOperationException">httpClient</exception>
    public static Task<GqlMutationResponse> PostGqlMutationAsync(
        this HttpClient httpClient,
        MutationQueryBuilderGql mutationBuilder,
        CancellationToken cancellationToken,
        bool expectSuccess = true)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        ArgumentNullException.ThrowIfNull(mutationBuilder);

        return PostGqlMutationInternalAsync(
            httpClient,
            mutationBuilder,
            cancellationToken,
            expectSuccess);
    }

    private static async Task<GqlMutationResponse> PostGqlMutationInternalAsync(
        HttpClient httpClient,
        MutationQueryBuilderGql mutationBuilder,
        CancellationToken cancellationToken,
        bool expectSuccess = true)
    {
        var graphQlRequest = new GraphQLRequest
        {
            Query = mutationBuilder.Build(Formatting.Indented),
        };

        var json = JsonSerializer.Serialize(
            graphQlRequest,
            GetJsonSerializerOptions());

        using var stringContent = new StringContent(
            json,
            Encoding.UTF8,
            "application/json");

        var response = await httpClient.PostAsync(
            Routes.GraphQL,
            stringContent, cancellationToken);

        if (expectSuccess && !response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(responseContent);
        }

        if (expectSuccess)
        {
            response.StatusCode.Should()
                .Be(HttpStatusCode.OK);
        }

        return await response.DeserializeHttpResponseAsync<GqlMutationResponse>();
    }

    private static async Task<T> DeserializeHttpResponseAsync<T>(this HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<T>(
                   json,
                   GetJsonSerializerOptions())
               ?? throw new InvalidOperationException(
                   $"The response of type {typeof(T).Name} was null.");
    }

    private static void ThrowIfInvalidParams(HttpClient httpClient, string uri)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        if (string.IsNullOrWhiteSpace(uri))
        {
            throw new ArgumentException(
                "Can't be null or empty",
                nameof(uri));
        }
    }
}
