using Microsoft.AspNetCore.Authorization;

namespace RxDBDotNet.Security;

public sealed class AccessRequirement : IAuthorizationRequirement
{
    public AccessType Type { get; }
    public SecurityOptions SecurityOptions { get; }

    public AccessRequirement(AccessType type, SecurityOptions securityOptions)
    {
        Type = type;
        SecurityOptions = securityOptions;
    }
}
