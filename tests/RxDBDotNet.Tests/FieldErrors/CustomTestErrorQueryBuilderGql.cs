namespace RxDBDotNet.Tests.Model;

public class CustomTestErrorQueryBuilderGql : GraphQlQueryBuilder<CustomTestErrorQueryBuilderGql>
{
    private static readonly GraphQlFieldMetadata[] AllFieldMetadata =
    {
        new GraphQlFieldMetadata { Name = "message" },
        new GraphQlFieldMetadata { Name = "customField" },
    };

    protected override string TypeName { get; } = "CustomTestError";

    public override IReadOnlyList<GraphQlFieldMetadata> AllFields { get; } = AllFieldMetadata;

    public CustomTestErrorQueryBuilderGql WithMessage(string? alias = null, SkipDirective? skip = null, IncludeDirective? include = null)
        => WithScalarField("message", alias, [skip, include]);

    public CustomTestErrorQueryBuilderGql WithCustomField(string? alias = null, SkipDirective? skip = null, IncludeDirective? include = null)
        => WithScalarField("customField", alias, [skip, include]);

    public CustomTestErrorQueryBuilderGql ExceptMessage() => ExceptField("message");

    public CustomTestErrorQueryBuilderGql ExceptCustomField() => ExceptField("customField");
}
