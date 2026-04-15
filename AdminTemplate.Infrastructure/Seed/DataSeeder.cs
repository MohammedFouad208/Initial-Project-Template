using AdminTemplate.Application.Providers;
using AdminTemplate.Domain.Entities;
using AdminTemplate.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace AdminTemplate.Infrastructure.Seed;

public class DataSeeder
{
    private const string SuperAdminRoleName = "SuperAdmin";
    private const string AdminRoleName = "Admin";
    private const string ViewerRoleName = "Viewer";

    private static readonly string[] AdminFunctions = ["Browse", "Create", "Update"];
    private static readonly string[] ViewerFunctions = ["Browse"];

    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IPermissionProvider _permissionProvider;
    private readonly IConfiguration _configuration;
    private readonly UserManager<ApplicationUser> _userManager;

    public DataSeeder(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IPermissionProvider permissionProvider,
        IConfiguration configuration,
        UserManager<ApplicationUser> userManager)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _permissionProvider = permissionProvider;
        _configuration = configuration;
        _userManager = userManager;
    }

    public async Task SeedAsync()
    {
        var superAdminEmail = _configuration["Seed:SuperAdminEmail"];
        var superAdminPassword = _configuration["Seed:SuperAdminPassword"];

        if (string.IsNullOrWhiteSpace(superAdminEmail) || string.IsNullOrWhiteSpace(superAdminPassword))
        {
            throw new InvalidOperationException("Seed credentials are missing from configuration.");
        }

        var role = await EnsureSuperAdminRoleAsync();
        var user = await EnsureSuperAdminUserAsync(superAdminEmail, superAdminPassword);

        if (!await _userManager.IsInRoleAsync(user, SuperAdminRoleName))
        {
            var addRoleResult = await _userManager.AddToRoleAsync(user, SuperAdminRoleName);
            if (!addRoleResult.Succeeded)
            {
                var error = string.Join("; ", addRoleResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to assign super admin role: {error}");
            }
        }

        if (role.Id == Guid.Empty)
        {
            throw new InvalidOperationException("SuperAdmin role was created without an Id.");
        }

        await SeedPermissionsAsync(role.Id);
        await SeedDemoRolesAsync();
    }

    private async Task SeedDemoRolesAsync()
    {
        var adminEmail = _configuration["Seed:AdminEmail"];
        var adminPassword = _configuration["Seed:AdminPassword"];
        if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
        {
            var adminRole = await EnsureRoleAsync(AdminRoleName, "Demo administrator role");
            await SeedPermissionsForFunctionsAsync(adminRole.Id, AdminFunctions);
            await EnsureUserInRoleAsync(adminEmail, adminPassword, "Demo Admin", AdminRoleName);
        }

        var viewerEmail = _configuration["Seed:ViewerEmail"];
        var viewerPassword = _configuration["Seed:ViewerPassword"];
        if (!string.IsNullOrWhiteSpace(viewerEmail) && !string.IsNullOrWhiteSpace(viewerPassword))
        {
            var viewerRole = await EnsureRoleAsync(ViewerRoleName, "Demo viewer role (read-only)");
            await SeedPermissionsForFunctionsAsync(viewerRole.Id, ViewerFunctions);
            await EnsureUserInRoleAsync(viewerEmail, viewerPassword, "Demo Viewer", ViewerRoleName);
        }
    }

    private async Task<ApplicationRole> EnsureRoleAsync(string roleName, string description)
    {
        var role = await _roleRepository.GetByNameAsync(roleName);
        if (role is not null)
        {
            return role;
        }

        var newRole = new ApplicationRole
        {
            Name = roleName,
            NormalizedName = roleName.ToUpperInvariant(),
            Description = description,
            CreatedAt = DateTime.UtcNow
        };

        await _roleRepository.AddAsync(newRole);

        return await _roleRepository.GetByNameAsync(roleName)
            ?? throw new InvalidOperationException($"{roleName} role creation failed.");
    }

    private async Task SeedPermissionsForFunctionsAsync(Guid roleId, string[] allowedFunctions)
    {
        var permissionObjects = _permissionProvider.GetAll();

        foreach (var permissionObject in permissionObjects)
        {
            foreach (var functionName in permissionObject.Functions)
            {
                if (!allowedFunctions.Contains(functionName))
                {
                    continue;
                }

                var exists = await _permissionRepository.ExistsAsync(roleId, permissionObject.Name, functionName);
                if (exists)
                {
                    continue;
                }

                await _permissionRepository.AddAsync(new RolePermission
                {
                    RoleId = roleId,
                    ObjectName = permissionObject.Name,
                    FunctionName = functionName,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
    }

    private async Task EnsureUserInRoleAsync(string email, string password, string fullName, string roleName)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user is null)
        {
            var newUser = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(newUser, password);
            if (!createResult.Succeeded)
            {
                var error = string.Join("; ", createResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create {roleName} user: {error}");
            }

            user = await _userRepository.GetByEmailAsync(email)
                ?? throw new InvalidOperationException($"{roleName} user creation failed.");
        }

        if (!await _userManager.IsInRoleAsync(user, roleName))
        {
            var addRoleResult = await _userManager.AddToRoleAsync(user, roleName);
            if (!addRoleResult.Succeeded)
            {
                var error = string.Join("; ", addRoleResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to assign {roleName} role: {error}");
            }
        }
    }

    private async Task<ApplicationRole> EnsureSuperAdminRoleAsync()
    {
        var role = await _roleRepository.GetByNameAsync(SuperAdminRoleName);
        if (role is not null)
        {
            return role;
        }

        var newRole = new ApplicationRole
        {
            Name = SuperAdminRoleName,
            NormalizedName = SuperAdminRoleName.ToUpperInvariant(),
            Description = "Default super administrator role",
            CreatedAt = DateTime.UtcNow
        };

        await _roleRepository.AddAsync(newRole);

        return await _roleRepository.GetByNameAsync(SuperAdminRoleName)
            ?? throw new InvalidOperationException("SuperAdmin role creation failed.");
    }

    private async Task<ApplicationUser> EnsureSuperAdminUserAsync(string email, string password)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user is not null)
        {
            return user;
        }

        var newUser = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = "Super Administrator",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            EmailConfirmed = true
        };

        var createResult = await _userManager.CreateAsync(newUser, password);
        if (!createResult.Succeeded)
        {
            var error = string.Join("; ", createResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create super admin user: {error}");
        }

        return await _userRepository.GetByEmailAsync(email)
            ?? throw new InvalidOperationException("SuperAdmin user creation failed.");
    }

    private async Task SeedPermissionsAsync(Guid roleId)
    {
        var permissionObjects = _permissionProvider.GetAll();

        foreach (var permissionObject in permissionObjects)
        {
            foreach (var functionName in permissionObject.Functions)
            {
                var exists = await _permissionRepository.ExistsAsync(
                    roleId,
                    permissionObject.Name,
                    functionName);

                if (exists)
                {
                    continue;
                }

                await _permissionRepository.AddAsync(new RolePermission
                {
                    RoleId = roleId,
                    ObjectName = permissionObject.Name,
                    FunctionName = functionName,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
    }
}
