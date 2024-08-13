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

            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(u => u.Id)
                    .ValueGeneratedNever();
                
                entity.HasAlternateKey(u => u.ReplicatedDocumentId);
                
                entity.HasIndex(u => u.Email)
                    .IsUnique();

                entity.HasOne(e => e.Workspace)
                    .WithMany()
                    .HasForeignKey(d => d.WorkspaceId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.OwnsOne(
                    user => user.Topics,
                    ownedNavigationBuilder =>
                    {
                        ownedNavigationBuilder.ToJson();
                    });
            });

            modelBuilder.Entity<Workspace>(entity =>
            {
                entity.Property(w => w.Id)
                    .ValueGeneratedNever();

                entity.HasAlternateKey(u => u.ReplicatedDocumentId);

                entity.HasIndex(w => w.Name)
                    .IsUnique();

                entity.OwnsOne(
                    workspace => workspace.Topics,
                    ownedNavigationBuilder =>
                    {
                        ownedNavigationBuilder.ToJson();
                    });
            });

            modelBuilder.Entity<LiveDoc>(entity =>
            {
                entity.Property(d => d.Id)
                    .ValueGeneratedNever();

                entity.HasAlternateKey(u => u.ReplicatedDocumentId);

                entity.HasOne(e => e.Owner)
                    .WithMany()
                    .HasForeignKey(d => d.OwnerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Workspace)
                    .WithMany()
                    .HasForeignKey(d => d.WorkspaceId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.OwnsOne(
                    workspace => workspace.Topics,
                    ownedNavigationBuilder =>
                    {
                        ownedNavigationBuilder.ToJson();
                    });
            });
        }
    }
}
