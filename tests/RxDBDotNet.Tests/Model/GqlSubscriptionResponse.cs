using System.Text.Json.Serialization;

namespace RxDBDotNet.Tests.Model
{
    public class GqlSubscriptionResponse
    {
        [JsonPropertyName("data")]
        public SubscriptionGql? Data { get; init; }

        [JsonPropertyName("errors")]
        public ICollection<GraphQlQueryError>? Errors { get; init; }
    }
}
