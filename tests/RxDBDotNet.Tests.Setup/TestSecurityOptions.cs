using RxDBDotNet.Security;
using LiveDocs.GraphQLApi.Models.Shared;

namespace RxDBDotNet.Tests.Setup;

public class TestSecurityOptions
{
    public Action<SecurityOptions>? UserSecurityConfig { get; set; }
    public Action<SecurityOptions>? WorkspaceSecurityConfig { get; set; }
    public Action<SecurityOptions>? LiveDocSecurityConfig { get; set; }

    public static TestSecurityOptions Default => new()
    {
        UserSecurityConfig = options => options
            .RequireMinimumRoleToRead(UserRole.StandardUser)
            .RequireMinimumRoleToWrite(UserRole.WorkspaceAdmin)
            .RequireMinimumRoleToDelete(UserRole.WorkspaceAdmin),

        WorkspaceSecurityConfig = options => options
            .RequireMinimumRoleToRead(UserRole.StandardUser)
            .RequireMinimumRoleToWrite(UserRole.SystemAdmin),

        LiveDocSecurityConfig = options => options
            .RequireMinimumRole(UserRole.StandardUser),
    };
}
