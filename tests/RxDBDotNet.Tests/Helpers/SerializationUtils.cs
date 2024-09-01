using System.Text.Json;
using System.Text.Json.Serialization;

namespace RxDBDotNet.Tests.Helpers;

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
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        };
    }

    public static Newtonsoft.Json.JsonSerializerSettings GetJsonSerializerSettings()
    {
        return new Newtonsoft.Json.JsonSerializerSettings
        {
            ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
            NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
            ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver(),
            Converters = { new Newtonsoft.Json.Converters.StringEnumConverter(new Newtonsoft.Json.Serialization.CamelCaseNamingStrategy()) },
            DateParseHandling = Newtonsoft.Json.DateParseHandling.None, // Not a direct equivalent but often useful
            MissingMemberHandling = Newtonsoft.Json.MissingMemberHandling.Ignore, // Commonly used option
        };
    }
}
