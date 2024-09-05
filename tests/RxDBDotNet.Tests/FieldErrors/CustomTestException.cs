namespace RxDBDotNet.Tests.Model;

public class CustomTestException : Exception
{
    public string? CustomField { get; }

    public CustomTestException() { }
    public CustomTestException(string message) : base(message) { }
    public CustomTestException(string message, Exception innerException) : base(message, innerException) { }
    public CustomTestException(string message, string? customField) : base(message)
    {
        CustomField = customField;
    }
}
