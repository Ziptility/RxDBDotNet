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

            var liveDocsWorkspace = new Workspace
            {
                Id = RT.Comb.Provider.Sql.Create(),
                Name = "LiveDocs",
                UpdatedAt = DateTimeOffset.UtcNow,
                IsDeleted = false,
            };

            await dbContext.Workspaces.AddAsync(liveDocsWorkspace, cancellationToken);

            var superAdminUser = new User
            {
                Id = RT.Comb.Provider.Sql.Create(),
                FirstName = "Super",
                LastName = "Admin",
                Email = "superadmin@livedocs.org",
                Role = UserRole.SuperAdmin,
                WorkspaceId = liveDocsWorkspace.Id,
                UpdatedAt = DateTimeOffset.UtcNow,
                IsDeleted = false,
            };

            await dbContext.Users.AddAsync(superAdminUser, cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
