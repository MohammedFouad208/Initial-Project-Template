namespace AdminTemplate.Application.DTOs;

public record RoleDto(
    string Id,
    string Name,
    string? Description,
    int UserCount,
    int PermissionCount,
    DateTime CreatedAt);
