namespace AdminTemplate.Application.DTOs.CodeGen;

public record CreateEntityDto(
    string Name,
    string? Description,
    string? TableName,
    IList<CreateEntityColumnDto> Columns,
    IList<CreateEntityRelationDto> Relations);

public record CreateEntityColumnDto(
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

public record CreateEntityRelationDto(
    string RelatedEntityName,
    string ForeignKeyName,
    string NavigationPropertyName,
    string DisplayColumn,
    int RelationType);
