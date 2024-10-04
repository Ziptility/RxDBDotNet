// tests\RxDBDotNet.Tests\Model\GraphQLRequest.cs
namespace RxDBDotNet.Tests.Model;

public readonly record struct GraphQLRequest
{
    public string Query { get; init; }
}
