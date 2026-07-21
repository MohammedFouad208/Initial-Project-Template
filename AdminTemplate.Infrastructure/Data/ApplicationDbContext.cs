using AdminTemplate.Domain.Entities;
using AdminTemplate.Domain.Entities.CodeGen;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AdminTemplate.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<EntityDefinition> EntityDefinitions => Set<EntityDefinition>();
    public DbSet<EntityColumn> EntityColumns => Set<EntityColumn>();
    public DbSet<EntityRelation> EntityRelations => Set<EntityRelation>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.FullName)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(u => u.CreatedAt)
                .IsRequired();
        });

        builder.Entity<ApplicationRole>(entity =>
        {
            entity.Property(r => r.Description)
                .HasMaxLength(500);

            entity.Property(r => r.CreatedAt)
                .IsRequired();
        });

        builder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(rp => rp.Id);

            entity.Property(rp => rp.ObjectName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(rp => rp.FunctionName)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasOne<ApplicationRole>()
                .WithMany()
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(rp => new { rp.RoleId, rp.ObjectName, rp.FunctionName })
                .IsUnique();
        });

        builder.Entity<EntityDefinition>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.TableName).HasMaxLength(100);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        builder.Entity<EntityColumn>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).HasMaxLength(100).IsRequired();
            entity.Property(c => c.DataType).HasMaxLength(50).IsRequired();
            entity.Property(c => c.DefaultValue).HasMaxLength(200);
            entity.HasOne(c => c.EntityDefinition)
                .WithMany(e => e.Columns)
                .HasForeignKey(c => c.EntityDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<EntityRelation>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.RelatedEntityName).HasMaxLength(100).IsRequired();
            entity.Property(r => r.ForeignKeyName).HasMaxLength(100).IsRequired();
            entity.Property(r => r.NavigationPropertyName).HasMaxLength(100).IsRequired();
            entity.Property(r => r.DisplayColumn).HasMaxLength(100);
            entity.HasOne(r => r.EntityDefinition)
                .WithMany(e => e.Relations)
                .HasForeignKey(r => r.EntityDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
