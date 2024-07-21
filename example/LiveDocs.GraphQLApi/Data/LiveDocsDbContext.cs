using Microsoft.EntityFrameworkCore;
using LiveDocs.GraphQLApi.Models;

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

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Workspace>()
                .HasIndex(w => w.Name)
                .IsUnique();

            modelBuilder.Entity<LiveDoc>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(d => d.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LiveDoc>()
                .HasOne<Workspace>()
                .WithMany()
                .HasForeignKey(d => d.WorkspaceId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
