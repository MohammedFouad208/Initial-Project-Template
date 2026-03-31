using AdminTemplate.Application.DTOs;
using AdminTemplate.Application.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace AdminTemplate.Application.Services;

public class RoleService : IRoleService
{
    public Task<RoleDto?> GetByIdAsync(string id)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<RoleDto>> GetAllAsync()
    {
        throw new NotImplementedException();
    }

    public Task<(IReadOnlyList<RoleDto> Items, int TotalCount)> GetPagedAsync(
        int pageIndex,
        int pageSize,
        string? searchTerm = null)
    {
        throw new NotImplementedException();
    }

    public Task<IdentityResult> CreateAsync(CreateRoleDto dto)
    {
        throw new NotImplementedException();
    }

    public Task<IdentityResult> UpdateAsync(string id, UpdateRoleDto dto)
    {
        throw new NotImplementedException();
    }

    public Task<IdentityResult> DeleteAsync(string id)
    {
        throw new NotImplementedException();
    }
}
