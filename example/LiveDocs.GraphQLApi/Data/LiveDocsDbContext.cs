using LiveDocs.GraphQLApi.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LiveDocs.GraphQLApi.Data
{
    public class LiveDocsDbContext(DbContextOptions options) : DbContext(options)
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<Workspace> Workspaces => Set<Workspace>();
        public DbSet<LiveDoc> LiveDocs => Set<LiveDoc>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ArgumentNullException.ThrowIfNull(modelBuilder);
            
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(e =>
            {
                e.HasIndex(u => u.Email)
                    .IsUnique();

                e.HasOne<Workspace>()
                    .WithMany()
                    .HasForeignKey(d => d.WorkspaceId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.Property(w => w.Topics).HasColumnType("nvarchar(max)");

                e.OwnsOne(
                    user => user.Topics,
                    ownedNavigationBuilder =>
                    {
                        ownedNavigationBuilder.ToJson();
                    });
            });

            modelBuilder.Entity<Workspace>(e =>
            {
                e.HasIndex(w => w.Name)
                    .IsUnique();

                e.Property(w => w.Topics).HasColumnType("nvarchar(max)");

                e.OwnsOne(
                    workspace => workspace.Topics,
                    ownedNavigationBuilder =>
                    {
                        ownedNavigationBuilder.ToJson();
                    });
            });

            modelBuilder.Entity<LiveDoc>(e =>
            {
                e.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(d => d.OwnerId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne<Workspace>()
                    .WithMany()
                    .HasForeignKey(d => d.WorkspaceId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.Property(w => w.Topics).HasColumnType("nvarchar(max)");

                e.OwnsOne(
                    workspace => workspace.Topics,
                    ownedNavigationBuilder =>
                    {
                        ownedNavigationBuilder.ToJson();
                    });
            });
        }
    }
}
