using AdminTemplate.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace AdminTemplate.Domain.Entities.CodeGen;

public class EntityDefinition : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(100)]
    public string? TableName { get; set; }

    public bool IsGenerated { get; set; } = false;

    public DateTime? GeneratedAt { get; set; }

    public ICollection<EntityColumn> Columns { get; set; } = new List<EntityColumn>();
    public ICollection<EntityRelation> Relations { get; set; } = new List<EntityRelation>();
}
