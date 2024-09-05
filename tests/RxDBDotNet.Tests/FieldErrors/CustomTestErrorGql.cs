namespace RxDBDotNet.Tests.Model;

[GraphQlObjectType("CustomTestError")]
public class CustomTestErrorGql : IPushWorkspaceErrorGql, IErrorGql
{
    public string Message { get; set; } = null!;
    public string? CustomField { get; set; }
}
