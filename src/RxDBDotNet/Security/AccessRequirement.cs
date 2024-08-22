using Microsoft.AspNetCore.Authorization;

namespace RxDBDotNet.Security;

public sealed class AccessRequirement : IAuthorizationRequirement
{
    public OperationType Type { get; }
    public SecurityOptions SecurityOptions { get; }

    public AccessRequirement(OperationType type, SecurityOptions securityOptions)
    {
        Type = type;
        SecurityOptions = securityOptions;
    }
}
