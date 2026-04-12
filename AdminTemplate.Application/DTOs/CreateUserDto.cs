namespace AdminTemplate.Application.DTOs;

public record CreateUserDto(
    string FullName,
    string Email,
    string Password,
    IReadOnlyList<string> Roles,
    bool IsActive = true);
