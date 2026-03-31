using AdminTemplate.Domain.Entities;
using AdminTemplate.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AdminTemplate.Infrastructure.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public RoleRepository(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager)
    {
        _roleManager = roleManager;
        _userManager = userManager;
    }

    public Task<ApplicationRole?> GetByIdAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<ApplicationRole>> GetAllAsync()
    {
        throw new NotImplementedException();
    }

    public Task<(IReadOnlyList<ApplicationRole> Items, int TotalCount)> GetPagedAsync(
        int pageIndex,
        int pageSize,
        string? searchTerm = null)
    {
        throw new NotImplementedException();
    }

    public async Task<ApplicationRole?> GetByNameAsync(string name)
    {
        return await _roleManager.FindByNameAsync(name);
    }

    public async Task AddAsync(ApplicationRole role)
    {
        var result = await _roleManager.CreateAsync(role);

        if (!result.Succeeded)
        {
            var error = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create role: {error}");
        }
    }

    public Task UpdateAsync(ApplicationRole role)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> HasUsersAsync(Guid roleId)
    {
        var role = await _roleManager.Roles.FirstOrDefaultAsync(r => r.Id == roleId);
        if (role is null || string.IsNullOrWhiteSpace(role.Name))
        {
            return false;
        }

        var users = await _userManager.GetUsersInRoleAsync(role.Name);
        return users.Count > 0;
    }
}
