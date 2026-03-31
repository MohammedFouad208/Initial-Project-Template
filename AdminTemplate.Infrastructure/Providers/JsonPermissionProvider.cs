using System.Text.Json;
using AdminTemplate.Application.DTOs;
using AdminTemplate.Application.Providers;
using Microsoft.Extensions.Configuration;

namespace AdminTemplate.Infrastructure.Providers;

public sealed class JsonPermissionProvider : IPermissionProvider
{
    private readonly IReadOnlyList<PermissionObjectDto> _permissions;

    public JsonPermissionProvider(IConfiguration configuration)
    {
        var configuredPath = configuration["Permissions:FilePath"];
        var relativePath = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine("Config", "permissions.json")
            : configuredPath;

        var fullPath = Path.IsPathRooted(relativePath)
            ? relativePath
            : Path.Combine(AppContext.BaseDirectory, relativePath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("permissions.json file was not found.", fullPath);
        }

        var json = File.ReadAllText(fullPath);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var data = JsonSerializer.Deserialize<PermissionConfig>(json, options)
            ?? throw new InvalidOperationException("Failed to deserialize permissions.json.");

        _permissions = data.PermissionObjects;
    }

    public IReadOnlyList<PermissionObjectDto> GetAll()
    {
        return _permissions;
    }

    private sealed class PermissionConfig
    {
        public IReadOnlyList<PermissionObjectDto> PermissionObjects { get; init; } = [];
    }
}
