using AdminTemplate.Application.DTOs;
using AdminTemplate.Application.Interfaces;
using AdminTemplate.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace AdminTemplate.Application.Services;

public class RoleService : IRoleService
{
    private readonly RoleManager<ApplicationRole> _roleManager;

    public RoleService(RoleManager<ApplicationRole> roleManager)
    {
        _roleManager = roleManager;
    }

    public Task<RoleDto?> GetByIdAsync(string id)
    {
        throw new NotImplementedException();
    }

    public Task<int> GetTotalCountAsync()
    {
        return Task.FromResult(_roleManager.Roles.Count());
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
