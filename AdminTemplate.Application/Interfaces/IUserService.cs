using AdminTemplate.Application.DTOs;
using Microsoft.AspNetCore.Identity;

namespace AdminTemplate.Application.Interfaces;

public interface IUserService
{
    Task<UserDto?> GetByIdAsync(string id);
    Task<int> GetTotalCountAsync();
    Task<int> GetActiveCountAsync();
    Task<(IReadOnlyList<UserDto> Items, int TotalCount)> GetPagedAsync(
        int pageIndex,
        int pageSize,
        string? searchTerm = null);
    Task<IdentityResult> CreateAsync(CreateUserDto dto);
    Task<IdentityResult> UpdateAsync(string id, UpdateUserDto dto);
    Task<IdentityResult> SetActiveAsync(string id, bool isActive);
    Task<IdentityResult> DeleteAsync(string id);
}
