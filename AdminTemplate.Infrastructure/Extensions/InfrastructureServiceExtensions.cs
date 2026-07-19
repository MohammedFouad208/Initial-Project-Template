using AdminTemplate.Application.Interfaces;
using AdminTemplate.Application.Interfaces.CodeGen;
using AdminTemplate.Application.Providers;
using AdminTemplate.Application.Services;
using AdminTemplate.Domain.Interfaces;
using AdminTemplate.Infrastructure.Data;
using AdminTemplate.Infrastructure.Providers;
using AdminTemplate.Infrastructure.Repositories;
using AdminTemplate.Infrastructure.Seed;
using AdminTemplate.Infrastructure.Services;
using AdminTemplate.Infrastructure.Services.CodeGen;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AdminTemplate.Infrastructure.Extensions;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, IHostEnvironment? environment = null)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddSingleton<IPermissionProvider, JsonPermissionProvider>();
        services.AddSingleton<IObjectsProvider, JsonObjectsProvider>();
        services.AddSingleton<ILkpProvider>(sp =>
            new JsonLkpProvider(configuration, environment?.ContentRootPath));

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IPermissionService, PermissionService>();

        services.AddScoped<DataSeeder>();

        // Application-level and cross-cutting services for Auth
        services.AddScoped<IEmailSender, AdminTemplate.Infrastructure.Providers.NoOpEmailSender>();
        services.AddScoped<AdminTemplate.Application.Interfaces.IAccountService, AdminTemplate.Infrastructure.Services.AccountService>();

        // Code generation services
        services.AddScoped<IEntityBuilderService, EntityBuilderService>();
        services.AddScoped<IEntityPermissionSeeder, EntityPermissionSeeder>();
        services.AddScoped<IAppRestartService, AppRestartService>();
        services.AddSingleton<IThemeService, ThemeService>();

        var templatesPath = configuration["CodeGen:TemplatesPath"]
            ?? Path.Combine(AppContext.BaseDirectory, "CodeGenTemplates");

        services.AddSingleton<ICodeGeneratorService>(_ => new CodeGeneratorService(templatesPath));

        return services;
    }
}
