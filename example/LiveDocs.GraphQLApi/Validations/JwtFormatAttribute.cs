using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;

namespace LiveDocs.GraphQLApi.Validations
{
    /// <summary>
    /// Validates that a string property contains a well-formed JWT (JSON Web Token).
    /// </summary>
    /// <remarks>
    /// This attribute checks if a string is a valid JWT format by using the <see cref="JwtSecurityTokenHandler"/> class
    /// from the <c>System.IdentityModel.Tokens.Jwt</c> namespace.
    /// <para>
    /// The validation does not check the token's signature or claims; it only ensures that the string is correctly structured as a JWT.
    /// </para>
    /// <para>
    /// <strong>Usage:</strong>
    /// Apply this attribute to string properties that are intended to hold JWT access tokens.
    /// </para>
    /// <example>
    /// <code>
    /// [JwtFormat(AllowNull = true)]
    /// public string? JwtAccessToken { get; set; }
    /// </code>
    /// </example>
    /// <para>
    /// <strong>Note:</strong>
    /// This attribute only validates the format of the JWT. It does not verify the token's integrity, expiration, or claims.
    /// For full token validation, consider using a more comprehensive solution involving token validation middleware or services.
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class JwtFormatAttribute : ValidationAttribute
    {
        /// <summary>
        /// Gets or sets a value indicating whether null values are allowed.
        /// </summary>
        public bool AllowNull { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtFormatAttribute"/> class.
        /// </summary>
        public JwtFormatAttribute()
        {
            AllowNull = false;
        }

        /// <summary>
        /// Validates whether the specified value is a well-formed JWT or null if allowed.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <param name="validationContext">Provides contextual information about the validation operation.</param>
        /// <returns>
        /// A <see cref="ValidationResult"/> indicating whether the validation was successful.
        /// <para>
        /// Returns <see cref="ValidationResult.Success"/> if the value is a valid JWT format or if null is allowed and the value is null;
        /// otherwise, returns an error message.
        /// </para>
        /// </returns>
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is null)
            {
                return AllowNull ? ValidationResult.Success : new ValidationResult("The JWT token cannot be null.");
            }

            if (value is string jwt)
            {
                if (string.IsNullOrWhiteSpace(jwt))
                {
                    return new ValidationResult("The JWT token is empty or consists only of whitespace.");
                }

                var handler = new JwtSecurityTokenHandler();
                return handler.CanReadToken(jwt) ? ValidationResult.Success : new ValidationResult("The JWT token format is invalid.");
            }

            return new ValidationResult($"The value must be a string. Actual type: {value.GetType().Name}");
        }
    }
}
