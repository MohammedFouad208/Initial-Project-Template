using AdminTemplate.Application.DTOs;
using AdminTemplate.Application.Interfaces;
using AdminTemplate.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace AdminTemplate.Application.Services;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<UserDto?> GetByIdAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);
        return new UserDto(
            user.Id.ToString(),
            user.FullName,
            user.Email ?? string.Empty,
            user.IsActive,
            roles.ToList().AsReadOnly(),
            user.CreatedAt);
    }

    public Task<int> GetTotalCountAsync()
    {
        return Task.FromResult(_userManager.Users.Count());
    }

    public Task<int> GetActiveCountAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var count = _userManager.Users.Count(user =>
            user.IsActive &&
            (!user.LockoutEnd.HasValue || user.LockoutEnd <= now));

        return Task.FromResult(count);
    }

    public async Task<(IReadOnlyList<UserDto> Items, int TotalCount)> GetPagedAsync(
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

        var total = query.Count();
        var users = query
            .OrderBy(u => u.FullName)
            .Skip(pageIndex)
            .Take(pageSize)
            .ToList();

        var items = new List<UserDto>(users.Count);
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            items.Add(new UserDto(
                user.Id.ToString(),
                user.FullName,
                user.Email ?? string.Empty,
                user.IsActive,
                roles.ToList().AsReadOnly(),
                user.CreatedAt));
        }

        return (items.AsReadOnly(), total);
    }

    public async Task<IdentityResult> CreateAsync(CreateUserDto dto)
    {
        var user = new ApplicationUser
        {
            FullName  = dto.FullName,
            Email     = dto.Email,
            UserName  = dto.Email,
            IsActive  = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            return result;
        }

        if (dto.Roles is { Count: > 0 })
        {
            var rolesResult = await _userManager.AddToRolesAsync(user, dto.Roles);
            if (!rolesResult.Succeeded)
            {
                return rolesResult;
            }
        }

        return IdentityResult.Success;
    }

    public async Task<IdentityResult> UpdateAsync(string id, UpdateUserDto dto)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return IdentityResult.Failed(new IdentityError { Description = "User not found." });
        }

        user.FullName        = dto.FullName;
        user.Email           = dto.Email;
        user.UserName        = dto.Email;
        user.NormalizedEmail = dto.Email.ToUpperInvariant();
        user.NormalizedUserName = dto.Email.ToUpperInvariant();
        user.IsActive        = dto.IsActive;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return updateResult;
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
        if (!removeResult.Succeeded)
        {
            return removeResult;
        }

        if (dto.Roles is { Count: > 0 })
        {
            var addResult = await _userManager.AddToRolesAsync(user, dto.Roles);
            if (!addResult.Succeeded)
            {
                return addResult;
            }
        }

        return IdentityResult.Success;
    }

    public async Task<IdentityResult> SetActiveAsync(string id, bool isActive)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return IdentityResult.Failed(new IdentityError { Description = "User not found." });
        }

        user.IsActive = isActive;
        return await _userManager.UpdateAsync(user);
    }

    public Task<IdentityResult> DeleteAsync(string id)
    {
        throw new NotImplementedException();
    }
}
