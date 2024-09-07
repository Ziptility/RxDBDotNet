namespace RxDBDotNet.Tests.Model;

[GraphQlObjectType("CustomTestError")]
public class CustomTestErrorGql : IPushWorkspaceErrorGql, IErrorGql
{
    public string? CustomField { get; set; }
    public string Message { get; set; } = null!;
}
