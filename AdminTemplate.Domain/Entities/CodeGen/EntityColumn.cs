using AdminTemplate.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace AdminTemplate.Domain.Entities.CodeGen;

public class EntityColumn : BaseEntity
{
    public Guid EntityDefinitionId { get; set; }
    public EntityDefinition EntityDefinition { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string DataType { get; set; } = string.Empty;  // string, int, decimal, bool, DateTime, Guid

    public bool IsRequired { get; set; } = false;

    public bool IsUnique { get; set; } = false;

    public int? MaxLength { get; set; }

    public int SortOrder { get; set; } = 0;

    [MaxLength(200)]
    public string? DefaultValue { get; set; }

    public bool ShowInList { get; set; } = true;

    public bool ShowInForm { get; set; } = true;

    public bool UseInSearch { get; set; } = false;
}
