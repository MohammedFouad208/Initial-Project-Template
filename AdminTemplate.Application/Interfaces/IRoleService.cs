using AdminTemplate.Application.DTOs;
using Microsoft.AspNetCore.Identity;

namespace AdminTemplate.Application.Interfaces;

public interface IRoleService
{
    Task<RoleDto?> GetByIdAsync(string id);
    Task<IReadOnlyList<RoleDto>> GetAllAsync();
    Task<(IReadOnlyList<RoleDto> Items, int TotalCount)> GetPagedAsync(
        int pageIndex,
        int pageSize,
        string? searchTerm = null);
    Task<IdentityResult> CreateAsync(CreateRoleDto dto);
    Task<IdentityResult> UpdateAsync(string id, UpdateRoleDto dto);
    Task<IdentityResult> DeleteAsync(string id);
}
