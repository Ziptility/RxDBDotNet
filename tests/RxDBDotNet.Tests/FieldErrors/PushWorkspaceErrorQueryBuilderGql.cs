namespace RxDBDotNet.Tests.Model;

public partial class PushWorkspaceErrorQueryBuilderGql
{
    public PushWorkspaceErrorQueryBuilderGql WithCustomTestErrorFragment(
        CustomTestErrorQueryBuilderGql customTestErrorQueryBuilder,
        SkipDirective? skip = null,
        IncludeDirective? include = null)
    {
        return WithFragment(customTestErrorQueryBuilder, [skip, include]);
    }
}
