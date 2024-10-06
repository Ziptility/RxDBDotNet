// tests\RxDBDotNet.TestModelGenerator\ScalarFieldTypeMappingProvider.cs
using System.Text.Json;
using GraphQlClientGenerator;
using LiveDocs.GraphQLApi.Security;

namespace RxDBDotNet.TestModelGenerator;

internal sealed class ScalarFieldTypeMappingProvider : IScalarFieldTypeMappingProvider
{
    public ScalarFieldTypeDescription GetCustomScalarFieldType(
        GraphQlGeneratorConfiguration configuration,
        GraphQlType baseType,
        GraphQlTypeBase valueType,
        string valueName)
    {
        var nullSuffix = valueType.Kind != GraphQlTypeKind.NonNull ? "?" : string.Empty;
        valueType = valueType is GraphQlFieldType fieldType ? fieldType.UnwrapIfNonNull() : valueType;

        if (ShouldUseFallbackType(valueType))
        {
            return GetFallbackFieldType(configuration, valueType, nullSuffix);
        }

        if (valueType.Kind is GraphQlTypeKind.Scalar or GraphQlTypeKind.NonNull or GraphQlTypeKind.Enum)
        {
            return GetScalarTypeDescription(valueType, nullSuffix);
        }

        return GetFallbackFieldType(configuration, valueType, nullSuffix);
    }

    private static bool ShouldUseFallbackType(GraphQlTypeBase valueType) =>
        valueType.Name?.EndsWith("Input", StringComparison.Ordinal) != false
        || valueType.Name is "SortEnumType" or "Upload" or "URL" or "UriHostNameType";

    private static ScalarFieldTypeDescription GetScalarTypeDescription(GraphQlTypeBase valueType, string nullSuffix) =>
        valueType.Name switch
        {
            "Any" => new ScalarFieldTypeDescription
            {
                NetTypeName = typeof(JsonDocument).FullName + nullSuffix,
                FormatMask = null,
            },
            "JSON" => new ScalarFieldTypeDescription
            {
                NetTypeName = typeof(JsonElement).FullName + nullSuffix,
                FormatMask = null,
            },
            nameof(DateTime) => new ScalarFieldTypeDescription
            {
                NetTypeName = $"DateTimeOffset{nullSuffix}",
                FormatMask = null,
            },
            "Date" => new ScalarFieldTypeDescription
            {
                NetTypeName = $"DateOnly{nullSuffix}",
                FormatMask = null,
            },
            "UUID" => new ScalarFieldTypeDescription
            {
                NetTypeName = $"Guid{nullSuffix}",
                FormatMask = null,
            },
            nameof(String) or "EmailAddress" or "PhoneNumber" => new ScalarFieldTypeDescription
            {
                NetTypeName = $"string{nullSuffix}",
                FormatMask = null,
            },
            "Long" => new ScalarFieldTypeDescription
            {
                NetTypeName = $"long{nullSuffix}",
                FormatMask = null,
            },
            "NonNegativeInt" => new ScalarFieldTypeDescription
            {
                NetTypeName = $"int{nullSuffix}",
                FormatMask = null,
            },
            "Decimal" => new ScalarFieldTypeDescription
            {
                NetTypeName = $"decimal{nullSuffix}",
                FormatMask = null,
            },
            nameof(UserRole) => new ScalarFieldTypeDescription
            {
                NetTypeName = $"{typeof(UserRole).Namespace}.{nameof(UserRole)}{nullSuffix}",
                FormatMask = null,
            },
            _ => throw new InvalidOperationException($"The type {valueType.Name} must be mapped to the server-side type"),
        };

    private static ScalarFieldTypeDescription GetFallbackFieldType(
        GraphQlGeneratorConfiguration configuration,
        GraphQlTypeBase valueType,
        string nullSuffix)
    {
        valueType = (valueType as GraphQlFieldType)?.UnwrapIfNonNull() ?? valueType;
        if (valueType.Kind == GraphQlTypeKind.Enum)
        {
            return new ScalarFieldTypeDescription
            {
                NetTypeName = configuration.ClassPrefix + NamingHelper.ToPascalCase(valueType.Name) + configuration.ClassSuffix + nullSuffix,
            };
        }

        var dataType = string.Equals(valueType.Name, GraphQlTypeBase.GraphQlTypeScalarString, StringComparison.OrdinalIgnoreCase)
            ? "string"
            : "object";
        return new ScalarFieldTypeDescription
        {
            NetTypeName = AddQuestionMarkIfNullableReferencesEnabled(configuration, dataType, nullSuffix),
        };
    }

    private static string AddQuestionMarkIfNullableReferencesEnabled(
        GraphQlGeneratorConfiguration configuration,
        string dataTypeIdentifier,
        string nullSuffix)
    {
        return configuration.CSharpVersion == CSharpVersion.NewestWithNullableReferences ? dataTypeIdentifier + nullSuffix : dataTypeIdentifier;
    }
}
