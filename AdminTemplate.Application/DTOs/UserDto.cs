namespace AdminTemplate.Application.DTOs;

public record UserDto(
    string Id,
    string FullName,
    string Email,
    bool IsActive,
    IReadOnlyList<string> Roles,
    DateTime CreatedAt);
