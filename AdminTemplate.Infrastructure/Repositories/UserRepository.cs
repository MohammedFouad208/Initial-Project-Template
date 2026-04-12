using AdminTemplate.Domain.Entities;
using AdminTemplate.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AdminTemplate.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserRepository(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<ApplicationUser?> GetByIdAsync(Guid id)
    {
        return await _userManager.FindByIdAsync(id.ToString());
    }

    public async Task<IReadOnlyList<ApplicationUser>> GetAllAsync()
    {
        return await _userManager.Users
            .OrderBy(u => u.FullName)
            .ToListAsync();
    }

    public async Task<(IReadOnlyList<ApplicationUser> Items, int TotalCount)> GetPagedAsync(
        int pageIndex,
        int pageSize,
        string? searchTerm = null)
    {
        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(u =>
                u.FullName.Contains(searchTerm) ||
                (u.Email != null && u.Email.Contains(searchTerm)));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(u => u.FullName)
            .Skip(pageIndex)
            .Take(pageSize)
            .ToListAsync();

        return (items.AsReadOnly(), total);
    }

    public async Task<ApplicationUser?> GetByEmailAsync(string email)
    {
        return await _userManager.FindByEmailAsync(email);
    }

    public async Task AddAsync(ApplicationUser user)
    {
        IdentityResult result;

        if (string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            result = await _userManager.CreateAsync(user);
        }
        else
        {
            result = await _userManager.CreateAsync(user, user.PasswordHash);
        }

        if (!result.Succeeded)
        {
            var error = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create user: {error}");
        }
    }

    public async Task UpdateAsync(ApplicationUser user)
    {
        await _userManager.UpdateAsync(user);
    }

    public async Task DeleteAsync(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is not null)
        {
            await _userManager.DeleteAsync(user);
        }
    }

    public async Task<IReadOnlyList<string>> GetRoleNamesAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return [];

        var roles = await _userManager.GetRolesAsync(user);
        return roles.ToList().AsReadOnly();
    }
}
