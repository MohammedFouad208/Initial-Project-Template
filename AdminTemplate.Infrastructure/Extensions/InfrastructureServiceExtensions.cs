using AdminTemplate.Application.Interfaces;
using AdminTemplate.Application.Providers;
using AdminTemplate.Application.Services;
using AdminTemplate.Domain.Interfaces;
using AdminTemplate.Infrastructure.Data;
using AdminTemplate.Infrastructure.Providers;
using AdminTemplate.Infrastructure.Repositories;
using AdminTemplate.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AdminTemplate.Infrastructure.Extensions;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddSingleton<IPermissionProvider, JsonPermissionProvider>();

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IPermissionService, PermissionService>();

        services.AddScoped<DataSeeder>();

        // Application-level and cross-cutting services for Auth
        services.AddScoped<IEmailSender, AdminTemplate.Infrastructure.Providers.NoOpEmailSender>();
        services.AddScoped<AdminTemplate.Application.Interfaces.IAccountService, AdminTemplate.Infrastructure.Services.AccountService>();

        return services;
    }
}
