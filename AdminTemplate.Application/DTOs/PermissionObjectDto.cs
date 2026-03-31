namespace AdminTemplate.Application.DTOs;

public record PermissionObjectDto(
    string Name,
    string DisplayName,
    IReadOnlyList<string> Functions);
