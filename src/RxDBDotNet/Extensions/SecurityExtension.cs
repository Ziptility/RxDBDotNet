using Microsoft.AspNetCore.Authorization;
using RxDBDotNet.Documents;
using RxDBDotNet.Extensions;
using RxDBDotNet.Security;

public class SecurityExtension<TDocument> : ObjectTypeExtension
    where TDocument : class, IReplicatedDocument
{
    private readonly SecurityOptions _securityOptions;

    public SecurityExtension(SecurityOptions securityOptions)
    {
        _securityOptions = securityOptions;
    }

    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        var typeName = DocumentExtensions.GetGraphQLTypeName<TDocument>();

        descriptor
            .Field($"pull{typeName}")
            .Authorize(new AuthorizationPolicyBuilder()
                .AddRequirements(new AccessRequirement(OperationType.Read, _securityOptions))
                .Build());

        descriptor
            .Field($"push{typeName}")
            .Authorize(new AuthorizationPolicyBuilder()
                .AddRequirements(new AccessRequirement(OperationType.Write, _securityOptions))
                .Build());

        descriptor
            .Field($"stream{typeName}")
            .Authorize(new AuthorizationPolicyBuilder()
                .AddRequirements(new AccessRequirement(OperationType.Read, _securityOptions))
                .Build());
    }
}
