using System.ComponentModel.DataAnnotations;

namespace LiveDocs.GraphQLApi.Validations;

/// <summary>
/// An attribute that trims leading and trailing whitespace from a string property.
/// This attribute is designed to be used on properties that require whitespace trimming
/// before they are processed or validated.
///<para></para>
/// Usage:
/// Apply this attribute to string properties where trimming is necessary.
/// Example:
/// <code>
/// [Trim]
/// public string FirstName { get; set; }
/// </code>
/// </summary>
/// <remarks>
/// This attribute is not a validation attribute in the traditional sense, but it inherits from
/// <see cref="ValidationAttribute"/> to enable it to participate in validation workflows if necessary.
/// The attribute modifies the property's value by removing any leading or trailing whitespace.
/// It is intended to be used as part of the data entry process to ensure that string values are
/// consistently formatted.
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public sealed class TrimAttribute : ValidationAttribute
{
    /// <summary>
    /// Overrides the <see cref="ValidationAttribute.IsValid"/> method to trim whitespace from the property value.
    /// This method is called automatically during the validation process.
    /// </summary>
    /// <param name="value">The value of the property to be validated and trimmed.</param>
    /// <param name="validationContext">Provides contextual information about the validation operation.</param>
    /// <returns>
    /// A <see cref="ValidationResult"/> indicating whether the trimming was successful.
    /// Always returns <see cref="ValidationResult.Success"/> as trimming itself is not a validation concern.
    /// </returns>
    /// <remarks>
    /// The method uses reflection to set the trimmed value back to the property.
    /// </remarks>
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        ArgumentNullException.ThrowIfNull(validationContext);

        if (value is string str)
        {
            // Trim the string
            var trimmed = str.Trim();

            // Use reflection to set the trimmed value back to the property
            if (validationContext.MemberName != null)
            {
                var property = validationContext.ObjectType.GetProperty(validationContext.MemberName);
                if (property?.CanWrite == true)
                {
                    property.SetValue(validationContext.ObjectInstance, trimmed);
                }
            }
        }

        // Return success because trimming is always successful
        return ValidationResult.Success;
    }
}
