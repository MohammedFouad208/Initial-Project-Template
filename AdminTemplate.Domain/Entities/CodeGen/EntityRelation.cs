using AdminTemplate.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace AdminTemplate.Domain.Entities.CodeGen;

public class EntityRelation : BaseEntity
{
    public Guid EntityDefinitionId { get; set; }
    public EntityDefinition EntityDefinition { get; set; } = null!;

    // The target entity this relation points to
    [Required]
    [MaxLength(100)]
    public string RelatedEntityName { get; set; } = string.Empty;

    // The FK column name on this entity (e.g. "CategoryId")
    [Required]
    [MaxLength(100)]
    public string ForeignKeyName { get; set; } = string.Empty;

    // Navigation property name (e.g. "Category")
    [Required]
    [MaxLength(100)]
    public string NavigationPropertyName { get; set; } = string.Empty;

    // The display column from the related entity shown in dropdowns (e.g. "Name")
    [MaxLength(100)]
    public string DisplayColumn { get; set; } = "Name";

    public RelationType RelationType { get; set; } = RelationType.ManyToOne;
}

public enum RelationType
{
    ManyToOne = 0,
    OneToOne  = 1
}
