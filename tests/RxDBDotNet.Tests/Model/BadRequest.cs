using System.Text;

namespace RxDBDotNet.Tests.Model;

/// <summary>
/// Represents the details of a validation problem, typically used for BadRequest (400) responses in ASP.NET Core.
/// </summary>
public class BadRequest
{
    /// <summary>
    /// Gets or sets a URI that identifies the problem type.
    /// </summary>
    /// <example>https://tools.ietf.org/html/rfc7231#section-6.5.1</example>
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets a short, human-readable summary of the problem type.
    /// </summary>
    /// <example>One or more validation errors occurred.</example>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the HTTP status code for the response.
    /// </summary>
    /// <example>400</example>
    public int Status { get; set; }

    /// <summary>
    /// Gets or sets a more detailed explanation specific to this occurrence of the problem.
    /// </summary>
    public string? Detail { get; set; }

    /// <summary>
    /// Gets or sets a URI that identifies the specific occurrence of the problem.
    /// </summary>
    public string? Instance { get; set; }

    /// <summary>
    /// Gets or sets a dictionary of validation errors.
    /// The keys represent the properties with validation errors.
    /// The values are string arrays containing error messages for the corresponding property.
    /// </summary>
    public IDictionary<string, string[]>? Errors { get; init; }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine("BadRequest:")
            .AppendLine($"  Type: {Type ?? "null"}")
            .AppendLine($"  Title: {Title ?? "null"}")
            .AppendLine($"  Status: {Status}")
            .AppendLine($"  Detail: {Detail ?? "null"}")
            .AppendLine($"  Instance: {Instance ?? "null"}");

        if (Errors?.Any() == true)
        {
            sb.AppendLine("  Errors:");
            foreach (var error in Errors)
            {
                sb.AppendLine($"    {error.Key}:");
                foreach (var message in error.Value)
                {
                    sb.AppendLine($"      - {message}");
                }
            }
        }
        else
        {
            sb.AppendLine("  Errors: None");
        }

        return sb.ToString();
    }
}
