using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace AdminTemplate.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public ApplicationUser()
    {
        Id = Guid.NewGuid();
    }

    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
