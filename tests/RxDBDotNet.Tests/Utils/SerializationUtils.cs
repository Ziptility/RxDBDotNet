using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace RxDBDotNet.Tests.Utils;

public static class SerializationUtils
{
    public static JsonSerializerOptions GetJsonSerializerOptions()
    {
        return new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
            },
        };
    }

    public static JsonSerializerSettings GetJsonSerializerSettings()
    {
        return new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Converters =
            {
                new StringEnumConverter(new CamelCaseNamingStrategy()),
            },
            DateParseHandling = DateParseHandling.None, // Not a direct equivalent but often useful
            MissingMemberHandling = MissingMemberHandling.Ignore, // Commonly used option
        };
    }
}
