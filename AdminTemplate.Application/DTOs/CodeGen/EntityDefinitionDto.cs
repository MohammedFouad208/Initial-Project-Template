namespace AdminTemplate.Application.DTOs.CodeGen;

public record EntityDefinitionDto(
    Guid Id,
    string Name,
    string? Description,
    string? TableName,
    bool IsGenerated,
    DateTime? GeneratedAt,
    IReadOnlyList<EntityColumnDto> Columns,
    IReadOnlyList<EntityRelationDto> Relations);

public record EntityColumnDto(
    Guid Id,
    string Name,
    string DataType,
    bool IsRequired,
    bool IsUnique,
    int? MaxLength,
    int SortOrder,
    string? DefaultValue,
    bool ShowInList,
    bool ShowInForm,
    bool UseInSearch);

public record EntityRelationDto(
    Guid Id,
    string RelatedEntityName,
    string ForeignKeyName,
    string NavigationPropertyName,
    string DisplayColumn,
    int RelationType);
