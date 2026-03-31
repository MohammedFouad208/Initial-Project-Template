using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace AdminTemplate.Domain.Entities;

public class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole()
    {
        Id = Guid.NewGuid();
    }

    public ApplicationRole(string roleName) : base(roleName)
    {
        Id = Guid.NewGuid();
    }

    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
