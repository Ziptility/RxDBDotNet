// tests\RxDBDotNet.Tests\FieldErrors\CustomTestException.cs
namespace RxDBDotNet.Tests.Model;

public class CustomTestException : Exception
{
    public CustomTestException() { }
    public CustomTestException(string message) : base(message) { }
    public CustomTestException(string message, Exception innerException) : base(message, innerException) { }

    public CustomTestException(string message, string? customField) : base(message)
    {
        CustomField = customField;
    }

    public string? CustomField { get; }
}
