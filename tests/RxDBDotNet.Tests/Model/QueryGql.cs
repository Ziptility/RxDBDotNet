using System.Text.Json.Serialization;

namespace RxDBDotNet.Tests.Model;

public partial class QueryGql
{
    [JsonPropertyName("__typename")]
    public string? TypeName { get; init; }
}
