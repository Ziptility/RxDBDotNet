using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using RxDBDotNet.Tests.Model;
using static RxDBDotNet.Tests.Helpers.SerializationUtils;
using BadRequest = RxDBDotNet.Tests.Model.BadRequest;
using GraphQLRequest = RxDBDotNet.Tests.Model.GraphQLRequest;

namespace RxDBDotNet.Tests.Helpers;

internal static class HttpClientExtensions
{
    /// <summary>
    ///     Get an instance from a json response.
    /// </summary>
    /// <typeparam name="TResponse">The type of the t response.</typeparam>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="uri">The URI.</param>
    /// <param name="expectedStatusCode">The expected status code.</param>
    /// <returns>A Task&lt;T&gt; representing the asynchronous operation.</returns>
    private static async Task<TResponse> GetFromJsonAsync<TResponse>(
        this HttpClient httpClient,
        string uri,
        HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
    {
        ThrowIfInvalidParams(
            httpClient,
            uri);

        using var response = await httpClient.GetAsync(
            new Uri(uri),
            HttpCompletionOption.ResponseHeadersRead);

        response.StatusCode.Should()
            .Be(expectedStatusCode);

        return await response.DeserializeHttpResponseAsync<TResponse>();
    }

    /// <summary>
    ///     Send a POST request to the specified Uri as an asynchronous operation.
    /// </summary>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="uri">The URI.</param>
    /// <param name="expectedStatusCode">The expected status code.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">value</exception>
    /// <exception cref="InvalidOperationException"></exception>
    public static Task<TResponse> PostWithNoContent<TResponse>(
        this HttpClient httpClient,
        string uri,
        HttpStatusCode expectedStatusCode = HttpStatusCode.Created)
    {
        ThrowIfInvalidParams(
            httpClient,
            uri);

        return PostWithNoContentInternalAsync<TResponse>(
            httpClient,
            uri,
            expectedStatusCode);
    }

    private static async Task<TResponse> PostWithNoContentInternalAsync<TResponse>(
        HttpClient httpClient,
        string uri,
        HttpStatusCode expectedStatusCode)
    {
        using var response = await httpClient.PostAsync(
            uri,
            null);

        if (expectedStatusCode != HttpStatusCode.BadRequest
            && response.StatusCode == HttpStatusCode.BadRequest)
        {
            var badRequest = await response.DeserializeHttpResponseAsync<BadRequestResult>();
            throw new InvalidOperationException(
                string.Join(
                    ", ",
                    badRequest));
        }

        response.StatusCode.Should()
            .Be(expectedStatusCode);

        var deserializedPostResponse = await response.DeserializeHttpResponseAsync<TResponse>();

        if (expectedStatusCode == HttpStatusCode.Created)
        {
            var locationHeader = response.Headers.Location;
            locationHeader.Should()
                .NotBeNull();
            Debug.Assert(
                locationHeader != null,
                nameof(locationHeader) + " != null");
            var getResponse =
                await httpClient.GetFromJsonAsync<TResponse>(locationHeader.AbsoluteUri);
            AssertionOptions.FormattingOptions.MaxDepth = 100;
            getResponse.Should()
                .BeEquivalentTo(
                    deserializedPostResponse,
                    options => options.ComparingByMembers<TResponse>());
        }

        return deserializedPostResponse;
    }

    /// <summary>
    ///     Send a POST request to the specified Uri as an asynchronous operation.
    /// </summary>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="uri">The URI.</param>
    /// <param name="value">The value.</param>
    /// <param name="expectedStatusCode">The expected status code.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">value</exception>
    /// <exception cref="InvalidOperationException"></exception>
    public static Task<TResponse> PostAsJsonAsync<TResponse>(
        this HttpClient httpClient,
        string uri,
        object value,
        HttpStatusCode expectedStatusCode = HttpStatusCode.Created)
    {
        ThrowIfInvalidParams(
            httpClient,
            uri);

        ArgumentNullException.ThrowIfNull(value);

        return PostAsJsonInternalAsync<TResponse>(
            httpClient,
            uri,
            value,
            expectedStatusCode);
    }

    private static async Task<TResponse> PostAsJsonInternalAsync<TResponse>(
        HttpClient httpClient,
        string uri,
        object value,
        HttpStatusCode expectedStatusCode)
    {
        var json = JsonSerializer.Serialize(
            value,
            GetJsonSerializerOptions());

        using var stringContent = new StringContent(
            json,
            Encoding.UTF8,
            "application/json");

        var response = await httpClient.PostAsync(
            uri,
            stringContent);

        if (expectedStatusCode != HttpStatusCode.BadRequest
            && response.StatusCode == HttpStatusCode.BadRequest)
        {
            var badRequest = await response.DeserializeHttpResponseAsync<BadRequest>();
            throw new InvalidOperationException(badRequest.ToString());
        }

        response.StatusCode.Should()
            .Be(expectedStatusCode);

        var deserializedPostResponse = await response.DeserializeHttpResponseAsync<TResponse>();

        if (expectedStatusCode == HttpStatusCode.Created)
        {
            var locationHeader = response.Headers.Location;
            locationHeader.Should()
                .NotBeNull();
            Debug.Assert(
                locationHeader != null,
                nameof(locationHeader) + " != null");
            var getResponse =
                await httpClient.GetFromJsonAsync<TResponse>(locationHeader.AbsoluteUri);
            AssertionOptions.FormattingOptions.MaxDepth = 100;
            getResponse.Should()
                .BeEquivalentTo(
                    deserializedPostResponse,
                    options => options.ComparingByMembers<TResponse>());
        }

        return deserializedPostResponse;
    }

    /// <summary>
    ///     Send a POST request to the specified Uri as an asynchronous operation.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="queryBuilder">The graph ql string.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">queryBuilder</exception>
    public static Task<GqlQueryResponse> PostGqlQueryAsync(
        this HttpClient httpClient,
        QueryQueryBuilderGql queryBuilder)
    {
        ThrowIfInvalidParams(
            httpClient,
            Routes.GraphQL);

        ArgumentNullException.ThrowIfNull(queryBuilder);

        return PostGqlQueryInternalAsync(
            httpClient,
            queryBuilder);
    }

    private static async Task<GqlQueryResponse> PostGqlQueryInternalAsync(
        HttpClient httpClient,
        IGraphQlQueryBuilder queryBuilder)
    {
        var response = await PostGqlQueryAsync(httpClient, queryBuilder);

        return await response.DeserializeHttpResponseAsync<GqlQueryResponse>();
    }

    /// <summary>
    ///     Get the raw json response.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="uri">The URI.</param>
    /// <param name="expectedStatusCode">The expected status code.</param>
    public static async Task<string> GetJsonAsync(
        this HttpClient httpClient,
        string uri,
        HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
    {
        ThrowIfInvalidParams(
            httpClient,
            uri);

        using var response = await httpClient.GetAsync(
            new Uri(uri, UriKind.Relative),
            HttpCompletionOption.ResponseHeadersRead);

        response.StatusCode.Should()
            .Be(expectedStatusCode);

        return await response.Content.ReadAsStringAsync();
    }

    public static async Task<string> GetJsonAsync(this HttpClient httpClient, IGraphQlQueryBuilder queryBuilder)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(queryBuilder);

        var response = await PostGqlQueryAsync(httpClient, queryBuilder);

        return await response.Content.ReadAsStringAsync();
    }

    private static async Task<HttpResponseMessage> PostGqlQueryAsync(this HttpClient httpClient, IGraphQlQueryBuilder queryBuilder)
    {
        var queryJson = queryBuilder.Build();

        var graphQlRequest = new GraphQLRequest
        {
            Query = queryJson,
        };

        var json = JsonSerializer.Serialize(graphQlRequest, GetJsonSerializerOptions());

        using var jsonContent = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync("/graphql", jsonContent);

        if (!response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
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
    /// <param name="expectSuccess">Whether the request is expected to be successful.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">httpClient</exception>
    /// <exception cref="ArgumentNullException">mutationBuilder</exception>
    /// <exception cref="InvalidOperationException">httpClient</exception>
    public static Task<GqlMutationResponse> PostGqlMutationAsync(
        this HttpClient httpClient,
        MutationQueryBuilderGql mutationBuilder,
        bool expectSuccess = true)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        ArgumentNullException.ThrowIfNull(mutationBuilder);

        return PostGqlMutationInternalAsync(
            httpClient,
            mutationBuilder,
            expectSuccess);
    }

    private static async Task<GqlMutationResponse> PostGqlMutationInternalAsync(
        HttpClient httpClient,
        MutationQueryBuilderGql mutationBuilder,
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
            stringContent);

        if (expectSuccess && !response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(responseContent);
        }

        if (expectSuccess)
        {
            response.StatusCode.Should()
                .Be(HttpStatusCode.OK);
        }

        return await response.DeserializeHttpResponseAsync<GqlMutationResponse>();
    }

    /// <summary>
    ///     Send a PUT request to the specified Uri as an asynchronous operation.
    /// </summary>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="uri">The URI.</param>
    /// <param name="value">The value.</param>
    /// <param name="expectedStatusCode">The expected status code.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">value</exception>
    /// <exception cref="InvalidOperationException"></exception>
    public static Task<TResponse> PutAsJsonAsync<TResponse>(
        this HttpClient httpClient,
        string uri,
        object value,
        HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
    {
        ThrowIfInvalidParams(
            httpClient,
            uri);

        ArgumentNullException.ThrowIfNull(value);

        return PutAsJsonInternalAsync<TResponse>(
            httpClient,
            uri,
            value,
            expectedStatusCode);
    }

    private static async Task<TResponse> PutAsJsonInternalAsync<TResponse>(
        HttpClient httpClient,
        string uri,
        object value,
        HttpStatusCode expectedStatusCode)
    {
        var json = JsonSerializer.Serialize(
            value,
            GetJsonSerializerOptions());

        using var stringContent = new StringContent(
            json,
            Encoding.UTF8,
            "application/json");

        var response = await httpClient.PutAsync(
            uri,
            stringContent);

        if (expectedStatusCode != HttpStatusCode.BadRequest
            && response.StatusCode == HttpStatusCode.BadRequest)
        {
            var badRequest = await response.DeserializeHttpResponseAsync<BadRequest>();
            throw new InvalidOperationException(badRequest.ToString());
        }

        response.StatusCode.Should()
            .Be(expectedStatusCode);

        return await response.DeserializeHttpResponseAsync<TResponse>();
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
