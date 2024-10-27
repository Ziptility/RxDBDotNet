using System;

namespace LiveDocs.GraphQLApi.Security;

/// <summary>
/// Represents the parameters required for generating a JWT token.
/// </summary>
public class TokenParameters
{
    /// <summary>
    /// The issuer of the token. If not provided, the default JwtUtil.Issuer is used.
    /// </summary>
    public string? Issuer { get; init; } = JwtUtil.Issuer;

    /// <summary>
    /// The audience of the token. If not provided, the default JwtUtil.Audience is used.
    /// </summary>
    public string? Audience { get; init; } = JwtUtil.Audience;

    /// <summary>
    /// The secret key used to sign the token. If not provided, the default JwtUtil.SecretKey is used.
    /// </summary>
    public string? SecretKey { get; init; } = JwtUtil.SecretKey;

    /// <summary>
    /// The expiration time of the token. If not provided, the token will expire in 120 minutes
    /// </summary>
    public DateTime? Expires { get; init; } = DateTime.UtcNow.AddMinutes(120);
}
