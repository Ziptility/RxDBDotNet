using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace RxDBDotNet.Tests.Utils;

public static class SerializationUtils
{
    public static string Serialize<TValue>(this TValue instance)
    {
        return JsonSerializer.Serialize(instance, GetJsonSerializerOptions());
    }

    public static TValue? Deserialize<TValue>(this string json)
    {
        var result = JsonSerializer.Deserialize<TValue>(json, GetJsonSerializerOptions());
        if (!EqualityComparer<TValue?>.Default.Equals(result, default))
        {
            CheckRequiredProperties(result);
        }
        return result;
    }

    public static JsonSerializerOptions GetJsonSerializerOptions()
    {
        return new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver
            {
                Modifiers = { MakePropertiesOptional },
            },
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        };
    }

    private static void MakePropertiesOptional(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind == JsonTypeInfoKind.Object)
        {
            foreach (var propertyInfo in typeInfo.Properties)
            {
                propertyInfo.IsRequired = false;
            }
        }
    }

    private static void CheckRequiredProperties(object? obj, string path = "")
    {
        if (obj == null)
        {
            return;
        }

        var type = obj.GetType();
        foreach (var property in type.GetProperties())
        {
            var value = property.GetValue(obj);
            var propertyPath = string.IsNullOrEmpty(path) ? property.Name : $"{path}.{property.Name}";

            if (property.GetCustomAttribute<RequiredMemberAttribute>() != null)
            {
                // Check if the property type is nullable
                var isNullable = Nullable.GetUnderlyingType(property.PropertyType) != null
                    || property.PropertyType.IsClass
                    || property.GetCustomAttribute<NullableAttribute>() != null;

                // Only throw an exception if the property is not nullable and the value is null
                if (!isNullable && value == null)
                {
                    throw new JsonException($"Required non-nullable property {propertyPath} is null after deserialization.");
                }
            }

            // Recursively check complex types
            if (value != null && ShouldRecurse(property.PropertyType))
            {
                // Check if it's an enumerable
                if (typeof(System.Collections.IEnumerable).IsAssignableFrom(property.PropertyType) && property.PropertyType != typeof(string))
                {
                    var enumerable = (System.Collections.IEnumerable)value;
                    var index = 0;
                    foreach (var item in enumerable)
                    {
                        if (item != null && ShouldRecurse(item.GetType()))
                        {
                            CheckRequiredProperties(item, $"{propertyPath}[{index}]");
                        }
                        index++;
                    }
                }
                else
                {
                    // It's a complex type, recurse
                    CheckRequiredProperties(value, propertyPath);
                }
            }
        }
    }

    private static bool ShouldRecurse(Type type)
    {
        // Check if the type is a value type (struct) or a reference type (class)
        if (type.IsValueType)
        {
            // For value types, only recurse if it's not a primitive, not a enum, and not one of the known structs
            return !type.IsPrimitive
                && !type.IsEnum
                && type != typeof(DateTime)
                && type != typeof(DateTimeOffset)
                && type != typeof(TimeSpan)
                && type != typeof(Guid)
                && type != typeof(decimal);
        }

        // For reference types, recurse if it's not a string
        return type != typeof(string);
    }
}
