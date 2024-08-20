using LiveDocs.GraphQLApi.Models.Entities;
using LiveDocs.GraphQLApi.Models.Shared;
using Microsoft.EntityFrameworkCore;

namespace LiveDocs.GraphQLApi.Data;

public class LiveDocsDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<LiveDoc> LiveDocs => Set<LiveDoc>();
    public DbSet<Hero> Heroes => Set<Hero>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);

        ConfigureUserEntity(modelBuilder);
        ConfigureWorkspaceEntity(modelBuilder);
        ConfigureLiveDocEntity(modelBuilder);
        ConfigureHeroEntity(modelBuilder);
    }

    private static void ConfigureHeroEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Hero>(entity =>
        {
            entity.ToTable("Heroes");

            entity.Property(e => e.Id)
                .ValueGeneratedNever();

            entity.Property(e => e.UpdatedAt)
                .IsRequired()
                .HasColumnType("datetimeoffset(7)");
        });
    }

    private static void ConfigureUserEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(u => u.Id)
                .ValueGeneratedNever();

            entity.HasAlternateKey(u => u.ReplicatedDocumentId);

            entity.HasIndex(u => u.Email)
                .IsUnique();

            entity.Property(e => e.UpdatedAt)
                .IsRequired()
                .HasColumnType("datetimeoffset(7)"); // Highest precision

            entity.HasOne(e => e.Workspace)
                .WithMany()
                .HasForeignKey(d => d.WorkspaceId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            entity.OwnsMany(user => user.Topics, b => b.ToJson());
            entity.Navigation(e => e.Topics)
                .AutoInclude();
        });
    }

    private static void ConfigureWorkspaceEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Workspace>(entity =>
        {
            entity.Property(w => w.Id)
                .ValueGeneratedNever();

            entity.HasAlternateKey(u => u.ReplicatedDocumentId);

            entity.HasIndex(w => w.Name)
                .IsUnique();

            entity.Property(e => e.UpdatedAt)
                .IsRequired()
                .HasColumnType("datetimeoffset(7)"); // Highest precision

            entity.OwnsMany(workspace => workspace.Topics, builder => builder.ToJson());
            entity.Navigation(e => e.Topics)
                .AutoInclude();
        });
    }

    private static void ConfigureLiveDocEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LiveDoc>(entity =>
        {
            entity.Property(d => d.Id)
                .ValueGeneratedNever();

            entity.HasAlternateKey(u => u.ReplicatedDocumentId);

            entity.Property(e => e.UpdatedAt)
                .IsRequired()
                .HasColumnType("datetimeoffset(7)"); // Highest precision

            entity.HasOne(e => e.Owner)
                .WithMany()
                .HasForeignKey(d => d.OwnerId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Workspace)
                .WithMany()
                .HasForeignKey(d => d.WorkspaceId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            entity.OwnsMany(workspace => workspace.Topics, builder => builder.ToJson());
            entity.Navigation(e => e.Topics)
                .AutoInclude();
        });
    }
}
