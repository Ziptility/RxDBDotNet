// example/LiveDocs.GraphQLApi/Security/JwtUtil.cs

using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LiveDocs.GraphQLApi.Models.Replication;
using Microsoft.IdentityModel.Tokens;

namespace LiveDocs.GraphQLApi.Security;

/// <summary>
/// Provides utility methods for generating and validating JWT access tokens.
/// </summary>
/// <remarks>
/// This class is designed to be used in a non-production example application. It allows the simulation of user authentication
/// by generating JWT tokens that contain standard and custom claims. The generated tokens are intended for use in test
/// scenarios and demonstrations. In a production environment, a robust authentication and authorization system should be used.
/// </remarks>
public static class JwtUtil
{
    /// <summary>
    /// The secret key used for signing JWT tokens. This should be stored securely and not hard-coded in a production environment.
    /// </summary>
    public const string SecretKey = "fa891b0b-e8cb-44fd-bce5-1cd4acc2824d";

    /// <summary>
    /// The issuer of the JWT token, representing the application that generated the token.
    /// </summary>
    public const string Issuer = "LiveDocsExampleApp";

    /// <summary>
    /// The audience for the JWT token, representing the intended recipients or clients of the token.
    /// </summary>
    public const string Audience = "UnitTestAndExampleClients";

    /// <summary>
    /// Generates a JWT token for a given user, capturing the user's id, role, email, and a custom workspace claim.
    /// </summary>
    /// <param name="user">The <see cref="ReplicatedUser"/> for whom the token is being generated. This parameter cannot be null.</param>
    /// <param name="expires">Optional. The expiration time of the token. If not provided, the token will expire in 120 minutes.</param>
    /// <returns>A JWT token as a <see cref="string"/> that contains the user's claims.</returns>
    /// <remarks>
    /// This token is valid for 120 minutes and is signed using the HMAC SHA256 algorithm.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="user"/> parameter is null.</exception>
    public static string GenerateJwtToken(ReplicatedUser user, DateTime? expires = null)
    {
        ArgumentNullException.ThrowIfNull(user);

        var now = DateTimeOffset.UtcNow;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString("D", CultureInfo.InvariantCulture)), // User's unique ID as the subject claim
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("D", CultureInfo.InvariantCulture)), // Unique identifier for the token
            new(ClaimTypes.Role, user.Role.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64),
            new(JwtRegisteredClaimNames.Nbf, now.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64),
            new(CustomClaimTypes.WorkspaceId, user.WorkspaceId.ToString("D", CultureInfo.InvariantCulture)),
        };

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: expires ?? DateTime.UtcNow.AddMinutes(120),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Retrieves the token validation parameters used for validating JWT tokens.
    /// </summary>
    /// <returns>A <see cref="TokenValidationParameters"/> object configured with strict validation settings.</returns>
    /// <remarks>
    /// The validation parameters enforce comprehensive checks on the token, including:
    /// <list type="bullet">
    /// <item><description>Issuer validation: Ensures the token was issued by the expected issuer.</description></item>
    /// <item><description>Audience validation: Ensures the token is intended for the expected audience.</description></item>
    /// <item><description>Lifetime validation: Ensures the token has not expired and is being used within its valid time window.</description></item>
    /// <item><description>Signing key validation: Ensures the token was signed using the expected security key.</description></item>
    /// <item><description>Signature validation: Ensures the integrity of the token's signature.</description></item>
    /// <item><description>NotBefore validation: Ensures the token is not used before its 'nbf' (Not Before) time, which is part of lifetime validation.</description></item>
    /// </list>
    /// This method provides a strict set of validation rules suitable for testing environments. In a production environment,
    /// these settings should be carefully configured according to security requirements.
    /// </remarks>
    /// <exception cref="SecurityTokenException">Thrown if the validation parameters are misconfigured or if the token fails validation.</exception>
    public static TokenValidationParameters GetTokenValidationParameters()
    {
        return new TokenValidationParameters
        {
            ValidateIssuer = true, // Ensure the token is issued by the correct authority
            ValidIssuer = Issuer,  // Specify the valid issuer

            ValidateAudience = true, // Ensure the token is intended for the correct audience
            ValidAudience = Audience, // Specify the valid audience

            ValidateLifetime = true, // Ensure the token is not expired and is within its valid time range
            RequireExpirationTime = true, // Explicitly require that the token contains an expiration time
            ClockSkew = TimeSpan.Zero, // Set clock skew to zero to ensure strict expiration checks

            ValidateIssuerSigningKey = true, // Ensure the token was signed with the correct signing key
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey)), // Specify the signing key

            RequireSignedTokens = true, // Ensure the token is signed

            ValidateTokenReplay = true, // Prevent replay attacks by validating that the token is unique
            RequireAudience = true, // Explicitly require an audience claim
        };
    }
}
