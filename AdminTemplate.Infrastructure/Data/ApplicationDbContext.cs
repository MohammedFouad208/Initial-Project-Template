using AdminTemplate.Domain.Entities;
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
    }
}
