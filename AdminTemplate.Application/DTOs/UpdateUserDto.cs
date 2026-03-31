namespace AdminTemplate.Application.DTOs;

public record UpdateUserDto(
    string FullName,
    string Email,
    bool IsActive,
    IReadOnlyList<string> Roles);
