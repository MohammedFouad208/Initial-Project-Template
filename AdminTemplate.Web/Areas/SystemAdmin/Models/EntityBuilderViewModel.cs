using System.ComponentModel.DataAnnotations;

namespace AdminTemplate.Web.Areas.SystemAdmin.Models;

public class CreateEntityViewModel
{
    [Required]
    [MaxLength(100)]
    [RegularExpression(@"^[A-Z][A-Za-z0-9]*$",
        ErrorMessage = "Name must start with an uppercase letter and contain only letters and digits.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(100)]
    public string? TableName { get; set; }

    public List<ColumnViewModel> Columns { get; set; } = [];
    public List<RelationViewModel> Relations { get; set; } = [];
}

public class ColumnViewModel
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string DataType { get; set; } = "string";

    public bool IsRequired { get; set; }
    public bool IsUnique { get; set; }
    public int? MaxLength { get; set; }
    public int SortOrder { get; set; }
    public string? DefaultValue { get; set; }
    public bool ShowInList { get; set; } = true;
    public bool ShowInForm { get; set; } = true;
    public bool UseInSearch { get; set; } = false;
}

public class RelationViewModel
{
    [Required]
    [MaxLength(100)]
    public string RelatedEntityName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ForeignKeyName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string NavigationPropertyName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string DisplayColumn { get; set; } = "Name";

    public int RelationType { get; set; } = 0;
}
