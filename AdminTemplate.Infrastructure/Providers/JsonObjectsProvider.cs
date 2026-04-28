using System.Text.Json;
using AdminTemplate.Application.DTOs;
using AdminTemplate.Application.Providers;
using Microsoft.Extensions.Configuration;

namespace AdminTemplate.Infrastructure.Providers;

public sealed class JsonObjectsProvider : IObjectsProvider
{
    private readonly IReadOnlyList<SidebarObjectDto> _objects;

    public JsonObjectsProvider(IConfiguration configuration)
    {
        var configuredPath = configuration["Objects:FilePath"];
        var relativePath = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine("Config", "Objects.json")
            : configuredPath;

        var fullPath = Path.IsPathRooted(relativePath)
            ? relativePath
            : Path.Combine(AppContext.BaseDirectory, relativePath);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Objects.json file was not found.", fullPath);

        var json = File.ReadAllText(fullPath);

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var data = JsonSerializer.Deserialize<ObjectsConfig>(json, options)
            ?? throw new InvalidOperationException("Failed to deserialize Objects.json.");

        _objects = data.Objects;
    }

    public IReadOnlyList<SidebarObjectDto> GetAll() => _objects;

    private sealed class ObjectsConfig
    {
        public IReadOnlyList<SidebarObjectDto> Objects { get; init; } = [];
    }
}
