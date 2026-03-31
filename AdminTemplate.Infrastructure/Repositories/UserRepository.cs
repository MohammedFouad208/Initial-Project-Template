using AdminTemplate.Domain.Entities;
using AdminTemplate.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace AdminTemplate.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserRepository(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public Task<ApplicationUser?> GetByIdAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<ApplicationUser>> GetAllAsync()
    {
        throw new NotImplementedException();
    }

    public Task<(IReadOnlyList<ApplicationUser> Items, int TotalCount)> GetPagedAsync(
        int pageIndex,
        int pageSize,
        string? searchTerm = null)
    {
        throw new NotImplementedException();
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

    public Task UpdateAsync(ApplicationUser user)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(Guid id)
    {
        throw new NotImplementedException();
    }
}
