// tests\RxDBDotNet.Tests\Model\GraphQLTestModel.cs
#pragma warning disable 8618

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
#if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#endif

namespace RxDBDotNet.Tests.Model
{
    #region base classes
    public struct GraphQlFieldMetadata
    {
        public string Name { get; set; }
        public string DefaultAlias { get; set; }
        public bool IsComplex { get; set; }
        public bool RequiresParameters { get; set; }
        public global::System.Type QueryBuilderType { get; set; }
    }
    
    public enum Formatting
    {
        None,
        Indented
    }
    
    public class GraphQlObjectTypeAttribute : global::System.Attribute
    {
        public string TypeName { get; }
    
        public GraphQlObjectTypeAttribute(string typeName) => TypeName = typeName;
    }
    
    #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
    public class QueryBuilderParameterConverter<T> : global::Newtonsoft.Json.JsonConverter
    {
        public override object ReadJson(JsonReader reader, global::System.Type objectType, object existingValue, JsonSerializer serializer)
        {
            switch (reader.TokenType)
            {
                case JsonToken.Null:
                    return null;
    
                default:
                    return (QueryBuilderParameter<T>)(T)serializer.Deserialize(reader, typeof(T));
            }
        }
    
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
                writer.WriteNull();
            else
                serializer.Serialize(writer, ((QueryBuilderParameter<T>)value).Value, typeof(T));
        }
    
        public override bool CanConvert(global::System.Type objectType) => objectType.IsSubclassOf(typeof(QueryBuilderParameter));
    }
    
    public class GraphQlInterfaceJsonConverter : global::Newtonsoft.Json.JsonConverter
    {
        private const string FieldNameType = "__typename";
    
        private static readonly Dictionary<string, global::System.Type> InterfaceTypeMapping =
            typeof(GraphQlInterfaceJsonConverter).Assembly.GetTypes()
                .Select(t => new { Type = t, Attribute = t.GetCustomAttribute<GraphQlObjectTypeAttribute>() })
                .Where(x => x.Attribute != null && x.Type.Namespace == typeof(GraphQlInterfaceJsonConverter).Namespace)
                .ToDictionary(x => x.Attribute.TypeName, x => x.Type);
    
        public override bool CanConvert(global::System.Type objectType) => objectType.IsInterface || objectType.IsArray;
    
        public override object ReadJson(JsonReader reader, global::System.Type objectType, object existingValue, JsonSerializer serializer)
        {
            while (reader.TokenType == JsonToken.Comment)
                reader.Read();
    
            switch (reader.TokenType)
            {
                case JsonToken.Null:
                    return null;
    
                case JsonToken.StartObject:
                    var jObject = JObject.Load(reader);
                    if (!jObject.TryGetValue(FieldNameType, out var token) || token.Type != JTokenType.String)
                        throw CreateJsonReaderException(reader, $"\"{GetType().FullName}\" requires JSON object to contain \"{FieldNameType}\" field with type name");
    
                    var typeName = token.Value<string>();
                    if (!InterfaceTypeMapping.TryGetValue(typeName, out var type))
                        throw CreateJsonReaderException(reader, $"type \"{typeName}\" not found");
    
                    using (reader = CloneReader(jObject, reader))
                        return serializer.Deserialize(reader, type);
    
                case JsonToken.StartArray:
                    var elementType = GetElementType(objectType);
                    if (elementType == null)
                        throw CreateJsonReaderException(reader, $"array element type could not be resolved for type \"{objectType.FullName}\"");
    
                    return ReadArray(reader, objectType, elementType, serializer);
    
                default:
                    throw CreateJsonReaderException(reader, $"unrecognized token: {reader.TokenType}");
            }
        }
    
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => serializer.Serialize(writer, value);
    
        private static JsonReader CloneReader(JToken jToken, JsonReader reader)
        {
            var jObjectReader = jToken.CreateReader();
            jObjectReader.Culture = reader.Culture;
            jObjectReader.CloseInput = reader.CloseInput;
            jObjectReader.SupportMultipleContent = reader.SupportMultipleContent;
            jObjectReader.DateTimeZoneHandling = reader.DateTimeZoneHandling;
            jObjectReader.FloatParseHandling = reader.FloatParseHandling;
            jObjectReader.DateFormatString = reader.DateFormatString;
            jObjectReader.DateParseHandling = reader.DateParseHandling;
            return jObjectReader;
        }
    
        private static JsonReaderException CreateJsonReaderException(JsonReader reader, string message)
        {
            if (reader is IJsonLineInfo lineInfo && lineInfo.HasLineInfo())
                return new JsonReaderException(message, reader.Path, lineInfo.LineNumber, lineInfo.LinePosition, null);
    
            return new JsonReaderException(message);
        }
    
        private static global::System.Type GetElementType(global::System.Type arrayOrGenericContainer) =>
            arrayOrGenericContainer.IsArray ? arrayOrGenericContainer.GetElementType() : arrayOrGenericContainer.GenericTypeArguments.FirstOrDefault();
    
        private IList ReadArray(JsonReader reader, global::System.Type targetType, global::System.Type elementType, JsonSerializer serializer)
        {
            var list = CreateCompatibleList(targetType, elementType);
            while (reader.Read() && reader.TokenType != JsonToken.EndArray)
                list.Add(ReadJson(reader, elementType, null, serializer));
    
            if (!targetType.IsArray)
                return list;
    
            var array = Array.CreateInstance(elementType, list.Count);
            list.CopyTo(array, 0);
            return array;
        }
    
        private static IList CreateCompatibleList(global::System.Type targetContainerType, global::System.Type elementType) =>
            (IList)Activator.CreateInstance(targetContainerType.IsArray || targetContainerType.IsAbstract ? typeof(List<>).MakeGenericType(elementType) : targetContainerType);
    }
    #endif
    
    internal static class GraphQlQueryHelper
    {
        private static readonly Regex RegexGraphQlIdentifier = new Regex(@"^[_A-Za-z][_0-9A-Za-z]*$", RegexOptions.Compiled);
        private static readonly Regex RegexEscapeGraphQlString = new Regex(@"[\\\""/\b\f\n\r\t]", RegexOptions.Compiled);
    
        public static string GetIndentation(int level, byte indentationSize)
        {
            return new String(' ', level * indentationSize);
        }
    
        public static string EscapeGraphQlStringValue(string value)
        {
            return RegexEscapeGraphQlString.Replace(value, m => @$"\{GetEscapeSequence(m.Value)}");
        }
    
        private static string GetEscapeSequence(string input)
        {
            switch (input)
            {
                case "\\":
                    return "\\";
                case "\"":
                    return "\"";
                case "/":
                    return "/";
                case "\b":
                    return "b";
                case "\f":
                    return "f";
                case "\n":
                    return "n";
                case "\r":
                    return "r";
                case "\t":
                    return "t";
                default:
                    throw new InvalidOperationException($"invalid character: {input}");
            }
        }
    
        public static string BuildArgumentValue(object value, string formatMask, GraphQlBuilderOptions options, int level)
        {
            var serializer = options.ArgumentBuilder ?? DefaultGraphQlArgumentBuilder.Instance;
            if (serializer.TryBuild(new GraphQlArgumentBuilderContext { Value = value, FormatMask = formatMask, Options = options, Level = level }, out var serializedValue))
                return serializedValue;
    
            if (value is null)
                return "null";
    
            var enumerable = value as IEnumerable;
            if (!String.IsNullOrEmpty(formatMask) && enumerable == null)
                return
                    value is IFormattable formattable
                        ? $"\"{EscapeGraphQlStringValue(formattable.ToString(formatMask, CultureInfo.InvariantCulture))}\""
                        : throw new ArgumentException($"Value must implement {nameof(IFormattable)} interface to use a format mask. ", nameof(value));
    
            if (value is Enum @enum)
                return ConvertEnumToString(@enum);
    
            if (value is bool @bool)
                return @bool ? "true" : "false";
    
            if (value is DateTime dateTime)
                return $"\"{dateTime.ToString("O")}\"";
    
            if (value is DateTimeOffset dateTimeOffset)
                return $"\"{dateTimeOffset.ToString("O")}\"";
    
            if (value is IGraphQlInputObject inputObject)
                return BuildInputObject(inputObject, options, level + 2);
    
            if (value is Guid)
                return $"\"{value}\"";
    
            if (value is String @string)
                return $"\"{EscapeGraphQlStringValue(@string)}\"";
    
            if (enumerable != null)
                return BuildEnumerableArgument(enumerable, formatMask, options, level, '[', ']');
    
            if (value is short || value is ushort || value is byte || value is int || value is uint || value is long || value is ulong || value is float || value is double || value is decimal)
                return Convert.ToString(value, CultureInfo.InvariantCulture);
    
            var argumentValue = EscapeGraphQlStringValue(Convert.ToString(value, CultureInfo.InvariantCulture));
            return $"\"{argumentValue}\"";
        }
    
        public static string BuildEnumerableArgument(IEnumerable enumerable, string formatMask, GraphQlBuilderOptions options, int level, char openingSymbol, char closingSymbol)
        {
            var builder = new StringBuilder();
            builder.Append(openingSymbol);
            var delimiter = String.Empty;
            foreach (var item in enumerable)
            {
                builder.Append(delimiter);
    
                if (options.Formatting == Formatting.Indented)
                {
                    builder.AppendLine();
                    builder.Append(GetIndentation(level + 1, options.IndentationSize));
                }
    
                builder.Append(BuildArgumentValue(item, formatMask, options, level));
                delimiter = ",";
            }
    
            builder.Append(closingSymbol);
            return builder.ToString();
        }
    
        public static string BuildInputObject(IGraphQlInputObject inputObject, GraphQlBuilderOptions options, int level)
        {
            var builder = new StringBuilder();
            builder.Append("{");
    
            var isIndentedFormatting = options.Formatting == Formatting.Indented;
            string valueSeparator;
            if (isIndentedFormatting)
            {
                builder.AppendLine();
                valueSeparator = ": ";
            }
            else
                valueSeparator = ":";
    
            var separator = String.Empty;
            foreach (var propertyValue in inputObject.GetPropertyValues())
            {
                var queryBuilderParameter = propertyValue.Value as QueryBuilderParameter;
                var value =
                    queryBuilderParameter?.Name != null
                        ? $"${queryBuilderParameter.Name}"
                        : BuildArgumentValue(queryBuilderParameter == null ? propertyValue.Value : queryBuilderParameter.Value, propertyValue.FormatMask, options, level);
    
                builder.Append(isIndentedFormatting ? GetIndentation(level, options.IndentationSize) : separator);
                builder.Append(propertyValue.Name);
                builder.Append(valueSeparator);
                builder.Append(value);
    
                separator = ",";
    
                if (isIndentedFormatting)
                    builder.AppendLine();
            }
    
            if (isIndentedFormatting)
                builder.Append(GetIndentation(level - 1, options.IndentationSize));
    
            builder.Append("}");
    
            return builder.ToString();
        }
    
        public static string BuildDirective(GraphQlDirective directive, GraphQlBuilderOptions options, int level)
        {
            if (directive == null)
                return String.Empty;
    
            var isIndentedFormatting = options.Formatting == Formatting.Indented;
            var indentationSpace = isIndentedFormatting ? " " : String.Empty;
            var builder = new StringBuilder();
            builder.Append(indentationSpace);
            builder.Append("@");
            builder.Append(directive.Name);
            builder.Append("(");
    
            string separator = null;
            foreach (var kvp in directive.Arguments)
            {
                var argumentName = kvp.Key;
                var argument = kvp.Value;
    
                builder.Append(separator);
                builder.Append(argumentName);
                builder.Append(":");
                builder.Append(indentationSpace);
    
                if (argument.Name == null)
                    builder.Append(BuildArgumentValue(argument.Value, null, options, level));
                else
                {
                    builder.Append("$");
                    builder.Append(argument.Name);
                }
    
                separator = isIndentedFormatting ? ", " : ",";
            }
    
            builder.Append(")");
            return builder.ToString();
        }
    
        public static void ValidateGraphQlIdentifier(string name, string identifier)
        {
            if (identifier != null && !RegexGraphQlIdentifier.IsMatch(identifier))
                throw new ArgumentException("value must match " + RegexGraphQlIdentifier, name);
        }
    
        private static string ConvertEnumToString(Enum @enum)
        {
            var enumMember = @enum.GetType().GetField(@enum.ToString());
            if (enumMember == null)
                throw new InvalidOperationException("enum member resolution failed");
    
            var enumMemberAttribute = (EnumMemberAttribute)enumMember.GetCustomAttribute(typeof(EnumMemberAttribute));
    
            return enumMemberAttribute == null
                ? @enum.ToString()
                : enumMemberAttribute.Value;
        }
    }
    
    public interface IGraphQlArgumentBuilder
    {
        bool TryBuild(GraphQlArgumentBuilderContext context, out string graphQlString);
    }
    
    public class GraphQlArgumentBuilderContext
    {
        public object Value { get; set; }
        public string FormatMask { get; set; }
        public GraphQlBuilderOptions Options { get; set; }
        public int Level { get; set; }
    }
    
    public class DefaultGraphQlArgumentBuilder : IGraphQlArgumentBuilder
    {
        private static readonly Regex RegexWhiteSpace = new Regex(@"\s", RegexOptions.Compiled);
    
        public static readonly DefaultGraphQlArgumentBuilder Instance = new();
    
        public bool TryBuild(GraphQlArgumentBuilderContext context, out string graphQlString)
        {
    #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
            if (context.Value is JValue jValue)
            {
                switch (jValue.Type)
                {
                    case JTokenType.Null:
                        graphQlString = "null";
                        return true;
    
                    case JTokenType.Integer:
                    case JTokenType.Float:
                    case JTokenType.Boolean:
                        graphQlString = GraphQlQueryHelper.BuildArgumentValue(jValue.Value, null, context.Options, context.Level);
                        return true;
    
                    case JTokenType.String:
                        graphQlString = $"\"{GraphQlQueryHelper.EscapeGraphQlStringValue((string)jValue.Value)}\"";
                        return true;
    
                    default:
                        graphQlString = $"\"{jValue.Value}\"";
                        return true;
                }
            }
    
            if (context.Value is JProperty jProperty)
            {
                if (RegexWhiteSpace.IsMatch(jProperty.Name))
                    throw new ArgumentException($"JSON object keys used as GraphQL arguments must not contain whitespace; key: {jProperty.Name}");
    
                graphQlString = $"{jProperty.Name}:{(context.Options.Formatting == Formatting.Indented ? " " : null)}{GraphQlQueryHelper.BuildArgumentValue(jProperty.Value, null, context.Options, context.Level)}";
                return true;
            }
    
            if (context.Value is JObject jObject)
            {
                graphQlString = GraphQlQueryHelper.BuildEnumerableArgument(jObject, null, context.Options, context.Level + 1, '{', '}');
                return true;
            }
    #endif
    
            graphQlString = null;
            return false;
        }
    }
    
    internal struct InputPropertyInfo
    {
        public string Name { get; set; }
        public object Value { get; set; }
        public string FormatMask { get; set; }
    }
    
    internal interface IGraphQlInputObject
    {
        IEnumerable<InputPropertyInfo> GetPropertyValues();
    }
    
    public interface IGraphQlQueryBuilder
    {
        void Clear();
        void IncludeAllFields();
        string Build(Formatting formatting = Formatting.None, byte indentationSize = 2);
    }
    
    public struct QueryBuilderArgumentInfo
    {
        public string ArgumentName { get; set; }
        public QueryBuilderParameter ArgumentValue { get; set; }
        public string FormatMask { get; set; }
    }
    
    public abstract class QueryBuilderParameter
    {
        private string _name;
    
        internal string GraphQlTypeName { get; }
        internal object Value { get; set; }
    
        public string Name
        {
            get => _name;
            set
            {
                GraphQlQueryHelper.ValidateGraphQlIdentifier(nameof(Name), value);
                _name = value;
            }
        }
    
        protected QueryBuilderParameter(string name, string graphQlTypeName, object value)
        {
            Name = name?.Trim();
            GraphQlTypeName = graphQlTypeName?.Replace(" ", null).Replace("\t", null).Replace("\n", null).Replace("\r", null);
            Value = value;
        }
    
        protected QueryBuilderParameter(object value) => Value = value;
    }
    
    public class QueryBuilderParameter<T> : QueryBuilderParameter
    {
        public new T Value
        {
            get => base.Value == null ? default : (T)base.Value;
            set => base.Value = value;
        }
    
        protected QueryBuilderParameter(string name, string graphQlTypeName, T value) : base(name, graphQlTypeName, value)
        {
            EnsureGraphQlTypeName(graphQlTypeName);
        }
    
        protected QueryBuilderParameter(string name, string graphQlTypeName) : base(name, graphQlTypeName, null)
        {
            EnsureGraphQlTypeName(graphQlTypeName);
        }
    
        private QueryBuilderParameter(T value) : base(value)
        {
        }
    
        public void ResetValue() => base.Value = null;
    
        public static implicit operator QueryBuilderParameter<T>(T value) => new QueryBuilderParameter<T>(value);
    
        public static implicit operator T(QueryBuilderParameter<T> parameter) => parameter.Value;
    
        private static void EnsureGraphQlTypeName(string graphQlTypeName)
        {
            if (String.IsNullOrWhiteSpace(graphQlTypeName))
                throw new ArgumentException("value required", nameof(graphQlTypeName));
        }
    }
    
    public class GraphQlQueryParameter<T> : QueryBuilderParameter<T>
    {
        private string _formatMask;
    
        public string FormatMask
        {
            get => _formatMask;
            set => _formatMask =
                typeof(IFormattable).IsAssignableFrom(typeof(T))
                    ? value
                    : throw new InvalidOperationException($"Value must be of {nameof(IFormattable)} type. ");
        }
    
        public GraphQlQueryParameter(string name, string graphQlTypeName = null)
            : base(name, graphQlTypeName ?? GetGraphQlTypeName(typeof(T)))
        {
        }
    
        public GraphQlQueryParameter(string name, string graphQlTypeName, T defaultValue)
            : base(name, graphQlTypeName, defaultValue)
        {
        }
    
        public GraphQlQueryParameter(string name, T defaultValue, bool isNullable = true)
            : base(name, GetGraphQlTypeName(typeof(T), isNullable), defaultValue)
        {
        }
    
        private static string GetGraphQlTypeName(global::System.Type valueType, bool isNullable)
        {
            var graphQlTypeName = GetGraphQlTypeName(valueType);
            if (!isNullable)
                graphQlTypeName += "!";
    
            return graphQlTypeName;
        }
    
        private static string GetGraphQlTypeName(global::System.Type valueType)
        {
            var nullableUnderlyingType = Nullable.GetUnderlyingType(valueType);
            valueType = nullableUnderlyingType ?? valueType;
    
            if (valueType.IsArray)
            {
                var arrayItemType = GetGraphQlTypeName(valueType.GetElementType());
                return arrayItemType == null ? null : "[" + arrayItemType + "]";
            }
    
            if (typeof(IEnumerable).IsAssignableFrom(valueType))
            {
                var genericArguments = valueType.GetGenericArguments();
                if (genericArguments.Length == 1)
                {
                    var listItemType = GetGraphQlTypeName(valueType.GetGenericArguments()[0]);
                    return listItemType == null ? null : "[" + listItemType + "]";
                }
            }
    
            if (GraphQlTypes.ReverseMapping.TryGetValue(valueType, out var graphQlTypeName))
                return graphQlTypeName;
    
            if (valueType == typeof(string))
                return "String";
    
            var nullableSuffix = nullableUnderlyingType == null ? null : "?";
            graphQlTypeName = GetValueTypeGraphQlTypeName(valueType);
            return graphQlTypeName == null ? null : graphQlTypeName + nullableSuffix;
        }
    
        private static string GetValueTypeGraphQlTypeName(global::System.Type valueType)
        {
            if (valueType == typeof(bool))
                return "Boolean";
    
            if (valueType == typeof(float) || valueType == typeof(double) || valueType == typeof(decimal))
                return "Float";
    
            if (valueType == typeof(Guid))
                return "ID";
    
            if (valueType == typeof(sbyte) || valueType == typeof(byte) || valueType == typeof(short) || valueType == typeof(ushort) || valueType == typeof(int) || valueType == typeof(uint) ||
                valueType == typeof(long) || valueType == typeof(ulong))
                return "Int";
    
            return null;
        }
    }
    
    public abstract class GraphQlDirective
    {
        private readonly Dictionary<string, QueryBuilderParameter> _arguments = new Dictionary<string, QueryBuilderParameter>();
    
        internal IEnumerable<KeyValuePair<string, QueryBuilderParameter>> Arguments => _arguments;
    
        public string Name { get; }
    
        protected GraphQlDirective(string name)
        {
            GraphQlQueryHelper.ValidateGraphQlIdentifier(nameof(name), name);
            Name = name;
        }
    
        protected void AddArgument(string name, QueryBuilderParameter value)
        {
            if (value != null)
                _arguments[name] = value;
        }
    }
    
    public class GraphQlBuilderOptions
    {
        public Formatting Formatting { get; set; }
        public byte IndentationSize { get; set; } = 2;
        public IGraphQlArgumentBuilder ArgumentBuilder { get; set; }
    }
    
    public abstract partial class GraphQlQueryBuilder : IGraphQlQueryBuilder
    {
        private readonly Dictionary<string, GraphQlFieldCriteria> _fieldCriteria = new Dictionary<string, GraphQlFieldCriteria>();
    
        private readonly string _operationType;
        private readonly string _operationName;
        private Dictionary<string, GraphQlFragmentCriteria> _fragments;
        private List<QueryBuilderArgumentInfo> _queryParameters;
    
        protected abstract string TypeName { get; }
    
        public abstract IReadOnlyList<GraphQlFieldMetadata> AllFields { get; }
    
        protected GraphQlQueryBuilder(string operationType, string operationName)
        {
            GraphQlQueryHelper.ValidateGraphQlIdentifier(nameof(operationName), operationName);
            _operationType = operationType;
            _operationName = operationName;
        }
    
        public virtual void Clear()
        {
            _fieldCriteria.Clear();
            _fragments?.Clear();
            _queryParameters?.Clear();
        }
    
        void IGraphQlQueryBuilder.IncludeAllFields()
        {
            IncludeAllFields();
        }
    
        public string Build(Formatting formatting = Formatting.None, byte indentationSize = 2)
        {
            return Build(new GraphQlBuilderOptions { Formatting = formatting, IndentationSize = indentationSize });
        }
    
        public string Build(GraphQlBuilderOptions options)
        {
            return Build(options, 1);
        }
    
        protected void IncludeAllFields()
        {
            IncludeFields(AllFields.Where(f => !f.RequiresParameters));
        }
    
        protected virtual string Build(GraphQlBuilderOptions options, int level)
        {
            var isIndentedFormatting = options.Formatting == Formatting.Indented;
            var separator = String.Empty;
            var indentationSpace = isIndentedFormatting ? " " : String.Empty;
            var builder = new StringBuilder();
    
            BuildOperationSignature(builder, options, indentationSpace, level);
    
            if (builder.Length > 0 || level > 1)
                builder.Append(indentationSpace);
    
            builder.Append("{");
    
            if (isIndentedFormatting)
                builder.AppendLine();
    
            separator = String.Empty;
    
            foreach (var criteria in _fieldCriteria.Values.Concat(_fragments?.Values ?? Enumerable.Empty<GraphQlFragmentCriteria>()))
            {
                var fieldCriteria = criteria.Build(options, level);
                if (isIndentedFormatting)
                    builder.AppendLine(fieldCriteria);
                else if (!String.IsNullOrEmpty(fieldCriteria))
                {
                    builder.Append(separator);
                    builder.Append(fieldCriteria);
                }
    
                separator = ",";
            }
    
            if (isIndentedFormatting)
                builder.Append(GraphQlQueryHelper.GetIndentation(level - 1, options.IndentationSize));
    
            builder.Append("}");
    
            return builder.ToString();
        }
    
        private void BuildOperationSignature(StringBuilder builder, GraphQlBuilderOptions options, string indentationSpace, int level)
        {
            if (String.IsNullOrEmpty(_operationType))
                return;
    
            builder.Append(_operationType);
    
            if (!String.IsNullOrEmpty(_operationName))
            {
                builder.Append(" ");
                builder.Append(_operationName);
            }
    
            if (_queryParameters?.Count > 0)
            {
                builder.Append(indentationSpace);
                builder.Append("(");
    
                var separator = String.Empty;
                var isIndentedFormatting = options.Formatting == Formatting.Indented;
    
                foreach (var queryParameterInfo in _queryParameters)
                {
                    if (isIndentedFormatting)
                    {
                        builder.AppendLine(separator);
                        builder.Append(GraphQlQueryHelper.GetIndentation(level, options.IndentationSize));
                    }
                    else
                        builder.Append(separator);
    
                    builder.Append("$");
                    builder.Append(queryParameterInfo.ArgumentValue.Name);
                    builder.Append(":");
                    builder.Append(indentationSpace);
    
                    builder.Append(queryParameterInfo.ArgumentValue.GraphQlTypeName);
    
                    if (!queryParameterInfo.ArgumentValue.GraphQlTypeName.EndsWith("!") && queryParameterInfo.ArgumentValue.Value is not null)
                    {
                        builder.Append(indentationSpace);
                        builder.Append("=");
                        builder.Append(indentationSpace);
                        builder.Append(GraphQlQueryHelper.BuildArgumentValue(queryParameterInfo.ArgumentValue.Value, queryParameterInfo.FormatMask, options, 0));
                    }
    
                    if (!isIndentedFormatting)
                        separator = ",";
                }
    
                builder.Append(")");
            }
        }
    
        protected void IncludeScalarField(string fieldName, string alias, IList<QueryBuilderArgumentInfo> args, GraphQlDirective[] directives)
        {
            _fieldCriteria[alias ?? fieldName] = new GraphQlScalarFieldCriteria(fieldName, alias, args, directives);
        }
    
        protected void IncludeObjectField(string fieldName, string alias, GraphQlQueryBuilder objectFieldQueryBuilder, IList<QueryBuilderArgumentInfo> args, GraphQlDirective[] directives)
        {
            _fieldCriteria[alias ?? fieldName] = new GraphQlObjectFieldCriteria(fieldName, alias, objectFieldQueryBuilder, args, directives);
        }
    
        protected void IncludeFragment(GraphQlQueryBuilder objectFieldQueryBuilder, GraphQlDirective[] directives)
        {
            _fragments = _fragments ?? new Dictionary<string, GraphQlFragmentCriteria>();
            _fragments[objectFieldQueryBuilder.TypeName] = new GraphQlFragmentCriteria(objectFieldQueryBuilder, directives);
        }
    
        protected void ExcludeField(string fieldName)
        {
            if (fieldName == null)
                throw new ArgumentNullException(nameof(fieldName));
    
            _fieldCriteria.Remove(fieldName);
        }
    
        protected void IncludeFields(IEnumerable<GraphQlFieldMetadata> fields)
        {
            IncludeFields(fields, 0, new Dictionary<global::System.Type, int>());
        }
    
        private void IncludeFields(IEnumerable<GraphQlFieldMetadata> fields, int level, Dictionary<global::System.Type, int> parentTypeLevel)
        {
            global::System.Type builderType = null;
    
            foreach (var field in fields)
            {
                if (field.QueryBuilderType == null)
                    IncludeScalarField(field.Name, field.DefaultAlias, null, null);
                else
                {
                    if (_operationType != null && GetType() == field.QueryBuilderType ||
                        parentTypeLevel.TryGetValue(field.QueryBuilderType, out var parentLevel) && parentLevel < level)
                        continue;
    
                    if (builderType is null)
                    {
                        builderType = GetType();
                        parentLevel = parentTypeLevel.TryGetValue(builderType, out parentLevel) ? parentLevel : level;
                        parentTypeLevel[builderType] = Math.Min(level, parentLevel);
                    }
    
                    var queryBuilder = InitializeChildQueryBuilder(builderType, field.QueryBuilderType, level, parentTypeLevel);
    
                    var includeFragmentMethods = field.QueryBuilderType.GetMethods().Where(IsIncludeFragmentMethod);
    
                    foreach (var includeFragmentMethod in includeFragmentMethods)
                        includeFragmentMethod.Invoke(
                            queryBuilder,
                            new object[] { InitializeChildQueryBuilder(builderType, includeFragmentMethod.GetParameters()[0].ParameterType, level, parentTypeLevel) });
    
                    if (queryBuilder._fieldCriteria.Count > 0 || queryBuilder._fragments != null)
                        IncludeObjectField(field.Name, field.DefaultAlias, queryBuilder, null, null);
                }
            }
        }
    
        private static GraphQlQueryBuilder InitializeChildQueryBuilder(global::System.Type parentQueryBuilderType, global::System.Type queryBuilderType, int level, Dictionary<global::System.Type, int> parentTypeLevel)
        {
            var queryBuilder = (GraphQlQueryBuilder)Activator.CreateInstance(queryBuilderType);
            queryBuilder.IncludeFields(
                queryBuilder.AllFields.Where(f => !f.RequiresParameters),
                level + 1,
                parentTypeLevel);
    
            return queryBuilder;
        }
    
        private static bool IsIncludeFragmentMethod(MethodInfo methodInfo)
        {
            if (!methodInfo.Name.StartsWith("With") || !methodInfo.Name.EndsWith("Fragment"))
                return false;
    
            var parameters = methodInfo.GetParameters();
            return parameters.Length == 1 && parameters[0].ParameterType.IsSubclassOf(typeof(GraphQlQueryBuilder));
        }
    
        protected void AddParameter<T>(GraphQlQueryParameter<T> parameter)
        {
            if (_queryParameters == null)
                _queryParameters = new List<QueryBuilderArgumentInfo>();
    
            _queryParameters.Add(new QueryBuilderArgumentInfo { ArgumentValue = parameter, FormatMask = parameter.FormatMask });
        }
    
        private abstract class GraphQlFieldCriteria
        {
            private readonly IList<QueryBuilderArgumentInfo> _args;
            private readonly GraphQlDirective[] _directives;
    
            protected readonly string FieldName;
            protected readonly string Alias;
    
            protected static string GetIndentation(Formatting formatting, int level, byte indentationSize) =>
                formatting == Formatting.Indented ? GraphQlQueryHelper.GetIndentation(level, indentationSize) : null;
    
            protected GraphQlFieldCriteria(string fieldName, string alias, IList<QueryBuilderArgumentInfo> args, GraphQlDirective[] directives)
            {
                GraphQlQueryHelper.ValidateGraphQlIdentifier(nameof(alias), alias);
                FieldName = fieldName;
                Alias = alias;
                _args = args;
                _directives = directives;
            }
    
            public abstract string Build(GraphQlBuilderOptions options, int level);
    
            protected string BuildArgumentClause(GraphQlBuilderOptions options, int level)
            {
                var separator = options.Formatting == Formatting.Indented ? " " : null;
                var argumentCount = _args?.Count ?? 0;
                if (argumentCount == 0)
                    return String.Empty;
    
                var arguments =
                    _args.Select(
                        a => $"{a.ArgumentName}:{separator}{(a.ArgumentValue.Name == null ? GraphQlQueryHelper.BuildArgumentValue(a.ArgumentValue.Value, a.FormatMask, options, level) : $"${a.ArgumentValue.Name}")}");
    
                return $"({String.Join($",{separator}", arguments)})";
            }
    
            protected string BuildDirectiveClause(GraphQlBuilderOptions options, int level) =>
                _directives == null ? null : String.Concat(_directives.Select(d => d == null ? null : GraphQlQueryHelper.BuildDirective(d, options, level)));
    
            protected static string BuildAliasPrefix(string alias, Formatting formatting)
            {
                var separator = formatting == Formatting.Indented ? " " : String.Empty;
                return String.IsNullOrWhiteSpace(alias) ? null : $"{alias}:{separator}";
            }
        }
    
        private class GraphQlScalarFieldCriteria : GraphQlFieldCriteria
        {
            public GraphQlScalarFieldCriteria(string fieldName, string alias, IList<QueryBuilderArgumentInfo> args, GraphQlDirective[] directives)
                : base(fieldName, alias, args, directives)
            {
            }
    
            public override string Build(GraphQlBuilderOptions options, int level) =>
                GetIndentation(options.Formatting, level, options.IndentationSize) +
                BuildAliasPrefix(Alias, options.Formatting) +
                FieldName +
                BuildArgumentClause(options, level) +
                BuildDirectiveClause(options, level);
        }
    
        private class GraphQlObjectFieldCriteria : GraphQlFieldCriteria
        {
            private readonly GraphQlQueryBuilder _objectQueryBuilder;
    
            public GraphQlObjectFieldCriteria(string fieldName, string alias, GraphQlQueryBuilder objectQueryBuilder, IList<QueryBuilderArgumentInfo> args, GraphQlDirective[] directives)
                : base(fieldName, alias, args, directives)
            {
                _objectQueryBuilder = objectQueryBuilder;
            }
    
            public override string Build(GraphQlBuilderOptions options, int level) =>
                _objectQueryBuilder._fieldCriteria.Count > 0 || _objectQueryBuilder._fragments?.Count > 0
                    ? GetIndentation(options.Formatting, level, options.IndentationSize) + BuildAliasPrefix(Alias, options.Formatting) + FieldName +
                      BuildArgumentClause(options, level) + BuildDirectiveClause(options, level) + _objectQueryBuilder.Build(options, level + 1)
                    : null;
        }
    
        private class GraphQlFragmentCriteria : GraphQlFieldCriteria
        {
            private readonly GraphQlQueryBuilder _objectQueryBuilder;
    
            public GraphQlFragmentCriteria(GraphQlQueryBuilder objectQueryBuilder, GraphQlDirective[] directives) : base(objectQueryBuilder.TypeName, null, null, directives)
            {
                _objectQueryBuilder = objectQueryBuilder;
            }
    
            public override string Build(GraphQlBuilderOptions options, int level) =>
                _objectQueryBuilder._fieldCriteria.Count == 0
                    ? null
                    : GetIndentation(options.Formatting, level, options.IndentationSize) + "..." + (options.Formatting == Formatting.Indented ? " " : null) + "on " +
                      FieldName + BuildArgumentClause(options, level) + BuildDirectiveClause(options, level) + _objectQueryBuilder.Build(options, level + 1);
        }
    }
    
    public abstract partial class GraphQlQueryBuilder<TQueryBuilder> : GraphQlQueryBuilder where TQueryBuilder : GraphQlQueryBuilder<TQueryBuilder>
    {
        protected GraphQlQueryBuilder(string operationType = null, string operationName = null) : base(operationType, operationName)
        {
        }
    
        /// <summary>
        /// Includes all fields that don't require parameters into the query.
        /// </summary>
        public TQueryBuilder WithAllFields()
        {
            IncludeAllFields();
            return (TQueryBuilder)this;
        }
    
        /// <summary>
        /// Includes all scalar fields that don't require parameters into the query.
        /// </summary>
        public TQueryBuilder WithAllScalarFields()
        {
            IncludeFields(AllFields.Where(f => !f.IsComplex && !f.RequiresParameters));
            return (TQueryBuilder)this;
        }
    
        public TQueryBuilder ExceptField(string fieldName)
        {
            ExcludeField(fieldName);
            return (TQueryBuilder)this;
        }
    
        /// <summary>
        /// Includes "__typename" field; included automatically for interface and union types.
        /// </summary>
        public TQueryBuilder WithTypeName(string alias = null, params GraphQlDirective[] directives)
        {
            IncludeScalarField("__typename", alias, null, directives);
            return (TQueryBuilder)this;
        }
    
        protected TQueryBuilder WithScalarField(string fieldName, string alias, GraphQlDirective[] directives, IList<QueryBuilderArgumentInfo> args = null)
        {
            IncludeScalarField(fieldName, alias, args, directives);
            return (TQueryBuilder)this;
        }
    
        protected TQueryBuilder WithObjectField(string fieldName, string alias, GraphQlQueryBuilder queryBuilder, GraphQlDirective[] directives, IList<QueryBuilderArgumentInfo> args = null)
        {
            IncludeObjectField(fieldName, alias, queryBuilder, args, directives);
            return (TQueryBuilder)this;
        }
    
        protected TQueryBuilder WithFragment(GraphQlQueryBuilder queryBuilder, GraphQlDirective[] directives)
        {
            IncludeFragment(queryBuilder, directives);
            return (TQueryBuilder)this;
        }
    
        protected TQueryBuilder WithParameterInternal<T>(GraphQlQueryParameter<T> parameter)
        {
            AddParameter(parameter);
            return (TQueryBuilder)this;
        }
    }
    
    public abstract class GraphQlResponse<TDataContract>
    {
        public TDataContract Data { get; set; }
        public ICollection<GraphQlQueryError> Errors { get; set; }
    }
    
    public class GraphQlQueryError
    {
        public string Message { get; set; }
        public ICollection<GraphQlErrorLocation> Locations { get; set; }
    }
    
    public class GraphQlErrorLocation
    {
        public int Line { get; set; }
        public int Column { get; set; }
    }
    #endregion

    #region GraphQL type helpers
    public static class GraphQlTypes
    {
        public const string Boolean = "Boolean";
        public const string DateTime = "DateTime";
        public const string EmailAddress = "EmailAddress";
        public const string Id = "ID";
        public const string Int = "Int";
        public const string String = "String";
        public const string Uuid = "UUID";

        public const string UserRole = "UserRole";

        public const string AuthenticationError = "AuthenticationError";
        public const string Checkpoint = "Checkpoint";
        public const string LiveDoc = "LiveDoc";
        public const string LiveDocPullBulk = "LiveDocPullBulk";
        public const string Mutation = "Mutation";
        public const string PushLiveDocPayload = "PushLiveDocPayload";
        public const string PushUserPayload = "PushUserPayload";
        public const string PushWorkspacePayload = "PushWorkspacePayload";
        public const string Query = "Query";
        public const string Subscription = "Subscription";
        public const string UnauthorizedAccessError = "UnauthorizedAccessError";
        public const string User = "User";
        public const string UserPullBulk = "UserPullBulk";
        public const string Workspace = "Workspace";
        public const string WorkspacePullBulk = "WorkspacePullBulk";

        public const string BooleanOperationFilterInput = "BooleanOperationFilterInput";
        public const string DateTimeOperationFilterInput = "DateTimeOperationFilterInput";
        public const string ListStringOperationFilterInput = "ListStringOperationFilterInput";
        public const string LiveDocFilterInput = "LiveDocFilterInput";
        public const string LiveDocInput = "LiveDocInput";
        public const string LiveDocInputCheckpoint = "LiveDocInputCheckpoint";
        public const string LiveDocInputHeaders = "LiveDocInputHeaders";
        public const string LiveDocInputPushRow = "LiveDocInputPushRow";
        public const string PushLiveDocInput = "PushLiveDocInput";
        public const string PushUserInput = "PushUserInput";
        public const string PushWorkspaceInput = "PushWorkspaceInput";
        public const string StringOperationFilterInput = "StringOperationFilterInput";
        public const string UserFilterInput = "UserFilterInput";
        public const string UserInput = "UserInput";
        public const string UserInputCheckpoint = "UserInputCheckpoint";
        public const string UserInputHeaders = "UserInputHeaders";
        public const string UserInputPushRow = "UserInputPushRow";
        public const string UserRoleOperationFilterInput = "UserRoleOperationFilterInput";
        public const string UuidOperationFilterInput = "UuidOperationFilterInput";
        public const string WorkspaceFilterInput = "WorkspaceFilterInput";
        public const string WorkspaceInput = "WorkspaceInput";
        public const string WorkspaceInputCheckpoint = "WorkspaceInputCheckpoint";
        public const string WorkspaceInputHeaders = "WorkspaceInputHeaders";
        public const string WorkspaceInputPushRow = "WorkspaceInputPushRow";

        public const string PushLiveDocError = "PushLiveDocError";
        public const string PushUserError = "PushUserError";
        public const string PushWorkspaceError = "PushWorkspaceError";

        public const string Error = "Error";

        public static readonly IReadOnlyDictionary<global::System.Type, string> ReverseMapping =
            new Dictionary<global::System.Type, string>
            {
                { typeof(string), "String" },
                { typeof(Guid), "UUID" },
                { typeof(DateTimeOffset), "DateTime" },
                { typeof(bool), "Boolean" },
                { typeof(BooleanOperationFilterInputGql), "BooleanOperationFilterInput" },
                { typeof(DateTimeOperationFilterInputGql), "DateTimeOperationFilterInput" },
                { typeof(ListStringOperationFilterInputGql), "ListStringOperationFilterInput" },
                { typeof(LiveDocFilterInputGql), "LiveDocFilterInput" },
                { typeof(LiveDocInputGql), "LiveDocInput" },
                { typeof(LiveDocInputCheckpointGql), "LiveDocInputCheckpoint" },
                { typeof(LiveDocInputHeadersGql), "LiveDocInputHeaders" },
                { typeof(LiveDocInputPushRowGql), "LiveDocInputPushRow" },
                { typeof(PushLiveDocInputGql), "PushLiveDocInput" },
                { typeof(PushUserInputGql), "PushUserInput" },
                { typeof(PushWorkspaceInputGql), "PushWorkspaceInput" },
                { typeof(StringOperationFilterInputGql), "StringOperationFilterInput" },
                { typeof(UserFilterInputGql), "UserFilterInput" },
                { typeof(UserInputGql), "UserInput" },
                { typeof(UserInputCheckpointGql), "UserInputCheckpoint" },
                { typeof(UserInputHeadersGql), "UserInputHeaders" },
                { typeof(UserInputPushRowGql), "UserInputPushRow" },
                { typeof(UserRoleOperationFilterInputGql), "UserRoleOperationFilterInput" },
                { typeof(UuidOperationFilterInputGql), "UuidOperationFilterInput" },
                { typeof(WorkspaceFilterInputGql), "WorkspaceFilterInput" },
                { typeof(WorkspaceInputGql), "WorkspaceInput" },
                { typeof(WorkspaceInputCheckpointGql), "WorkspaceInputCheckpoint" },
                { typeof(WorkspaceInputHeadersGql), "WorkspaceInputHeaders" },
                { typeof(WorkspaceInputPushRowGql), "WorkspaceInputPushRow" }
            };
}
    #endregion

    #region enums
    public enum UserRoleGql
    {
        StandardUser,
        WorkspaceAdmin,
        SystemAdmin
    }
    #endregion

    #nullable enable
    #region directives
    public class SkipDirective : GraphQlDirective
    {
        public SkipDirective(QueryBuilderParameter<bool> @if) : base("skip")
        {
            AddArgument("if", @if);
        }
    }

    public class IncludeDirective : GraphQlDirective
    {
        public IncludeDirective(QueryBuilderParameter<bool> @if) : base("include")
        {
            AddArgument("if", @if);
        }
    }
    #endregion

    #region builder classes
    public partial class UserPullBulkQueryBuilderGql : GraphQlQueryBuilder<UserPullBulkQueryBuilderGql>
    {
        private static readonly GraphQlFieldMetadata[] AllFieldMetadata =
        {
            new GraphQlFieldMetadata { Name = "documents", IsComplex = true, QueryBuilderType = typeof(UserQueryBuilderGql) },
            new GraphQlFieldMetadata { Name = "checkpoint", IsComplex = true, QueryBuilderType = typeof(CheckpointQueryBuilderGql) }
        };

        protected override string TypeName { get; } = "UserPullBulk";

        public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get; } = AllFieldMetadata;

        public UserPullBulkQueryBuilderGql WithDocuments(UserQueryBuilderGql userQueryBuilder, string? alias = null, SkipDirective? skip = null, IncludeDirective? include = null) => WithObjectField("documents", alias, userQueryBuilder, new GraphQlDirective?[] { skip, include });

        public UserPullBulkQueryBuilderGql ExceptDocuments() => ExceptField("documents");

        public UserPullBulkQueryBuilderGql WithCheckpoint(CheckpointQueryBuilderGql checkpointQueryBuilder, string? alias = null, SkipDirective? skip = null, IncludeDirective? include = null) => WithObjectField("checkpoint", alias, checkpointQueryBuilder, new GraphQlDirective?[] { skip, include });

        public UserPullBulkQueryBuilderGql ExceptCheckpoint() => ExceptField("checkpoint");
    }

    public partial class WorkspacePullBulkQueryBuilderGql : GraphQlQueryBuilder<WorkspacePullBulkQueryBuilderGql>
    {
        private static readonly GraphQlFieldMetadata[] AllFieldMetadata =
        {
            new GraphQlFieldMetadata { Name = "documents", IsComplex = true, QueryBuilderType = typeof(WorkspaceQueryBuilderGql) },
            new GraphQlFieldMetadata { Name = "checkpoint", IsComplex = true, QueryBuilderType = typeof(CheckpointQueryBuilderGql) }
        };

        protected override string TypeName { get; } = "WorkspacePullBulk";

        public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get; } = AllFieldMetadata;

        public WorkspacePullBulkQueryBuilderGql WithDocuments(WorkspaceQueryBuilderGql workspaceQueryBuilder, string? alias = null, SkipDirective? skip = null, IncludeDirective? include = null) => WithObjectField("documents", alias, workspaceQueryBuilder, new GraphQlDirective?[] { skip, include });

        public WorkspacePullBulkQueryBuilderGql ExceptDocuments() => ExceptField("documents");

        public WorkspacePullBulkQueryBuilderGql WithCheckpoint(CheckpointQueryBuilderGql checkpointQueryBuilder, string? alias = null, SkipDirective? skip = null, IncludeDirective? include = null) => WithObjectField("checkpoint", alias, checkpointQueryBuilder, new GraphQlDirective?[] { skip, include });

        public WorkspacePullBulkQueryBuilderGql ExceptCheckpoint() => ExceptField("checkpoint");
    }

    public partial class LiveDocPullBulkQueryBuilderGql : GraphQlQueryBuilder<LiveDocPullBulkQueryBuilderGql>
    {
        private static readonly GraphQlFieldMetadata[] AllFieldMetadata =
        {
            new GraphQlFieldMetadata { Name = "documents", IsComplex = true, QueryBuilderType = typeof(LiveDocQueryBuilderGql) },
            new GraphQlFieldMetadata { Name = "checkpoint", IsComplex = true, QueryBuilderType = typeof(CheckpointQueryBuilderGql) }
        };

        protected override string TypeName { get; } = "LiveDocPullBulk";

        public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get; } = AllFieldMetadata;

        public LiveDocPullBulkQueryBuilderGql WithDocuments(LiveDocQueryBuilderGql liveDocQueryBuilder, string? alias = null, SkipDirective? skip = null, IncludeDirective? include = null) => WithObjectField("documents", alias, liveDocQueryBuilder, new GraphQlDirective?[] { skip, include });

        public LiveDocPullBulkQueryBuilderGql ExceptDocuments() => ExceptField("documents");

        public LiveDocPullBulkQueryBuilderGql WithCheckpoint(CheckpointQueryBuilderGql checkpointQueryBuilder, string? alias = null, SkipDirective? skip = null, IncludeDirective? include = null) => WithObjectField("checkpoint", alias, checkpointQueryBuilder, new GraphQlDirective?[] { skip, include });

        public LiveDocPullBulkQueryBuilderGql ExceptCheckpoint() => ExceptField("checkpoint");
    }

    public partial class QueryQueryBuilderGql : GraphQlQueryBuilder<QueryQueryBuilderGql>
    {
        private static readonly GraphQlFieldMetadata[] AllFieldMetadata =
        {
            new GraphQlFieldMetadata { Name = "pullUser", RequiresParameters = true, IsComplex = true, QueryBuilderType = typeof(UserPullBulkQueryBuilderGql) },
            new GraphQlFieldMetadata { Name = "pullWorkspace", RequiresParameters = true, IsComplex = true, QueryBuilderType = typeof(WorkspacePullBulkQueryBuilderGql) },
            new GraphQlFieldMetadata { Name = "pullLiveDoc", RequiresParameters = true, IsComplex = true, QueryBuilderType = typeof(LiveDocPullBulkQueryBuilderGql) }
        };

        protected override string TypeName { get; } = "Query";

        public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get; } = AllFieldMetadata;

        public QueryQueryBuilderGql(string? operationName = null) : base("query", operationName)
        {
        }

        public QueryQueryBuilderGql WithParameter<T>(GraphQlQueryParameter<T> parameter) => WithParameterInternal(parameter);

        public QueryQueryBuilderGql WithPullUser(UserPullBulkQueryBuilderGql userPullBulkQueryBuilder, QueryBuilderParameter<int> limit, QueryBuilderParameter<UserInputCheckpointGql?>? checkpoint = null, QueryBuilderParameter<UserFilterInputGql?>? where = null, string? alias = null, SkipDirective? skip = null, IncludeDirective? include = null)
        {
            var args = new List<QueryBuilderArgumentInfo>();
            if (checkpoint != null)
                args.Add(new QueryBuilderArgumentInfo { ArgumentName = "checkpoint", ArgumentValue = checkpoint} );

            args.Add(new QueryBuilderArgumentInfo { ArgumentName = "limit", ArgumentValue = limit} );
            if (where != null)
                args.Add(new QueryBuilderArgumentInfo { ArgumentName = "where", ArgumentValue = where} );

            return WithObjectField("pullUser", alias, userPullBulkQueryBuilder, new GraphQlDirective?[] { skip, include }, args);
        }

        public QueryQueryBuilderGql ExceptPullUser() => ExceptField("pullUser");

        public QueryQueryBuilderGql WithPullWorkspace(WorkspacePullBulkQueryBuilderGql workspacePullBulkQueryBuilder, QueryBuilderParameter<int> limit, QueryBuilderParameter<WorkspaceInputCheckpointGql?>? checkpoint = null, QueryBuilderParameter<WorkspaceFilterInputGql?>? where = null, string? alias = null, SkipDirective? skip = null, IncludeDirective? include = null)
        {
            var args = new List<QueryBuilderArgumentInfo>();
            if (checkpoint != null)
                args.Add(new QueryBuilderArgumentInfo { ArgumentName = "checkpoint", ArgumentValue = checkpoint} );

            args.Add(new QueryBuilderArgumentInfo { ArgumentName = "limit", ArgumentValue = limit} );
            if (where != null)
                args.Add(new QueryBuilderArgumentInfo { ArgumentName = "where", ArgumentValue = where} );

            return WithObjectField("pullWorkspace", alias, workspacePullBulkQueryBuilder, new GraphQlDirective?[] { skip, include }, args);
        }

        public QueryQueryBuilderGql ExceptPullWorkspace() => ExceptField("pullWorkspace");

        public QueryQueryBuilderGql WithPullLiveDoc(LiveDocPullBulkQueryBuilderGql liveDocPullBulkQueryBuilder, QueryBuilderParameter<int> limit, QueryBuilderParameter<LiveDocInputCheckpointGql?>? checkpoint = null, QueryBuilderParameter<LiveDocFilterInputGql?>? where = null, string? alias = null, SkipDirective? skip = null, IncludeDirective? include = null)
        {
            var args = new List<QueryBuilderArgumentInfo>();
            if (checkpoint != null)
                args.Add(new QueryBuilderArgumentInfo { ArgumentName = "checkpoint", ArgumentValue = checkpoint} );

            args.Add(new QueryBuilderArgumentInfo { ArgumentName = "limit", ArgumentValue = limit} );
            if (where != null)
                args.Add(new QueryBuilderArgumentInfo { ArgumentName = "where", ArgumentValue = where} );

            return WithObjectField("pullLiveDoc", alias, liveDocPullBulkQueryBuilder, new GraphQlDirective?[] { skip, include }, args);
        }

        public QueryQueryBuilderGql ExceptPullLiveDoc() => ExceptField("pullLiveDoc");
    }

    public partial class MutationQueryBuilderGql : GraphQlQueryBuilder<MutationQueryBuilderGql>
    {
        private static readonly GraphQlFieldMetadata[] AllFieldMetadata =
        {
            new GraphQlFieldMetadata { Name = "pushUser", RequiresParameters = true, IsComplex = true, QueryBuilderType = typeof(PushUserPayloadQueryBuilderGql) },
            new GraphQlFieldMetadata { Name = "pushWorkspace", RequiresParameters = true, IsComplex = true, QueryBuilderType = typeof(PushWorkspacePayloadQueryBuilderGql) },
            new GraphQlFieldMetadata { Name = "pushLiveDoc", RequiresParameters = true, IsComplex = true, QueryBuilderType = typeof(PushLiveDocPayloadQueryBuilderGql) }
        };

        protected override string TypeName { get; } = "Mutation";

        public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get; } = AllFieldMetadata;

        public MutationQueryBuilderGql(string? operationName = null) : base("mutation", operationName)
        {
        }

        public MutationQueryBuilderGql WithParameter<T>(GraphQlQueryParameter<T> parameter) => WithParameterInternal(parameter);

        public MutationQueryBuilderGql WithPushUser(PushUserPayloadQueryBuilderGql pushUserPayloadQueryBuilder, QueryBuilderParameter<PushUserInputGql> input, string? alias = null, SkipDirective? skip = null, IncludeDirective? include = null)
        {
            var args = new List<QueryBuilderArgumentInfo>();
            args.Add(new QueryBuilderArgumentInfo { ArgumentName = "input", ArgumentValue = input} );
            return WithObjectField("pushUser", alias, pushUserPayloadQueryBuilder, new GraphQlDirective?[] { skip, include }, args);
        }

        public MutationQueryBuilderGql ExceptPushUser() => ExceptField("pushUser");

        public MutationQueryBuilderGql WithPushWorkspace(PushWorkspacePayloadQueryBuilderGql pushWorkspacePayloadQueryBuilder, QueryBuilderParameter<PushWorkspaceInputGql> input, string? alias = null, SkipDirective? skip = null, IncludeDirective? include = null)
        {
            var args = new List<QueryBuilderArgumentInfo>();
            args.Add(new QueryBuilderArgumentInfo { ArgumentName = "input", ArgumentValue = input} );
            return WithObjectField("pushWorkspace", alias, pushWorkspacePayloadQueryBuilder, new GraphQlDirective?[] { skip, include }, args);
        }

        public MutationQueryBuilderGql ExceptPushWorkspace() => ExceptField("pushWorkspace");

        public MutationQueryBuilderGql WithPushLiveDoc(PushLiveDocPayloadQueryBuilderGql pushLiveDocPayloadQueryBuilder, QueryBuilderParameter<PushLiveDocInputGql> input, string? alias = null, SkipDirective? skip = null, IncludeDirective? include = null)
        {
            var args = new List<QueryBuilderArgumentInfo>();
            args.Add(new QueryBuilderArgumentInfo { ArgumentName = "input", ArgumentValue = input} );
            return WithObjectField("pushLiveDoc", alias, pushLiveDocPayloadQueryBuilder, new GraphQlDirective?[] { skip, include }, args);
        }

        public MutationQueryBuilderGql ExceptPushLiveDoc() => ExceptField("pushLiveDoc");
    }

    public partial class SubscriptionQueryBuilderGql : GraphQlQueryBuilder<SubscriptionQueryBuilderGql>
    {
        private static readonly GraphQlFieldMetadata[] AllFieldMetadata =
        {
            new GraphQlFieldMetadata { Name = "streamUser", IsComplex = true, QueryBuilderType = typeof(UserPullBulkQueryBuilderGql) },
            new GraphQlFieldMetadata { Name = "streamWorkspace", IsComplex = true, QueryBuilderType = typeof(WorkspacePullBulkQueryBuilderGql) },
            new GraphQlFieldMetadata { Name = "streamLiveDoc", IsComplex = true, QueryBuilderType = typeof(LiveDocPullBulkQueryBuilderGql) }
        };

        protected override string TypeName { get; } = "Subscription";

        public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get; } = AllFieldMetadata;

        public SubscriptionQueryBuilderGql(string? operationName = null) : base("subscription", operationName)
        {
        }

        public SubscriptionQueryBuilderGql WithParameter<T>(GraphQlQueryParameter<T> parameter) => WithParameterInternal(parameter);

        public SubscriptionQueryBuilderGql WithStreamUser(UserPullBulkQueryBuilderGql userPullBulkQueryBuilder, QueryBuilderParameter<UserInputHeadersGql?>? headers = null, QueryBuilderParameter<IEnumerable<string>>? topics = null, string? alias = null, SkipDirective? skip = null, IncludeDirective? include = null)
        {
            var args = new List<QueryBuilderArgumentInfo>();
            if (headers != null)
                args.Add(new QueryBuilderArgumentInfo { ArgumentName = "headers", ArgumentValue = headers} );

            if (topics != null)
                args.Add(new QueryBuilderArgumentInfo { ArgumentName = "topics", ArgumentValue = topics} );

            return WithObjectField("streamUser", alias, userPullBulkQueryBuilder, new GraphQlDirective?[] { skip, include }, args);
        }

        public SubscriptionQueryBuilderGql ExceptStreamUser() => ExceptField("streamUser");

        public SubscriptionQueryBuilderGql WithStreamWorkspace(WorkspacePullBulkQueryBuilderGql workspacePullBulkQueryBuilder, QueryBuilderParameter<WorkspaceInputHeadersGql?>? headers = null, QueryBuilderParameter<IEnumerable<string>>? topics = null, string? alias = null, SkipDirective? skip = null, IncludeDirective? include = null)
        {
            var args = new List<QueryBuilderArgumentInfo>();
            if (headers != null)
                args.Add(new QueryBuilderArgumentInfo { ArgumentName = "headers", ArgumentValue = headers} );

            if (topics != null)
                args.Add(new QueryBuilderArgumentInfo { ArgumentName = "topics", ArgumentValue = topics} );

            return WithObjectField("streamWorkspace", alias, workspacePullBulkQueryBuilder, new GraphQlDirective?[] { skip, include }, args);
        }

        public SubscriptionQueryBuilderGql ExceptStreamWorkspace() => ExceptField("streamWorkspace");

        public SubscriptionQueryBuilderGql WithStreamLiveDoc(LiveDocPullBulkQueryBuilderGql liveDocPullBulkQueryBuilder, QueryBuilderParameter<LiveDocInputHeadersGql?>? headers = null, QueryBuilderParameter<IEnumerable<string>>? topics = null, string? alias = null, SkipDirective? skip = null, IncludeDirective? include = null)
        {
            var args = new List<QueryBuilderArgumentInfo>();
            if (headers != null)
                args.Add(new QueryBuilderArgumentInfo { ArgumentName = "headers", ArgumentValue = headers} );

            if (topics != null)
                args.Add(new QueryBuilderArgumentInfo { ArgumentName = "topics", ArgumentValue = topics} );

            return WithObjectField("streamLiveDoc", alias, liveDocPullBulkQueryBuilder, new GraphQlDirective?[] { skip, include }, args);
        }

        public SubscriptionQueryBuilderGql ExceptStreamLiveDoc() => ExceptField("streamLiveDoc");
    }

    public partial class UserQueryBuilderGql : GraphQlQueryBuilder<UserQueryBuilderGql>
    {
        private static readonly GraphQlFieldMetadata[] AllFieldMetadata =
        {
            new GraphQlFieldMetadata { Name = "firstName" },
            new GraphQlFieldMetadata { Name = "lastName" },
            new GraphQlFieldMetadata { Name = "fullName" },
            new GraphQlFieldMetadata { Name = "email" },
            new GraphQlFieldMetadata { Name = "role" },
            new GraphQlFieldMetadata { Name = "jwtAccessToken" },
            new GraphQlFieldMetadata { Name = "workspaceId" },
            new GraphQlFieldMetadata { Name = "id" },
            new GraphQlFieldMetadata { Name = "isDeleted" },
            new GraphQlFieldMetadata { Name = "updatedAt" },
            new GraphQlFieldMetadata { Name = "topics", IsComplex = true }
        };

        protected override string TypeName { get; } = "User";

        public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get; } = AllFieldMetadata;

        public UserQueryBuilderGql WithFirstName(string? alias = null, SkipDirective? skip = null, IncludeDirective? include = null) => WithScalarField("firstName", alias, new GraphQlDirective?[] { skip, include });

        public UserQueryBuilderGql ExceptFirstName() => ExceptField("firstName");

        public UserQueryBuilderGql WithLastName(string? alias = null, SkipDirective? skip = null, IncludeDirective? include = null) => WithScalarField("lastName", alias, new GraphQlDirective?[] { skip, include });

        public UserQueryBuilderGql ExceptLastName() => ExceptField("lastName");

        public UserQueryBuilderGql WithFullName(string? alias = null, SkipDirective? skip = null, IncludeDirective? include = null) => WithScalarField("fullName", alias, new GraphQlDirective?[] { skip, include });

        public UserQueryBuilderGql ExceptFullName() => ExceptField("fullName");

        public UserQueryBuilderGql WithEmail(string? alias = null, SkipDirective? skip = null, IncludeDirective? include = null) => WithScalarField("email", alias, new GraphQlDirective?[] { skip, include });

        public UserQueryBuilderGql ExceptEmail() => ExceptField("email");

        public UserQueryBuilderGql WithRole(string? alias = null, SkipDirective? skip = null, IncludeDirective? include = null) => WithScalarField("role", alias, new GraphQlDirective?[] { skip, include });

        public UserQueryBuilderGql ExceptRole() => ExceptField("role");

        public UserQueryBuilderGql WithJwtAccessToken(string? alias = null, SkipDirective? skip = null, IncludeDirective? include = null) => WithScalarField("jwtAccessToken", alias, new GraphQlDirective?[] { skip, include });

        public UserQueryBuilderGql ExceptJwtAccessToken() => ExceptField("jwtAccessToken");

        public UserQueryBuilderGql WithWorkspaceId(string? alias = null, SkipDirective? skip = null, IncludeDirective? include = null) => WithScalarField("workspaceId", alias, new GraphQlDirective?[] { skip, include });

        public UserQueryBuilderGql ExceptWorkspaceId() => ExceptField("workspaceId");

        public UserQueryBuilderGql WithId(string? alias = null, SkipDirective? skip = null, IncludeDirective? include = null) => WithScalarField("id", alias, new GraphQlDirective?[] { skip, include });

        public UserQueryBuilderGql ExceptId() => ExceptField("id");

        public UserQueryBuilderGql WithIsDeleted(string? alias = null, SkipDirective? skip = null, IncludeDirective? include = null) => WithScalarField("isDeleted", alias, new GraphQlDirective?[] { skip, include });

        public UserQueryBuilderGql ExceptIsDeleted() => ExceptField("isDeleted");

        public UserQueryBuilderGql WithUpdatedAt(string? alias = null, SkipDirective? skip = null, IncludeDirective? include = null) => WithScalarField("updatedAt", alias, new GraphQlDirective?[] { skip, include });

        public UserQueryBuilderGql ExceptUpdatedAt() => ExceptField("updatedAt");

        public UserQueryBuilderGql WithTopics(string? alias = null, SkipDirective? skip = null, IncludeDirective? include = null) => WithScalarField("topics", alias, new GraphQlDirective?[] { skip, include });

        public UserQueryBuilderGql ExceptTopics() => ExceptField("topics");
    }

    public partial class CheckpointQueryBuilderGql : GraphQlQueryBuilder<CheckpointQueryBuilderGql>
    {
        private static readonly GraphQlFieldMetadata[] AllFieldMetadata =
        {
            new GraphQlFieldMetadata { Name = "lastDocumentId" },
            new GraphQlFieldMetadata { Name = "updatedAt" }
        };

        protected override string TypeName { get; } = "Checkpoint";

        public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get; } = AllFieldMetadata;

        public CheckpointQueryBuilderGql WithLastDocumentId(string? alias = null, SkipDirective? skip = null, IncludeDirective? include = null) => WithScalarField("lastDocumentId", alias, new GraphQlDirective?[] { skip, include });

        public CheckpointQueryBuilderGql ExceptLastDocumentId() => ExceptField("lastDocumentId");

        public CheckpointQueryBuilderGql WithUpdatedAt(string? alias = null, SkipDirective? skip = null, IncludeDirective? include = null) => WithScalarField("updatedAt", alias, new GraphQlDirective?[] { skip, include });

        public CheckpointQueryBuilderGql ExceptUpdatedAt() => ExceptField("updatedAt");
    }

    public partial class AuthenticationErrorQueryBuilderGql : GraphQlQueryBuilder<AuthenticationErrorQueryBuilderGql>
    {
        private static readonly GraphQlFieldMetadata[] AllFieldMetadata =
        {
            new GraphQlFieldMetadata { Name = "message" }
        };

        protected override string TypeName { get; } = "AuthenticationError";

        public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get; } = AllFieldMetadata;

        public AuthenticationErrorQueryBuilderGql WithMessage(string? alias = null, SkipDirective? skip = null, IncludeDirective? include = null) => WithScalarField("message", alias, new GraphQlDirective?[] { skip, include });

        public AuthenticationErrorQueryBuilderGql ExceptMessage() => ExceptField("message");
    }

    public partial class UnauthorizedAccessErrorQueryBuilderGql : GraphQlQueryBuilder<UnauthorizedAccessErrorQueryBuilderGql>
    {
        private static readonly GraphQlFieldMetadata[] AllFieldMetadata =
        {
            new GraphQlFieldMetadata { Name = "message" }
        };

        protected override string TypeName { get; } = "UnauthorizedAccessError";

        public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get; } = AllFieldMetadata;

        public UnauthorizedAccessErrorQueryBuilderGql WithMessage(string? alias = null, SkipDirective? skip = null, IncludeDirective? include = null) => WithScalarField("message", alias, new GraphQlDirective?[] { skip, include });

        public UnauthorizedAccessErrorQueryBuilderGql ExceptMessage() => ExceptField("message");
    }

    public partial class WorkspaceQueryBuilderGql : GraphQlQueryBuilder<WorkspaceQueryBuilderGql>
    {
        private static readonly GraphQlFieldMetadata[] AllFieldMetadata =
        {
            new GraphQlFieldMetadata { Name = "name" },
            new GraphQlFieldMetadata { Name = "id" },
            new GraphQlFieldMetadata { Name = "isDeleted" },
            new GraphQlFieldMetadata { Name = "updatedAt" },
            new GraphQlFieldMetadata { Name = "topics", IsComplex = true }
        };

        protected override string TypeName { get; } = "Workspace";

        public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get; } = AllFieldMetadata;

        public WorkspaceQueryBuilderGql WithName(string? alias = null, SkipDirective? skip = null, IncludeDirective? include = null) => WithScalarField("name", alias, new GraphQlDirective?[] { skip, include });

        public WorkspaceQueryBuilderGql ExceptName() => ExceptField("name");

        public WorkspaceQueryBuilderGql WithId(string? alias = null, SkipDirective? skip = null, IncludeDirective? include = null) => WithScalarField("id", alias, new GraphQlDirective?[] { skip, include });

        public WorkspaceQueryBuilderGql ExceptId() => ExceptField("id");

        public WorkspaceQueryBuilderGql WithIsDeleted(string? alias = null, SkipDirective? skip = null, IncludeDirective? include = null) => WithScalarField("isDeleted", alias, new GraphQlDirective?[] { skip, include });

        public WorkspaceQueryBuilderGql ExceptIsDeleted() => ExceptField("isDeleted");

        public WorkspaceQueryBuilderGql WithUpdatedAt(string? alias = null, SkipDirective? skip = null, IncludeDirective? include = null) => WithScalarField("updatedAt", alias, new GraphQlDirective?[] { skip, include });

        public WorkspaceQueryBuilderGql ExceptUpdatedAt() => ExceptField("updatedAt");

        public WorkspaceQueryBuilderGql WithTopics(string? alias = null, SkipDirective? skip = null, IncludeDirective? include = null) => WithScalarField("topics", alias, new GraphQlDirective?[] { skip, include });

        public WorkspaceQueryBuilderGql ExceptTopics() => ExceptField("topics");
    }

    public partial class LiveDocQueryBuilderGql : GraphQlQueryBuilder<LiveDocQueryBuilderGql>
    {
        private static readonly GraphQlFieldMetadata[] AllFieldMetadata =
        {
            new GraphQlFieldMetadata { Name = "content" },
            new GraphQlFieldMetadata { Name = "ownerId" },
            new GraphQlFieldMetadata { Name = "workspaceId" },
            new GraphQlFieldMetadata { Name = "id" },
            new GraphQlFieldMetadata { Name = "isDeleted" },
            new GraphQlFieldMetadata { Name = "updatedAt" },
            new GraphQlFieldMetadata { Name = "topics", IsComplex = true }
        };

        protected override string TypeName { get; } = "LiveDoc";

        public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get; } = AllFieldMetadata;

        public LiveDocQueryBuilderGql WithContent(string? alias = null, SkipDirective? skip = null, IncludeDirective? include = null) => WithScalarField("content", alias, new GraphQlDirective?[] { skip, include });

        public LiveDocQueryBuilderGql ExceptContent() => ExceptField("content");

        public LiveDocQueryBuilderGql WithOwnerId(string? alias = null, SkipDirective? skip = null, IncludeDirective? include = null) => WithScalarField("ownerId", alias, new GraphQlDirective?[] { skip, include });

        public LiveDocQueryBuilderGql ExceptOwnerId() => ExceptField("ownerId");

        public LiveDocQueryBuilderGql WithWorkspaceId(string? alias = null, SkipDirective? skip = null, IncludeDirective? include = null) => WithScalarField("workspaceId", alias, new GraphQlDirective?[] { skip, include });

        public LiveDocQueryBuilderGql ExceptWorkspaceId() => ExceptField("workspaceId");

        public LiveDocQueryBuilderGql WithId(string? alias = null, SkipDirective? skip = null, IncludeDirective? include = null) => WithScalarField("id", alias, new GraphQlDirective?[] { skip, include });

        public LiveDocQueryBuilderGql ExceptId() => ExceptField("id");

        public LiveDocQueryBuilderGql WithIsDeleted(string? alias = null, SkipDirective? skip = null, IncludeDirective? include = null) => WithScalarField("isDeleted", alias, new GraphQlDirective?[] { skip, include });

        public LiveDocQueryBuilderGql ExceptIsDeleted() => ExceptField("isDeleted");

        public LiveDocQueryBuilderGql WithUpdatedAt(string? alias = null, SkipDirective? skip = null, IncludeDirective? include = null) => WithScalarField("updatedAt", alias, new GraphQlDirective?[] { skip, include });

        public LiveDocQueryBuilderGql ExceptUpdatedAt() => ExceptField("updatedAt");

        public LiveDocQueryBuilderGql WithTopics(string? alias = null, SkipDirective? skip = null, IncludeDirective? include = null) => WithScalarField("topics", alias, new GraphQlDirective?[] { skip, include });

        public LiveDocQueryBuilderGql ExceptTopics() => ExceptField("topics");
    }

    public partial class ErrorQueryBuilderGql : GraphQlQueryBuilder<ErrorQueryBuilderGql>
    {
        private static readonly GraphQlFieldMetadata[] AllFieldMetadata =
        {
            new GraphQlFieldMetadata { Name = "message" }
        };

        public ErrorQueryBuilderGql() => WithTypeName();

        protected override string TypeName { get; } = "Error";

        public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get; } = AllFieldMetadata;

        public ErrorQueryBuilderGql WithMessage(string? alias = null, SkipDirective? skip = null, IncludeDirective? include = null) => WithScalarField("message", alias, new GraphQlDirective?[] { skip, include });

        public ErrorQueryBuilderGql ExceptMessage() => ExceptField("message");

        public ErrorQueryBuilderGql WithAuthenticationErrorFragment(AuthenticationErrorQueryBuilderGql authenticationErrorQueryBuilder, SkipDirective? skip = null, IncludeDirective? include = null) => WithFragment(authenticationErrorQueryBuilder, new GraphQlDirective?[] { skip, include });

        public ErrorQueryBuilderGql WithUnauthorizedAccessErrorFragment(UnauthorizedAccessErrorQueryBuilderGql unauthorizedAccessErrorQueryBuilder, SkipDirective? skip = null, IncludeDirective? include = null) => WithFragment(unauthorizedAccessErrorQueryBuilder, new GraphQlDirective?[] { skip, include });
    }

    public partial class PushUserErrorQueryBuilderGql : GraphQlQueryBuilder<PushUserErrorQueryBuilderGql>
    {
        private static readonly GraphQlFieldMetadata[] AllFieldMetadata = Array.Empty<GraphQlFieldMetadata>();

        public PushUserErrorQueryBuilderGql() => WithTypeName();

        protected override string TypeName { get; } = "PushUserError";

        public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get; } = AllFieldMetadata;

        public PushUserErrorQueryBuilderGql WithAuthenticationErrorFragment(AuthenticationErrorQueryBuilderGql authenticationErrorQueryBuilder, SkipDirective? skip = null, IncludeDirective? include = null) => WithFragment(authenticationErrorQueryBuilder, new GraphQlDirective?[] { skip, include });

        public PushUserErrorQueryBuilderGql WithUnauthorizedAccessErrorFragment(UnauthorizedAccessErrorQueryBuilderGql unauthorizedAccessErrorQueryBuilder, SkipDirective? skip = null, IncludeDirective? include = null) => WithFragment(unauthorizedAccessErrorQueryBuilder, new GraphQlDirective?[] { skip, include });
    }

    public partial class PushUserPayloadQueryBuilderGql : GraphQlQueryBuilder<PushUserPayloadQueryBuilderGql>
    {
        private static readonly GraphQlFieldMetadata[] AllFieldMetadata =
        {
            new GraphQlFieldMetadata { Name = "user", IsComplex = true, QueryBuilderType = typeof(UserQueryBuilderGql) },
            new GraphQlFieldMetadata { Name = "errors", IsComplex = true, QueryBuilderType = typeof(PushUserErrorQueryBuilderGql) }
        };

        protected override string TypeName { get; } = "PushUserPayload";

        public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get; } = AllFieldMetadata;

        public PushUserPayloadQueryBuilderGql WithUser(UserQueryBuilderGql userQueryBuilder, string? alias = null, SkipDirective? skip = null, IncludeDirective? include = null) => WithObjectField("user", alias, userQueryBuilder, new GraphQlDirective?[] { skip, include });

        public PushUserPayloadQueryBuilderGql ExceptUser() => ExceptField("user");

        public PushUserPayloadQueryBuilderGql WithErrors(PushUserErrorQueryBuilderGql pushUserErrorQueryBuilder, string? alias = null, SkipDirective? skip = null, IncludeDirective? include = null) => WithObjectField("errors", alias, pushUserErrorQueryBuilder, new GraphQlDirective?[] { skip, include });

        public PushUserPayloadQueryBuilderGql ExceptErrors() => ExceptField("errors");
    }

    public partial class PushWorkspaceErrorQueryBuilderGql : GraphQlQueryBuilder<PushWorkspaceErrorQueryBuilderGql>
    {
        private static readonly GraphQlFieldMetadata[] AllFieldMetadata = Array.Empty<GraphQlFieldMetadata>();

        public PushWorkspaceErrorQueryBuilderGql() => WithTypeName();

        protected override string TypeName { get; } = "PushWorkspaceError";

        public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get; } = AllFieldMetadata;

        public PushWorkspaceErrorQueryBuilderGql WithAuthenticationErrorFragment(AuthenticationErrorQueryBuilderGql authenticationErrorQueryBuilder, SkipDirective? skip = null, IncludeDirective? include = null) => WithFragment(authenticationErrorQueryBuilder, new GraphQlDirective?[] { skip, include });

        public PushWorkspaceErrorQueryBuilderGql WithUnauthorizedAccessErrorFragment(UnauthorizedAccessErrorQueryBuilderGql unauthorizedAccessErrorQueryBuilder, SkipDirective? skip = null, IncludeDirective? include = null) => WithFragment(unauthorizedAccessErrorQueryBuilder, new GraphQlDirective?[] { skip, include });
    }

    public partial class PushWorkspacePayloadQueryBuilderGql : GraphQlQueryBuilder<PushWorkspacePayloadQueryBuilderGql>
    {
        private static readonly GraphQlFieldMetadata[] AllFieldMetadata =
        {
            new GraphQlFieldMetadata { Name = "workspace", IsComplex = true, QueryBuilderType = typeof(WorkspaceQueryBuilderGql) },
            new GraphQlFieldMetadata { Name = "errors", IsComplex = true, QueryBuilderType = typeof(PushWorkspaceErrorQueryBuilderGql) }
        };

        protected override string TypeName { get; } = "PushWorkspacePayload";

        public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get; } = AllFieldMetadata;

        public PushWorkspacePayloadQueryBuilderGql WithWorkspace(WorkspaceQueryBuilderGql workspaceQueryBuilder, string? alias = null, SkipDirective? skip = null, IncludeDirective? include = null) => WithObjectField("workspace", alias, workspaceQueryBuilder, new GraphQlDirective?[] { skip, include });

        public PushWorkspacePayloadQueryBuilderGql ExceptWorkspace() => ExceptField("workspace");

        public PushWorkspacePayloadQueryBuilderGql WithErrors(PushWorkspaceErrorQueryBuilderGql pushWorkspaceErrorQueryBuilder, string? alias = null, SkipDirective? skip = null, IncludeDirective? include = null) => WithObjectField("errors", alias, pushWorkspaceErrorQueryBuilder, new GraphQlDirective?[] { skip, include });

        public PushWorkspacePayloadQueryBuilderGql ExceptErrors() => ExceptField("errors");
    }

    public partial class PushLiveDocErrorQueryBuilderGql : GraphQlQueryBuilder<PushLiveDocErrorQueryBuilderGql>
    {
        private static readonly GraphQlFieldMetadata[] AllFieldMetadata = Array.Empty<GraphQlFieldMetadata>();

        public PushLiveDocErrorQueryBuilderGql() => WithTypeName();

        protected override string TypeName { get; } = "PushLiveDocError";

        public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get; } = AllFieldMetadata;

        public PushLiveDocErrorQueryBuilderGql WithAuthenticationErrorFragment(AuthenticationErrorQueryBuilderGql authenticationErrorQueryBuilder, SkipDirective? skip = null, IncludeDirective? include = null) => WithFragment(authenticationErrorQueryBuilder, new GraphQlDirective?[] { skip, include });

        public PushLiveDocErrorQueryBuilderGql WithUnauthorizedAccessErrorFragment(UnauthorizedAccessErrorQueryBuilderGql unauthorizedAccessErrorQueryBuilder, SkipDirective? skip = null, IncludeDirective? include = null) => WithFragment(unauthorizedAccessErrorQueryBuilder, new GraphQlDirective?[] { skip, include });
    }

    public partial class PushLiveDocPayloadQueryBuilderGql : GraphQlQueryBuilder<PushLiveDocPayloadQueryBuilderGql>
    {
        private static readonly GraphQlFieldMetadata[] AllFieldMetadata =
        {
            new GraphQlFieldMetadata { Name = "liveDoc", IsComplex = true, QueryBuilderType = typeof(LiveDocQueryBuilderGql) },
            new GraphQlFieldMetadata { Name = "errors", IsComplex = true, QueryBuilderType = typeof(PushLiveDocErrorQueryBuilderGql) }
        };

        protected override string TypeName { get; } = "PushLiveDocPayload";

        public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get; } = AllFieldMetadata;

        public PushLiveDocPayloadQueryBuilderGql WithLiveDoc(LiveDocQueryBuilderGql liveDocQueryBuilder, string? alias = null, SkipDirective? skip = null, IncludeDirective? include = null) => WithObjectField("liveDoc", alias, liveDocQueryBuilder, new GraphQlDirective?[] { skip, include });

        public PushLiveDocPayloadQueryBuilderGql ExceptLiveDoc() => ExceptField("liveDoc");

        public PushLiveDocPayloadQueryBuilderGql WithErrors(PushLiveDocErrorQueryBuilderGql pushLiveDocErrorQueryBuilder, string? alias = null, SkipDirective? skip = null, IncludeDirective? include = null) => WithObjectField("errors", alias, pushLiveDocErrorQueryBuilder, new GraphQlDirective?[] { skip, include });

        public PushLiveDocPayloadQueryBuilderGql ExceptErrors() => ExceptField("errors");
    }
    #endregion

    #region input classes
    public partial class UserInputCheckpointGql : IGraphQlInputObject
    {
        private InputPropertyInfo _updatedAt;
        private InputPropertyInfo _lastDocumentId;

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<DateTimeOffset?>))]
        #endif
        public QueryBuilderParameter<DateTimeOffset?>? UpdatedAt
        {
            get => (QueryBuilderParameter<DateTimeOffset?>?)_updatedAt.Value;
            set => _updatedAt = new InputPropertyInfo { Name = "updatedAt", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<Guid?>))]
        #endif
        public QueryBuilderParameter<Guid?>? LastDocumentId
        {
            get => (QueryBuilderParameter<Guid?>?)_lastDocumentId.Value;
            set => _lastDocumentId = new InputPropertyInfo { Name = "lastDocumentId", Value = value };
        }

        IEnumerable<InputPropertyInfo> IGraphQlInputObject.GetPropertyValues()
        {
            if (_updatedAt.Name != null) yield return _updatedAt;
            if (_lastDocumentId.Name != null) yield return _lastDocumentId;
        }
    }

    public partial class UserInputPushRowGql : IGraphQlInputObject
    {
        private InputPropertyInfo _assumedMasterState;
        private InputPropertyInfo _newDocumentState;

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<UserInputGql?>))]
        #endif
        public QueryBuilderParameter<UserInputGql?>? AssumedMasterState
        {
            get => (QueryBuilderParameter<UserInputGql?>?)_assumedMasterState.Value;
            set => _assumedMasterState = new InputPropertyInfo { Name = "assumedMasterState", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<UserInputGql?>))]
        #endif
        public QueryBuilderParameter<UserInputGql?>? NewDocumentState
        {
            get => (QueryBuilderParameter<UserInputGql?>?)_newDocumentState.Value;
            set => _newDocumentState = new InputPropertyInfo { Name = "newDocumentState", Value = value };
        }

        IEnumerable<InputPropertyInfo> IGraphQlInputObject.GetPropertyValues()
        {
            if (_assumedMasterState.Name != null) yield return _assumedMasterState;
            if (_newDocumentState.Name != null) yield return _newDocumentState;
        }
    }

    public partial class WorkspaceInputCheckpointGql : IGraphQlInputObject
    {
        private InputPropertyInfo _updatedAt;
        private InputPropertyInfo _lastDocumentId;

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<DateTimeOffset?>))]
        #endif
        public QueryBuilderParameter<DateTimeOffset?>? UpdatedAt
        {
            get => (QueryBuilderParameter<DateTimeOffset?>?)_updatedAt.Value;
            set => _updatedAt = new InputPropertyInfo { Name = "updatedAt", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<Guid?>))]
        #endif
        public QueryBuilderParameter<Guid?>? LastDocumentId
        {
            get => (QueryBuilderParameter<Guid?>?)_lastDocumentId.Value;
            set => _lastDocumentId = new InputPropertyInfo { Name = "lastDocumentId", Value = value };
        }

        IEnumerable<InputPropertyInfo> IGraphQlInputObject.GetPropertyValues()
        {
            if (_updatedAt.Name != null) yield return _updatedAt;
            if (_lastDocumentId.Name != null) yield return _lastDocumentId;
        }
    }

    public partial class WorkspaceInputPushRowGql : IGraphQlInputObject
    {
        private InputPropertyInfo _assumedMasterState;
        private InputPropertyInfo _newDocumentState;

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<WorkspaceInputGql?>))]
        #endif
        public QueryBuilderParameter<WorkspaceInputGql?>? AssumedMasterState
        {
            get => (QueryBuilderParameter<WorkspaceInputGql?>?)_assumedMasterState.Value;
            set => _assumedMasterState = new InputPropertyInfo { Name = "assumedMasterState", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<WorkspaceInputGql?>))]
        #endif
        public QueryBuilderParameter<WorkspaceInputGql?>? NewDocumentState
        {
            get => (QueryBuilderParameter<WorkspaceInputGql?>?)_newDocumentState.Value;
            set => _newDocumentState = new InputPropertyInfo { Name = "newDocumentState", Value = value };
        }

        IEnumerable<InputPropertyInfo> IGraphQlInputObject.GetPropertyValues()
        {
            if (_assumedMasterState.Name != null) yield return _assumedMasterState;
            if (_newDocumentState.Name != null) yield return _newDocumentState;
        }
    }

    public partial class LiveDocInputCheckpointGql : IGraphQlInputObject
    {
        private InputPropertyInfo _updatedAt;
        private InputPropertyInfo _lastDocumentId;

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<DateTimeOffset?>))]
        #endif
        public QueryBuilderParameter<DateTimeOffset?>? UpdatedAt
        {
            get => (QueryBuilderParameter<DateTimeOffset?>?)_updatedAt.Value;
            set => _updatedAt = new InputPropertyInfo { Name = "updatedAt", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<Guid?>))]
        #endif
        public QueryBuilderParameter<Guid?>? LastDocumentId
        {
            get => (QueryBuilderParameter<Guid?>?)_lastDocumentId.Value;
            set => _lastDocumentId = new InputPropertyInfo { Name = "lastDocumentId", Value = value };
        }

        IEnumerable<InputPropertyInfo> IGraphQlInputObject.GetPropertyValues()
        {
            if (_updatedAt.Name != null) yield return _updatedAt;
            if (_lastDocumentId.Name != null) yield return _lastDocumentId;
        }
    }

    public partial class LiveDocInputPushRowGql : IGraphQlInputObject
    {
        private InputPropertyInfo _assumedMasterState;
        private InputPropertyInfo _newDocumentState;

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<LiveDocInputGql?>))]
        #endif
        public QueryBuilderParameter<LiveDocInputGql?>? AssumedMasterState
        {
            get => (QueryBuilderParameter<LiveDocInputGql?>?)_assumedMasterState.Value;
            set => _assumedMasterState = new InputPropertyInfo { Name = "assumedMasterState", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<LiveDocInputGql?>))]
        #endif
        public QueryBuilderParameter<LiveDocInputGql?>? NewDocumentState
        {
            get => (QueryBuilderParameter<LiveDocInputGql?>?)_newDocumentState.Value;
            set => _newDocumentState = new InputPropertyInfo { Name = "newDocumentState", Value = value };
        }

        IEnumerable<InputPropertyInfo> IGraphQlInputObject.GetPropertyValues()
        {
            if (_assumedMasterState.Name != null) yield return _assumedMasterState;
            if (_newDocumentState.Name != null) yield return _newDocumentState;
        }
    }

    public partial class UserFilterInputGql : IGraphQlInputObject
    {
        private InputPropertyInfo _and;
        private InputPropertyInfo _or;
        private InputPropertyInfo _firstName;
        private InputPropertyInfo _lastName;
        private InputPropertyInfo _fullName;
        private InputPropertyInfo _email;
        private InputPropertyInfo _role;
        private InputPropertyInfo _jwtAccessToken;
        private InputPropertyInfo _workspaceId;
        private InputPropertyInfo _id;
        private InputPropertyInfo _isDeleted;
        private InputPropertyInfo _updatedAt;
        private InputPropertyInfo _topics;

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<ICollection<UserFilterInputGql>?>))]
        #endif
        public QueryBuilderParameter<ICollection<UserFilterInputGql>?>? And
        {
            get => (QueryBuilderParameter<ICollection<UserFilterInputGql>?>?)_and.Value;
            set => _and = new InputPropertyInfo { Name = "and", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<ICollection<UserFilterInputGql>?>))]
        #endif
        public QueryBuilderParameter<ICollection<UserFilterInputGql>?>? Or
        {
            get => (QueryBuilderParameter<ICollection<UserFilterInputGql>?>?)_or.Value;
            set => _or = new InputPropertyInfo { Name = "or", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<StringOperationFilterInputGql?>))]
        #endif
        public QueryBuilderParameter<StringOperationFilterInputGql?>? FirstName
        {
            get => (QueryBuilderParameter<StringOperationFilterInputGql?>?)_firstName.Value;
            set => _firstName = new InputPropertyInfo { Name = "firstName", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<StringOperationFilterInputGql?>))]
        #endif
        public QueryBuilderParameter<StringOperationFilterInputGql?>? LastName
        {
            get => (QueryBuilderParameter<StringOperationFilterInputGql?>?)_lastName.Value;
            set => _lastName = new InputPropertyInfo { Name = "lastName", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<StringOperationFilterInputGql?>))]
        #endif
        public QueryBuilderParameter<StringOperationFilterInputGql?>? FullName
        {
            get => (QueryBuilderParameter<StringOperationFilterInputGql?>?)_fullName.Value;
            set => _fullName = new InputPropertyInfo { Name = "fullName", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<StringOperationFilterInputGql?>))]
        #endif
        public QueryBuilderParameter<StringOperationFilterInputGql?>? Email
        {
            get => (QueryBuilderParameter<StringOperationFilterInputGql?>?)_email.Value;
            set => _email = new InputPropertyInfo { Name = "email", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<UserRoleOperationFilterInputGql?>))]
        #endif
        public QueryBuilderParameter<UserRoleOperationFilterInputGql?>? Role
        {
            get => (QueryBuilderParameter<UserRoleOperationFilterInputGql?>?)_role.Value;
            set => _role = new InputPropertyInfo { Name = "role", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<StringOperationFilterInputGql?>))]
        #endif
        public QueryBuilderParameter<StringOperationFilterInputGql?>? JwtAccessToken
        {
            get => (QueryBuilderParameter<StringOperationFilterInputGql?>?)_jwtAccessToken.Value;
            set => _jwtAccessToken = new InputPropertyInfo { Name = "jwtAccessToken", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<UuidOperationFilterInputGql?>))]
        #endif
        public QueryBuilderParameter<UuidOperationFilterInputGql?>? WorkspaceId
        {
            get => (QueryBuilderParameter<UuidOperationFilterInputGql?>?)_workspaceId.Value;
            set => _workspaceId = new InputPropertyInfo { Name = "workspaceId", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<UuidOperationFilterInputGql?>))]
        #endif
        public QueryBuilderParameter<UuidOperationFilterInputGql?>? Id
        {
            get => (QueryBuilderParameter<UuidOperationFilterInputGql?>?)_id.Value;
            set => _id = new InputPropertyInfo { Name = "id", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<BooleanOperationFilterInputGql?>))]
        #endif
        public QueryBuilderParameter<BooleanOperationFilterInputGql?>? IsDeleted
        {
            get => (QueryBuilderParameter<BooleanOperationFilterInputGql?>?)_isDeleted.Value;
            set => _isDeleted = new InputPropertyInfo { Name = "isDeleted", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<DateTimeOperationFilterInputGql?>))]
        #endif
        public QueryBuilderParameter<DateTimeOperationFilterInputGql?>? UpdatedAt
        {
            get => (QueryBuilderParameter<DateTimeOperationFilterInputGql?>?)_updatedAt.Value;
            set => _updatedAt = new InputPropertyInfo { Name = "updatedAt", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<ListStringOperationFilterInputGql?>))]
        #endif
        public QueryBuilderParameter<ListStringOperationFilterInputGql?>? Topics
        {
            get => (QueryBuilderParameter<ListStringOperationFilterInputGql?>?)_topics.Value;
            set => _topics = new InputPropertyInfo { Name = "topics", Value = value };
        }

        IEnumerable<InputPropertyInfo> IGraphQlInputObject.GetPropertyValues()
        {
            if (_and.Name != null) yield return _and;
            if (_or.Name != null) yield return _or;
            if (_firstName.Name != null) yield return _firstName;
            if (_lastName.Name != null) yield return _lastName;
            if (_fullName.Name != null) yield return _fullName;
            if (_email.Name != null) yield return _email;
            if (_role.Name != null) yield return _role;
            if (_jwtAccessToken.Name != null) yield return _jwtAccessToken;
            if (_workspaceId.Name != null) yield return _workspaceId;
            if (_id.Name != null) yield return _id;
            if (_isDeleted.Name != null) yield return _isDeleted;
            if (_updatedAt.Name != null) yield return _updatedAt;
            if (_topics.Name != null) yield return _topics;
        }
    }

    public partial class UserInputGql : IGraphQlInputObject
    {
        private InputPropertyInfo _firstName;
        private InputPropertyInfo _lastName;
        private InputPropertyInfo _fullName;
        private InputPropertyInfo _email;
        private InputPropertyInfo _role;
        private InputPropertyInfo _jwtAccessToken;
        private InputPropertyInfo _workspaceId;
        private InputPropertyInfo _id;
        private InputPropertyInfo _isDeleted;
        private InputPropertyInfo _updatedAt;
        private InputPropertyInfo _topics;

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<string>))]
        #endif
        public QueryBuilderParameter<string>? FirstName
        {
            get => (QueryBuilderParameter<string>?)_firstName.Value;
            set => _firstName = new InputPropertyInfo { Name = "firstName", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<string>))]
        #endif
        public QueryBuilderParameter<string>? LastName
        {
            get => (QueryBuilderParameter<string>?)_lastName.Value;
            set => _lastName = new InputPropertyInfo { Name = "lastName", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<string?>))]
        #endif
        public QueryBuilderParameter<string?>? FullName
        {
            get => (QueryBuilderParameter<string?>?)_fullName.Value;
            set => _fullName = new InputPropertyInfo { Name = "fullName", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<string?>))]
        #endif
        public QueryBuilderParameter<string?>? Email
        {
            get => (QueryBuilderParameter<string?>?)_email.Value;
            set => _email = new InputPropertyInfo { Name = "email", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<LiveDocs.GraphQLApi.Security.UserRole>))]
        #endif
        public QueryBuilderParameter<LiveDocs.GraphQLApi.Security.UserRole>? Role
        {
            get => (QueryBuilderParameter<LiveDocs.GraphQLApi.Security.UserRole>?)_role.Value;
            set => _role = new InputPropertyInfo { Name = "role", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<string?>))]
        #endif
        public QueryBuilderParameter<string?>? JwtAccessToken
        {
            get => (QueryBuilderParameter<string?>?)_jwtAccessToken.Value;
            set => _jwtAccessToken = new InputPropertyInfo { Name = "jwtAccessToken", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<Guid>))]
        #endif
        public QueryBuilderParameter<Guid>? WorkspaceId
        {
            get => (QueryBuilderParameter<Guid>?)_workspaceId.Value;
            set => _workspaceId = new InputPropertyInfo { Name = "workspaceId", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<Guid>))]
        #endif
        public QueryBuilderParameter<Guid>? Id
        {
            get => (QueryBuilderParameter<Guid>?)_id.Value;
            set => _id = new InputPropertyInfo { Name = "id", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<bool>))]
        #endif
        public QueryBuilderParameter<bool>? IsDeleted
        {
            get => (QueryBuilderParameter<bool>?)_isDeleted.Value;
            set => _isDeleted = new InputPropertyInfo { Name = "isDeleted", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<DateTimeOffset>))]
        #endif
        public QueryBuilderParameter<DateTimeOffset>? UpdatedAt
        {
            get => (QueryBuilderParameter<DateTimeOffset>?)_updatedAt.Value;
            set => _updatedAt = new InputPropertyInfo { Name = "updatedAt", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<ICollection<string>?>))]
        #endif
        public QueryBuilderParameter<ICollection<string>?>? Topics
        {
            get => (QueryBuilderParameter<ICollection<string>?>?)_topics.Value;
            set => _topics = new InputPropertyInfo { Name = "topics", Value = value };
        }

        IEnumerable<InputPropertyInfo> IGraphQlInputObject.GetPropertyValues()
        {
            if (_firstName.Name != null) yield return _firstName;
            if (_lastName.Name != null) yield return _lastName;
            if (_fullName.Name != null) yield return _fullName;
            if (_email.Name != null) yield return _email;
            if (_role.Name != null) yield return _role;
            if (_jwtAccessToken.Name != null) yield return _jwtAccessToken;
            if (_workspaceId.Name != null) yield return _workspaceId;
            if (_id.Name != null) yield return _id;
            if (_isDeleted.Name != null) yield return _isDeleted;
            if (_updatedAt.Name != null) yield return _updatedAt;
            if (_topics.Name != null) yield return _topics;
        }
    }

    public partial class UserInputHeadersGql : IGraphQlInputObject
    {
        private InputPropertyInfo _authorization;

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<string>))]
        #endif
        public QueryBuilderParameter<string>? Authorization
        {
            get => (QueryBuilderParameter<string>?)_authorization.Value;
            set => _authorization = new InputPropertyInfo { Name = "Authorization", Value = value };
        }

        IEnumerable<InputPropertyInfo> IGraphQlInputObject.GetPropertyValues()
        {
            if (_authorization.Name != null) yield return _authorization;
        }
    }

    public partial class WorkspaceFilterInputGql : IGraphQlInputObject
    {
        private InputPropertyInfo _and;
        private InputPropertyInfo _or;
        private InputPropertyInfo _name;
        private InputPropertyInfo _id;
        private InputPropertyInfo _isDeleted;
        private InputPropertyInfo _updatedAt;
        private InputPropertyInfo _topics;

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<ICollection<WorkspaceFilterInputGql>?>))]
        #endif
        public QueryBuilderParameter<ICollection<WorkspaceFilterInputGql>?>? And
        {
            get => (QueryBuilderParameter<ICollection<WorkspaceFilterInputGql>?>?)_and.Value;
            set => _and = new InputPropertyInfo { Name = "and", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<ICollection<WorkspaceFilterInputGql>?>))]
        #endif
        public QueryBuilderParameter<ICollection<WorkspaceFilterInputGql>?>? Or
        {
            get => (QueryBuilderParameter<ICollection<WorkspaceFilterInputGql>?>?)_or.Value;
            set => _or = new InputPropertyInfo { Name = "or", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<StringOperationFilterInputGql?>))]
        #endif
        public QueryBuilderParameter<StringOperationFilterInputGql?>? Name
        {
            get => (QueryBuilderParameter<StringOperationFilterInputGql?>?)_name.Value;
            set => _name = new InputPropertyInfo { Name = "name", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<UuidOperationFilterInputGql?>))]
        #endif
        public QueryBuilderParameter<UuidOperationFilterInputGql?>? Id
        {
            get => (QueryBuilderParameter<UuidOperationFilterInputGql?>?)_id.Value;
            set => _id = new InputPropertyInfo { Name = "id", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<BooleanOperationFilterInputGql?>))]
        #endif
        public QueryBuilderParameter<BooleanOperationFilterInputGql?>? IsDeleted
        {
            get => (QueryBuilderParameter<BooleanOperationFilterInputGql?>?)_isDeleted.Value;
            set => _isDeleted = new InputPropertyInfo { Name = "isDeleted", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<DateTimeOperationFilterInputGql?>))]
        #endif
        public QueryBuilderParameter<DateTimeOperationFilterInputGql?>? UpdatedAt
        {
            get => (QueryBuilderParameter<DateTimeOperationFilterInputGql?>?)_updatedAt.Value;
            set => _updatedAt = new InputPropertyInfo { Name = "updatedAt", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<ListStringOperationFilterInputGql?>))]
        #endif
        public QueryBuilderParameter<ListStringOperationFilterInputGql?>? Topics
        {
            get => (QueryBuilderParameter<ListStringOperationFilterInputGql?>?)_topics.Value;
            set => _topics = new InputPropertyInfo { Name = "topics", Value = value };
        }

        IEnumerable<InputPropertyInfo> IGraphQlInputObject.GetPropertyValues()
        {
            if (_and.Name != null) yield return _and;
            if (_or.Name != null) yield return _or;
            if (_name.Name != null) yield return _name;
            if (_id.Name != null) yield return _id;
            if (_isDeleted.Name != null) yield return _isDeleted;
            if (_updatedAt.Name != null) yield return _updatedAt;
            if (_topics.Name != null) yield return _topics;
        }
    }

    public partial class WorkspaceInputGql : IGraphQlInputObject
    {
        private InputPropertyInfo _name;
        private InputPropertyInfo _id;
        private InputPropertyInfo _isDeleted;
        private InputPropertyInfo _updatedAt;
        private InputPropertyInfo _topics;

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<string>))]
        #endif
        public QueryBuilderParameter<string>? Name
        {
            get => (QueryBuilderParameter<string>?)_name.Value;
            set => _name = new InputPropertyInfo { Name = "name", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<Guid>))]
        #endif
        public QueryBuilderParameter<Guid>? Id
        {
            get => (QueryBuilderParameter<Guid>?)_id.Value;
            set => _id = new InputPropertyInfo { Name = "id", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<bool>))]
        #endif
        public QueryBuilderParameter<bool>? IsDeleted
        {
            get => (QueryBuilderParameter<bool>?)_isDeleted.Value;
            set => _isDeleted = new InputPropertyInfo { Name = "isDeleted", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<DateTimeOffset>))]
        #endif
        public QueryBuilderParameter<DateTimeOffset>? UpdatedAt
        {
            get => (QueryBuilderParameter<DateTimeOffset>?)_updatedAt.Value;
            set => _updatedAt = new InputPropertyInfo { Name = "updatedAt", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<ICollection<string>?>))]
        #endif
        public QueryBuilderParameter<ICollection<string>?>? Topics
        {
            get => (QueryBuilderParameter<ICollection<string>?>?)_topics.Value;
            set => _topics = new InputPropertyInfo { Name = "topics", Value = value };
        }

        IEnumerable<InputPropertyInfo> IGraphQlInputObject.GetPropertyValues()
        {
            if (_name.Name != null) yield return _name;
            if (_id.Name != null) yield return _id;
            if (_isDeleted.Name != null) yield return _isDeleted;
            if (_updatedAt.Name != null) yield return _updatedAt;
            if (_topics.Name != null) yield return _topics;
        }
    }

    public partial class WorkspaceInputHeadersGql : IGraphQlInputObject
    {
        private InputPropertyInfo _authorization;

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<string>))]
        #endif
        public QueryBuilderParameter<string>? Authorization
        {
            get => (QueryBuilderParameter<string>?)_authorization.Value;
            set => _authorization = new InputPropertyInfo { Name = "Authorization", Value = value };
        }

        IEnumerable<InputPropertyInfo> IGraphQlInputObject.GetPropertyValues()
        {
            if (_authorization.Name != null) yield return _authorization;
        }
    }

    public partial class LiveDocFilterInputGql : IGraphQlInputObject
    {
        private InputPropertyInfo _and;
        private InputPropertyInfo _or;
        private InputPropertyInfo _content;
        private InputPropertyInfo _ownerId;
        private InputPropertyInfo _workspaceId;
        private InputPropertyInfo _id;
        private InputPropertyInfo _isDeleted;
        private InputPropertyInfo _updatedAt;
        private InputPropertyInfo _topics;

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<ICollection<LiveDocFilterInputGql>?>))]
        #endif
        public QueryBuilderParameter<ICollection<LiveDocFilterInputGql>?>? And
        {
            get => (QueryBuilderParameter<ICollection<LiveDocFilterInputGql>?>?)_and.Value;
            set => _and = new InputPropertyInfo { Name = "and", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<ICollection<LiveDocFilterInputGql>?>))]
        #endif
        public QueryBuilderParameter<ICollection<LiveDocFilterInputGql>?>? Or
        {
            get => (QueryBuilderParameter<ICollection<LiveDocFilterInputGql>?>?)_or.Value;
            set => _or = new InputPropertyInfo { Name = "or", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<StringOperationFilterInputGql?>))]
        #endif
        public QueryBuilderParameter<StringOperationFilterInputGql?>? Content
        {
            get => (QueryBuilderParameter<StringOperationFilterInputGql?>?)_content.Value;
            set => _content = new InputPropertyInfo { Name = "content", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<UuidOperationFilterInputGql?>))]
        #endif
        public QueryBuilderParameter<UuidOperationFilterInputGql?>? OwnerId
        {
            get => (QueryBuilderParameter<UuidOperationFilterInputGql?>?)_ownerId.Value;
            set => _ownerId = new InputPropertyInfo { Name = "ownerId", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<UuidOperationFilterInputGql?>))]
        #endif
        public QueryBuilderParameter<UuidOperationFilterInputGql?>? WorkspaceId
        {
            get => (QueryBuilderParameter<UuidOperationFilterInputGql?>?)_workspaceId.Value;
            set => _workspaceId = new InputPropertyInfo { Name = "workspaceId", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<UuidOperationFilterInputGql?>))]
        #endif
        public QueryBuilderParameter<UuidOperationFilterInputGql?>? Id
        {
            get => (QueryBuilderParameter<UuidOperationFilterInputGql?>?)_id.Value;
            set => _id = new InputPropertyInfo { Name = "id", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<BooleanOperationFilterInputGql?>))]
        #endif
        public QueryBuilderParameter<BooleanOperationFilterInputGql?>? IsDeleted
        {
            get => (QueryBuilderParameter<BooleanOperationFilterInputGql?>?)_isDeleted.Value;
            set => _isDeleted = new InputPropertyInfo { Name = "isDeleted", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<DateTimeOperationFilterInputGql?>))]
        #endif
        public QueryBuilderParameter<DateTimeOperationFilterInputGql?>? UpdatedAt
        {
            get => (QueryBuilderParameter<DateTimeOperationFilterInputGql?>?)_updatedAt.Value;
            set => _updatedAt = new InputPropertyInfo { Name = "updatedAt", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<ListStringOperationFilterInputGql?>))]
        #endif
        public QueryBuilderParameter<ListStringOperationFilterInputGql?>? Topics
        {
            get => (QueryBuilderParameter<ListStringOperationFilterInputGql?>?)_topics.Value;
            set => _topics = new InputPropertyInfo { Name = "topics", Value = value };
        }

        IEnumerable<InputPropertyInfo> IGraphQlInputObject.GetPropertyValues()
        {
            if (_and.Name != null) yield return _and;
            if (_or.Name != null) yield return _or;
            if (_content.Name != null) yield return _content;
            if (_ownerId.Name != null) yield return _ownerId;
            if (_workspaceId.Name != null) yield return _workspaceId;
            if (_id.Name != null) yield return _id;
            if (_isDeleted.Name != null) yield return _isDeleted;
            if (_updatedAt.Name != null) yield return _updatedAt;
            if (_topics.Name != null) yield return _topics;
        }
    }

    public partial class LiveDocInputGql : IGraphQlInputObject
    {
        private InputPropertyInfo _content;
        private InputPropertyInfo _ownerId;
        private InputPropertyInfo _workspaceId;
        private InputPropertyInfo _id;
        private InputPropertyInfo _isDeleted;
        private InputPropertyInfo _updatedAt;
        private InputPropertyInfo _topics;

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<string>))]
        #endif
        public QueryBuilderParameter<string>? Content
        {
            get => (QueryBuilderParameter<string>?)_content.Value;
            set => _content = new InputPropertyInfo { Name = "content", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<Guid>))]
        #endif
        public QueryBuilderParameter<Guid>? OwnerId
        {
            get => (QueryBuilderParameter<Guid>?)_ownerId.Value;
            set => _ownerId = new InputPropertyInfo { Name = "ownerId", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<Guid>))]
        #endif
        public QueryBuilderParameter<Guid>? WorkspaceId
        {
            get => (QueryBuilderParameter<Guid>?)_workspaceId.Value;
            set => _workspaceId = new InputPropertyInfo { Name = "workspaceId", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<Guid>))]
        #endif
        public QueryBuilderParameter<Guid>? Id
        {
            get => (QueryBuilderParameter<Guid>?)_id.Value;
            set => _id = new InputPropertyInfo { Name = "id", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<bool>))]
        #endif
        public QueryBuilderParameter<bool>? IsDeleted
        {
            get => (QueryBuilderParameter<bool>?)_isDeleted.Value;
            set => _isDeleted = new InputPropertyInfo { Name = "isDeleted", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<DateTimeOffset>))]
        #endif
        public QueryBuilderParameter<DateTimeOffset>? UpdatedAt
        {
            get => (QueryBuilderParameter<DateTimeOffset>?)_updatedAt.Value;
            set => _updatedAt = new InputPropertyInfo { Name = "updatedAt", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<ICollection<string>?>))]
        #endif
        public QueryBuilderParameter<ICollection<string>?>? Topics
        {
            get => (QueryBuilderParameter<ICollection<string>?>?)_topics.Value;
            set => _topics = new InputPropertyInfo { Name = "topics", Value = value };
        }

        IEnumerable<InputPropertyInfo> IGraphQlInputObject.GetPropertyValues()
        {
            if (_content.Name != null) yield return _content;
            if (_ownerId.Name != null) yield return _ownerId;
            if (_workspaceId.Name != null) yield return _workspaceId;
            if (_id.Name != null) yield return _id;
            if (_isDeleted.Name != null) yield return _isDeleted;
            if (_updatedAt.Name != null) yield return _updatedAt;
            if (_topics.Name != null) yield return _topics;
        }
    }

    public partial class LiveDocInputHeadersGql : IGraphQlInputObject
    {
        private InputPropertyInfo _authorization;

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<string>))]
        #endif
        public QueryBuilderParameter<string>? Authorization
        {
            get => (QueryBuilderParameter<string>?)_authorization.Value;
            set => _authorization = new InputPropertyInfo { Name = "Authorization", Value = value };
        }

        IEnumerable<InputPropertyInfo> IGraphQlInputObject.GetPropertyValues()
        {
            if (_authorization.Name != null) yield return _authorization;
        }
    }

    public partial class StringOperationFilterInputGql : IGraphQlInputObject
    {
        private InputPropertyInfo _and;
        private InputPropertyInfo _or;
        private InputPropertyInfo _eq;
        private InputPropertyInfo _neq;
        private InputPropertyInfo _contains;
        private InputPropertyInfo _ncontains;
        private InputPropertyInfo _in;
        private InputPropertyInfo _nin;
        private InputPropertyInfo _startsWith;
        private InputPropertyInfo _nstartsWith;
        private InputPropertyInfo _endsWith;
        private InputPropertyInfo _nendsWith;

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<ICollection<StringOperationFilterInputGql>?>))]
        #endif
        public QueryBuilderParameter<ICollection<StringOperationFilterInputGql>?>? And
        {
            get => (QueryBuilderParameter<ICollection<StringOperationFilterInputGql>?>?)_and.Value;
            set => _and = new InputPropertyInfo { Name = "and", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<ICollection<StringOperationFilterInputGql>?>))]
        #endif
        public QueryBuilderParameter<ICollection<StringOperationFilterInputGql>?>? Or
        {
            get => (QueryBuilderParameter<ICollection<StringOperationFilterInputGql>?>?)_or.Value;
            set => _or = new InputPropertyInfo { Name = "or", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<string?>))]
        #endif
        public QueryBuilderParameter<string?>? Eq
        {
            get => (QueryBuilderParameter<string?>?)_eq.Value;
            set => _eq = new InputPropertyInfo { Name = "eq", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<string?>))]
        #endif
        public QueryBuilderParameter<string?>? Neq
        {
            get => (QueryBuilderParameter<string?>?)_neq.Value;
            set => _neq = new InputPropertyInfo { Name = "neq", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<string?>))]
        #endif
        public QueryBuilderParameter<string?>? Contains
        {
            get => (QueryBuilderParameter<string?>?)_contains.Value;
            set => _contains = new InputPropertyInfo { Name = "contains", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<string?>))]
        #endif
        public QueryBuilderParameter<string?>? Ncontains
        {
            get => (QueryBuilderParameter<string?>?)_ncontains.Value;
            set => _ncontains = new InputPropertyInfo { Name = "ncontains", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<ICollection<string?>?>))]
        #endif
        public QueryBuilderParameter<ICollection<string?>?>? In
        {
            get => (QueryBuilderParameter<ICollection<string?>?>?)_in.Value;
            set => _in = new InputPropertyInfo { Name = "in", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<ICollection<string?>?>))]
        #endif
        public QueryBuilderParameter<ICollection<string?>?>? Nin
        {
            get => (QueryBuilderParameter<ICollection<string?>?>?)_nin.Value;
            set => _nin = new InputPropertyInfo { Name = "nin", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<string?>))]
        #endif
        public QueryBuilderParameter<string?>? StartsWith
        {
            get => (QueryBuilderParameter<string?>?)_startsWith.Value;
            set => _startsWith = new InputPropertyInfo { Name = "startsWith", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<string?>))]
        #endif
        public QueryBuilderParameter<string?>? NstartsWith
        {
            get => (QueryBuilderParameter<string?>?)_nstartsWith.Value;
            set => _nstartsWith = new InputPropertyInfo { Name = "nstartsWith", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<string?>))]
        #endif
        public QueryBuilderParameter<string?>? EndsWith
        {
            get => (QueryBuilderParameter<string?>?)_endsWith.Value;
            set => _endsWith = new InputPropertyInfo { Name = "endsWith", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<string?>))]
        #endif
        public QueryBuilderParameter<string?>? NendsWith
        {
            get => (QueryBuilderParameter<string?>?)_nendsWith.Value;
            set => _nendsWith = new InputPropertyInfo { Name = "nendsWith", Value = value };
        }

        IEnumerable<InputPropertyInfo> IGraphQlInputObject.GetPropertyValues()
        {
            if (_and.Name != null) yield return _and;
            if (_or.Name != null) yield return _or;
            if (_eq.Name != null) yield return _eq;
            if (_neq.Name != null) yield return _neq;
            if (_contains.Name != null) yield return _contains;
            if (_ncontains.Name != null) yield return _ncontains;
            if (_in.Name != null) yield return _in;
            if (_nin.Name != null) yield return _nin;
            if (_startsWith.Name != null) yield return _startsWith;
            if (_nstartsWith.Name != null) yield return _nstartsWith;
            if (_endsWith.Name != null) yield return _endsWith;
            if (_nendsWith.Name != null) yield return _nendsWith;
        }
    }

    public partial class UserRoleOperationFilterInputGql : IGraphQlInputObject
    {
        private InputPropertyInfo _eq;
        private InputPropertyInfo _neq;
        private InputPropertyInfo _in;
        private InputPropertyInfo _nin;

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<LiveDocs.GraphQLApi.Security.UserRole?>))]
        #endif
        public QueryBuilderParameter<LiveDocs.GraphQLApi.Security.UserRole?>? Eq
        {
            get => (QueryBuilderParameter<LiveDocs.GraphQLApi.Security.UserRole?>?)_eq.Value;
            set => _eq = new InputPropertyInfo { Name = "eq", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<LiveDocs.GraphQLApi.Security.UserRole?>))]
        #endif
        public QueryBuilderParameter<LiveDocs.GraphQLApi.Security.UserRole?>? Neq
        {
            get => (QueryBuilderParameter<LiveDocs.GraphQLApi.Security.UserRole?>?)_neq.Value;
            set => _neq = new InputPropertyInfo { Name = "neq", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<ICollection<LiveDocs.GraphQLApi.Security.UserRole>?>))]
        #endif
        public QueryBuilderParameter<ICollection<LiveDocs.GraphQLApi.Security.UserRole>?>? In
        {
            get => (QueryBuilderParameter<ICollection<LiveDocs.GraphQLApi.Security.UserRole>?>?)_in.Value;
            set => _in = new InputPropertyInfo { Name = "in", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<ICollection<LiveDocs.GraphQLApi.Security.UserRole>?>))]
        #endif
        public QueryBuilderParameter<ICollection<LiveDocs.GraphQLApi.Security.UserRole>?>? Nin
        {
            get => (QueryBuilderParameter<ICollection<LiveDocs.GraphQLApi.Security.UserRole>?>?)_nin.Value;
            set => _nin = new InputPropertyInfo { Name = "nin", Value = value };
        }

        IEnumerable<InputPropertyInfo> IGraphQlInputObject.GetPropertyValues()
        {
            if (_eq.Name != null) yield return _eq;
            if (_neq.Name != null) yield return _neq;
            if (_in.Name != null) yield return _in;
            if (_nin.Name != null) yield return _nin;
        }
    }

    public partial class UuidOperationFilterInputGql : IGraphQlInputObject
    {
        private InputPropertyInfo _eq;
        private InputPropertyInfo _neq;
        private InputPropertyInfo _in;
        private InputPropertyInfo _nin;
        private InputPropertyInfo _gt;
        private InputPropertyInfo _ngt;
        private InputPropertyInfo _gte;
        private InputPropertyInfo _ngte;
        private InputPropertyInfo _lt;
        private InputPropertyInfo _nlt;
        private InputPropertyInfo _lte;
        private InputPropertyInfo _nlte;

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<Guid?>))]
        #endif
        public QueryBuilderParameter<Guid?>? Eq
        {
            get => (QueryBuilderParameter<Guid?>?)_eq.Value;
            set => _eq = new InputPropertyInfo { Name = "eq", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<Guid?>))]
        #endif
        public QueryBuilderParameter<Guid?>? Neq
        {
            get => (QueryBuilderParameter<Guid?>?)_neq.Value;
            set => _neq = new InputPropertyInfo { Name = "neq", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<ICollection<Guid?>?>))]
        #endif
        public QueryBuilderParameter<ICollection<Guid?>?>? In
        {
            get => (QueryBuilderParameter<ICollection<Guid?>?>?)_in.Value;
            set => _in = new InputPropertyInfo { Name = "in", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<ICollection<Guid?>?>))]
        #endif
        public QueryBuilderParameter<ICollection<Guid?>?>? Nin
        {
            get => (QueryBuilderParameter<ICollection<Guid?>?>?)_nin.Value;
            set => _nin = new InputPropertyInfo { Name = "nin", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<Guid?>))]
        #endif
        public QueryBuilderParameter<Guid?>? Gt
        {
            get => (QueryBuilderParameter<Guid?>?)_gt.Value;
            set => _gt = new InputPropertyInfo { Name = "gt", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<Guid?>))]
        #endif
        public QueryBuilderParameter<Guid?>? Ngt
        {
            get => (QueryBuilderParameter<Guid?>?)_ngt.Value;
            set => _ngt = new InputPropertyInfo { Name = "ngt", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<Guid?>))]
        #endif
        public QueryBuilderParameter<Guid?>? Gte
        {
            get => (QueryBuilderParameter<Guid?>?)_gte.Value;
            set => _gte = new InputPropertyInfo { Name = "gte", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<Guid?>))]
        #endif
        public QueryBuilderParameter<Guid?>? Ngte
        {
            get => (QueryBuilderParameter<Guid?>?)_ngte.Value;
            set => _ngte = new InputPropertyInfo { Name = "ngte", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<Guid?>))]
        #endif
        public QueryBuilderParameter<Guid?>? Lt
        {
            get => (QueryBuilderParameter<Guid?>?)_lt.Value;
            set => _lt = new InputPropertyInfo { Name = "lt", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<Guid?>))]
        #endif
        public QueryBuilderParameter<Guid?>? Nlt
        {
            get => (QueryBuilderParameter<Guid?>?)_nlt.Value;
            set => _nlt = new InputPropertyInfo { Name = "nlt", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<Guid?>))]
        #endif
        public QueryBuilderParameter<Guid?>? Lte
        {
            get => (QueryBuilderParameter<Guid?>?)_lte.Value;
            set => _lte = new InputPropertyInfo { Name = "lte", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<Guid?>))]
        #endif
        public QueryBuilderParameter<Guid?>? Nlte
        {
            get => (QueryBuilderParameter<Guid?>?)_nlte.Value;
            set => _nlte = new InputPropertyInfo { Name = "nlte", Value = value };
        }

        IEnumerable<InputPropertyInfo> IGraphQlInputObject.GetPropertyValues()
        {
            if (_eq.Name != null) yield return _eq;
            if (_neq.Name != null) yield return _neq;
            if (_in.Name != null) yield return _in;
            if (_nin.Name != null) yield return _nin;
            if (_gt.Name != null) yield return _gt;
            if (_ngt.Name != null) yield return _ngt;
            if (_gte.Name != null) yield return _gte;
            if (_ngte.Name != null) yield return _ngte;
            if (_lt.Name != null) yield return _lt;
            if (_nlt.Name != null) yield return _nlt;
            if (_lte.Name != null) yield return _lte;
            if (_nlte.Name != null) yield return _nlte;
        }
    }

    public partial class BooleanOperationFilterInputGql : IGraphQlInputObject
    {
        private InputPropertyInfo _eq;
        private InputPropertyInfo _neq;

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<bool?>))]
        #endif
        public QueryBuilderParameter<bool?>? Eq
        {
            get => (QueryBuilderParameter<bool?>?)_eq.Value;
            set => _eq = new InputPropertyInfo { Name = "eq", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<bool?>))]
        #endif
        public QueryBuilderParameter<bool?>? Neq
        {
            get => (QueryBuilderParameter<bool?>?)_neq.Value;
            set => _neq = new InputPropertyInfo { Name = "neq", Value = value };
        }

        IEnumerable<InputPropertyInfo> IGraphQlInputObject.GetPropertyValues()
        {
            if (_eq.Name != null) yield return _eq;
            if (_neq.Name != null) yield return _neq;
        }
    }

    public partial class DateTimeOperationFilterInputGql : IGraphQlInputObject
    {
        private InputPropertyInfo _eq;
        private InputPropertyInfo _neq;
        private InputPropertyInfo _in;
        private InputPropertyInfo _nin;
        private InputPropertyInfo _gt;
        private InputPropertyInfo _ngt;
        private InputPropertyInfo _gte;
        private InputPropertyInfo _ngte;
        private InputPropertyInfo _lt;
        private InputPropertyInfo _nlt;
        private InputPropertyInfo _lte;
        private InputPropertyInfo _nlte;

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<DateTimeOffset?>))]
        #endif
        public QueryBuilderParameter<DateTimeOffset?>? Eq
        {
            get => (QueryBuilderParameter<DateTimeOffset?>?)_eq.Value;
            set => _eq = new InputPropertyInfo { Name = "eq", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<DateTimeOffset?>))]
        #endif
        public QueryBuilderParameter<DateTimeOffset?>? Neq
        {
            get => (QueryBuilderParameter<DateTimeOffset?>?)_neq.Value;
            set => _neq = new InputPropertyInfo { Name = "neq", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<ICollection<DateTimeOffset?>?>))]
        #endif
        public QueryBuilderParameter<ICollection<DateTimeOffset?>?>? In
        {
            get => (QueryBuilderParameter<ICollection<DateTimeOffset?>?>?)_in.Value;
            set => _in = new InputPropertyInfo { Name = "in", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<ICollection<DateTimeOffset?>?>))]
        #endif
        public QueryBuilderParameter<ICollection<DateTimeOffset?>?>? Nin
        {
            get => (QueryBuilderParameter<ICollection<DateTimeOffset?>?>?)_nin.Value;
            set => _nin = new InputPropertyInfo { Name = "nin", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<DateTimeOffset?>))]
        #endif
        public QueryBuilderParameter<DateTimeOffset?>? Gt
        {
            get => (QueryBuilderParameter<DateTimeOffset?>?)_gt.Value;
            set => _gt = new InputPropertyInfo { Name = "gt", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<DateTimeOffset?>))]
        #endif
        public QueryBuilderParameter<DateTimeOffset?>? Ngt
        {
            get => (QueryBuilderParameter<DateTimeOffset?>?)_ngt.Value;
            set => _ngt = new InputPropertyInfo { Name = "ngt", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<DateTimeOffset?>))]
        #endif
        public QueryBuilderParameter<DateTimeOffset?>? Gte
        {
            get => (QueryBuilderParameter<DateTimeOffset?>?)_gte.Value;
            set => _gte = new InputPropertyInfo { Name = "gte", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<DateTimeOffset?>))]
        #endif
        public QueryBuilderParameter<DateTimeOffset?>? Ngte
        {
            get => (QueryBuilderParameter<DateTimeOffset?>?)_ngte.Value;
            set => _ngte = new InputPropertyInfo { Name = "ngte", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<DateTimeOffset?>))]
        #endif
        public QueryBuilderParameter<DateTimeOffset?>? Lt
        {
            get => (QueryBuilderParameter<DateTimeOffset?>?)_lt.Value;
            set => _lt = new InputPropertyInfo { Name = "lt", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<DateTimeOffset?>))]
        #endif
        public QueryBuilderParameter<DateTimeOffset?>? Nlt
        {
            get => (QueryBuilderParameter<DateTimeOffset?>?)_nlt.Value;
            set => _nlt = new InputPropertyInfo { Name = "nlt", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<DateTimeOffset?>))]
        #endif
        public QueryBuilderParameter<DateTimeOffset?>? Lte
        {
            get => (QueryBuilderParameter<DateTimeOffset?>?)_lte.Value;
            set => _lte = new InputPropertyInfo { Name = "lte", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<DateTimeOffset?>))]
        #endif
        public QueryBuilderParameter<DateTimeOffset?>? Nlte
        {
            get => (QueryBuilderParameter<DateTimeOffset?>?)_nlte.Value;
            set => _nlte = new InputPropertyInfo { Name = "nlte", Value = value };
        }

        IEnumerable<InputPropertyInfo> IGraphQlInputObject.GetPropertyValues()
        {
            if (_eq.Name != null) yield return _eq;
            if (_neq.Name != null) yield return _neq;
            if (_in.Name != null) yield return _in;
            if (_nin.Name != null) yield return _nin;
            if (_gt.Name != null) yield return _gt;
            if (_ngt.Name != null) yield return _ngt;
            if (_gte.Name != null) yield return _gte;
            if (_ngte.Name != null) yield return _ngte;
            if (_lt.Name != null) yield return _lt;
            if (_nlt.Name != null) yield return _nlt;
            if (_lte.Name != null) yield return _lte;
            if (_nlte.Name != null) yield return _nlte;
        }
    }

    public partial class ListStringOperationFilterInputGql : IGraphQlInputObject
    {
        private InputPropertyInfo _all;
        private InputPropertyInfo _none;
        private InputPropertyInfo _some;
        private InputPropertyInfo _any;

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<StringOperationFilterInputGql?>))]
        #endif
        public QueryBuilderParameter<StringOperationFilterInputGql?>? All
        {
            get => (QueryBuilderParameter<StringOperationFilterInputGql?>?)_all.Value;
            set => _all = new InputPropertyInfo { Name = "all", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<StringOperationFilterInputGql?>))]
        #endif
        public QueryBuilderParameter<StringOperationFilterInputGql?>? None
        {
            get => (QueryBuilderParameter<StringOperationFilterInputGql?>?)_none.Value;
            set => _none = new InputPropertyInfo { Name = "none", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<StringOperationFilterInputGql?>))]
        #endif
        public QueryBuilderParameter<StringOperationFilterInputGql?>? Some
        {
            get => (QueryBuilderParameter<StringOperationFilterInputGql?>?)_some.Value;
            set => _some = new InputPropertyInfo { Name = "some", Value = value };
        }

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<bool?>))]
        #endif
        public QueryBuilderParameter<bool?>? Any
        {
            get => (QueryBuilderParameter<bool?>?)_any.Value;
            set => _any = new InputPropertyInfo { Name = "any", Value = value };
        }

        IEnumerable<InputPropertyInfo> IGraphQlInputObject.GetPropertyValues()
        {
            if (_all.Name != null) yield return _all;
            if (_none.Name != null) yield return _none;
            if (_some.Name != null) yield return _some;
            if (_any.Name != null) yield return _any;
        }
    }

    public partial class PushUserInputGql : IGraphQlInputObject
    {
        private InputPropertyInfo _userPushRow;

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<ICollection<UserInputPushRowGql?>?>))]
        #endif
        public QueryBuilderParameter<ICollection<UserInputPushRowGql?>?>? UserPushRow
        {
            get => (QueryBuilderParameter<ICollection<UserInputPushRowGql?>?>?)_userPushRow.Value;
            set => _userPushRow = new InputPropertyInfo { Name = "userPushRow", Value = value };
        }

        IEnumerable<InputPropertyInfo> IGraphQlInputObject.GetPropertyValues()
        {
            if (_userPushRow.Name != null) yield return _userPushRow;
        }
    }

    public partial class PushWorkspaceInputGql : IGraphQlInputObject
    {
        private InputPropertyInfo _workspacePushRow;

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<ICollection<WorkspaceInputPushRowGql?>?>))]
        #endif
        public QueryBuilderParameter<ICollection<WorkspaceInputPushRowGql?>?>? WorkspacePushRow
        {
            get => (QueryBuilderParameter<ICollection<WorkspaceInputPushRowGql?>?>?)_workspacePushRow.Value;
            set => _workspacePushRow = new InputPropertyInfo { Name = "workspacePushRow", Value = value };
        }

        IEnumerable<InputPropertyInfo> IGraphQlInputObject.GetPropertyValues()
        {
            if (_workspacePushRow.Name != null) yield return _workspacePushRow;
        }
    }

    public partial class PushLiveDocInputGql : IGraphQlInputObject
    {
        private InputPropertyInfo _liveDocPushRow;

        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(QueryBuilderParameterConverter<ICollection<LiveDocInputPushRowGql?>?>))]
        #endif
        public QueryBuilderParameter<ICollection<LiveDocInputPushRowGql?>?>? LiveDocPushRow
        {
            get => (QueryBuilderParameter<ICollection<LiveDocInputPushRowGql?>?>?)_liveDocPushRow.Value;
            set => _liveDocPushRow = new InputPropertyInfo { Name = "liveDocPushRow", Value = value };
        }

        IEnumerable<InputPropertyInfo> IGraphQlInputObject.GetPropertyValues()
        {
            if (_liveDocPushRow.Name != null) yield return _liveDocPushRow;
        }
    }
    #endregion

    #region data classes
    public partial class UserPullBulkGql
    {
        public ICollection<UserGql>? Documents { get; set; }
        public CheckpointGql? Checkpoint { get; set; }
    }

    public partial class WorkspacePullBulkGql
    {
        public ICollection<WorkspaceGql>? Documents { get; set; }
        public CheckpointGql? Checkpoint { get; set; }
    }

    public partial class LiveDocPullBulkGql
    {
        public ICollection<LiveDocGql>? Documents { get; set; }
        public CheckpointGql? Checkpoint { get; set; }
    }

    public partial class QueryGql
    {
        public UserPullBulkGql? PullUser { get; set; }
        public WorkspacePullBulkGql? PullWorkspace { get; set; }
        public LiveDocPullBulkGql? PullLiveDoc { get; set; }
    }

    public partial class MutationGql
    {
        public PushUserPayloadGql? PushUser { get; set; }
        public PushWorkspacePayloadGql? PushWorkspace { get; set; }
        public PushLiveDocPayloadGql? PushLiveDoc { get; set; }
    }

    public partial class SubscriptionGql
    {
        public UserPullBulkGql? StreamUser { get; set; }
        public WorkspacePullBulkGql? StreamWorkspace { get; set; }
        public LiveDocPullBulkGql? StreamLiveDoc { get; set; }
    }

    public partial class UserGql
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public LiveDocs.GraphQLApi.Security.UserRole Role { get; set; }
        public string? JwtAccessToken { get; set; }
        public Guid WorkspaceId { get; set; }
        public Guid Id { get; set; }
        public bool IsDeleted { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public ICollection<string>? Topics { get; set; }
    }

    public partial class CheckpointGql
    {
        public Guid? LastDocumentId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }

    [GraphQlObjectType("AuthenticationError")]
    public partial class AuthenticationErrorGql : IPushUserErrorGql, IPushWorkspaceErrorGql, IPushLiveDocErrorGql, IErrorGql
    {
        public string Message { get; set; }
    }

    [GraphQlObjectType("UnauthorizedAccessError")]
    public partial class UnauthorizedAccessErrorGql : IPushUserErrorGql, IPushWorkspaceErrorGql, IPushLiveDocErrorGql, IErrorGql
    {
        public string Message { get; set; }
    }

    public partial class WorkspaceGql
    {
        public string Name { get; set; }
        public Guid Id { get; set; }
        public bool IsDeleted { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public ICollection<string>? Topics { get; set; }
    }

    public partial class LiveDocGql
    {
        public string Content { get; set; }
        public Guid OwnerId { get; set; }
        public Guid WorkspaceId { get; set; }
        public Guid Id { get; set; }
        public bool IsDeleted { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public ICollection<string>? Topics { get; set; }
    }

    public partial interface IErrorGql
    {
        string Message { get; set; }
    }

    public partial interface IPushUserErrorGql
    {
    }

    public partial class PushUserPayloadGql
    {
        public ICollection<UserGql>? User { get; set; }
        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(GraphQlInterfaceJsonConverter))]
        #endif
        public ICollection<IPushUserErrorGql>? Errors { get; set; }
    }

    public partial interface IPushWorkspaceErrorGql
    {
    }

    public partial class PushWorkspacePayloadGql
    {
        public ICollection<WorkspaceGql>? Workspace { get; set; }
        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(GraphQlInterfaceJsonConverter))]
        #endif
        public ICollection<IPushWorkspaceErrorGql>? Errors { get; set; }
    }

    public partial interface IPushLiveDocErrorGql
    {
    }

    public partial class PushLiveDocPayloadGql
    {
        public ICollection<LiveDocGql>? LiveDoc { get; set; }
        #if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        [JsonConverter(typeof(GraphQlInterfaceJsonConverter))]
        #endif
        public ICollection<IPushLiveDocErrorGql>? Errors { get; set; }
    }
    #endregion
    #nullable restore
}
