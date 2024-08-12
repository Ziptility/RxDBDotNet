using System.Text.Json;
using GraphQlClientGenerator;
using LiveDocs.GraphQLApi.Models.Shared;

namespace RxDBDotNet.LiveDocsTestModelGenerator;

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

#pragma warning disable RCS1146 // Use conditional access. // This format is easier to understand
        if (valueType.Name == null
            || valueType.Name.EndsWith("Input", StringComparison.Ordinal)
            || valueType.Name is "SortEnumType" or "Upload" or "URL" or "UriHostNameType")
        {
            return GetFallbackFieldType(configuration, valueType, nullSuffix);
        }
#pragma warning restore RCS1146 // Use conditional access.

        if (valueType.Kind is GraphQlTypeKind.Scalar or GraphQlTypeKind.NonNull or GraphQlTypeKind.Enum)
        {
            return valueType.Name switch
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
                    NetTypeName =
                        $"{typeof(UserRole).Namespace}.{nameof(UserRole)}{nullSuffix}",
                    FormatMask = null,
                },
                _ => throw new InvalidOperationException($"The type {valueType.Name} must be mapped to the server-side type"),
            };
        }

        return GetFallbackFieldType(configuration, valueType, nullSuffix);
    }

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
