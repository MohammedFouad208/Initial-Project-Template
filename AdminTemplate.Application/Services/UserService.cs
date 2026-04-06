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

    public Task<UserDto?> GetByIdAsync(string id)
    {
        throw new NotImplementedException();
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

    public Task<(IReadOnlyList<UserDto> Items, int TotalCount)> GetPagedAsync(
        int pageIndex,
        int pageSize,
        string? searchTerm = null)
    {
        throw new NotImplementedException();
    }

    public Task<IdentityResult> CreateAsync(CreateUserDto dto)
    {
        throw new NotImplementedException();
    }

    public Task<IdentityResult> UpdateAsync(string id, UpdateUserDto dto)
    {
        throw new NotImplementedException();
    }

    public Task<IdentityResult> SetActiveAsync(string id, bool isActive)
    {
        throw new NotImplementedException();
    }

    public Task<IdentityResult> DeleteAsync(string id)
    {
        throw new NotImplementedException();
    }
}
