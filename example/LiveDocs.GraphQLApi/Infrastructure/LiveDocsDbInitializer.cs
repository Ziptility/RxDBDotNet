using LiveDocs.GraphQLApi.Data;
using LiveDocs.GraphQLApi.Models.Entities;
using LiveDocs.GraphQLApi.Models.Shared;
using Microsoft.EntityFrameworkCore;

namespace LiveDocs.GraphQLApi.Infrastructure
{
    public static class LiveDocsDbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LiveDocsDbContext>();

            await dbContext.Database.EnsureCreatedAsync(cancellationToken);

            await SeedDataAsync(dbContext, cancellationToken);
        }

        private static async Task SeedDataAsync(LiveDocsDbContext dbContext, CancellationToken cancellationToken)
        {
            if (await dbContext.Workspaces.AnyAsync(cancellationToken))
            {
                return; // Data has already been seeded
            }

            var workspacePk = RT.Comb.Provider.Sql.Create();
            var liveDocsWorkspace = new Workspace
            {
                Id = workspacePk,
                Name = "LiveDocs",
                UpdatedAt = DateTimeOffset.UtcNow,
                IsDeleted = false,
                ReplicatedDocumentId = workspacePk,
            };

            await dbContext.Workspaces.AddAsync(liveDocsWorkspace, cancellationToken);

            var userPk = RT.Comb.Provider.Sql.Create();
            var superAdminUser = new User
            {
                Id = userPk,
                FirstName = "Super",
                LastName = "Admin",
                Email = "superadmin@livedocs.org",
                Role = UserRole.SystemAdmin,
                WorkspaceId = liveDocsWorkspace.Id,
                UpdatedAt = DateTimeOffset.UtcNow,
                IsDeleted = false,
                ReplicatedDocumentId = userPk,
            };

            await dbContext.Users.AddAsync(superAdminUser, cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
