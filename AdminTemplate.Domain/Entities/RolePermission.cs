using AdminTemplate.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace AdminTemplate.Domain.Entities;

public class RolePermission : BaseEntity
{
    [Required]
    public Guid RoleId { get; set; }

    [Required]
    [MaxLength(100)]
    public string ObjectName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FunctionName { get; set; } = string.Empty;
}
