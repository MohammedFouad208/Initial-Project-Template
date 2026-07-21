using System.Text.Json;
using System.Text.Json.Nodes;
using AdminTemplate.Application.DTOs;
using AdminTemplate.Application.Providers;
using Microsoft.Extensions.Configuration;

namespace AdminTemplate.Infrastructure.Providers;

public sealed class JsonLkpProvider : ILkpProvider
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
    private List<LkpTableDto> _tables = [];

    public JsonLkpProvider(IConfiguration configuration, string? contentRootPath = null)
    {
        var configuredPath = configuration["Lkp:FilePath"];
        var relativePath = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine("Config", "Lkp.json")
            : configuredPath;

        var basePath = contentRootPath ?? AppContext.BaseDirectory;

        _filePath = Path.IsPathRooted(relativePath)
            ? relativePath
            : Path.Combine(basePath, relativePath);

        Load();
    }

    public IReadOnlyList<LkpTableDto> GetAll() => _tables.AsReadOnly();

    public void AddTable(string name, string displayName)
    {
        if (_tables.Any(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase)))
            return;

        _tables.Add(new LkpTableDto(name, displayName));
        Save();
    }

    public void RemoveTable(string name)
    {
        _tables.RemoveAll(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
        Save();
    }

    public void Reload() => Load();

    private void Load()
    {
        if (!File.Exists(_filePath))
        {
            _tables = [];
            return;
        }

        var json = File.ReadAllText(_filePath);
        var root = JsonNode.Parse(json)?.AsObject();
        if (root is null) { _tables = []; return; }

        var array = root["LkpTables"]?.AsArray();
        if (array is null) { _tables = []; return; }

        _tables = array
            .Select(n => new LkpTableDto(
                n?["Name"]?.GetValue<string>() ?? string.Empty,
                n?["DisplayName"]?.GetValue<string>() ?? string.Empty))
            .Where(t => !string.IsNullOrWhiteSpace(t.Name))
            .ToList();
    }

    private void Save()
    {
        var array = new JsonArray();
        foreach (var t in _tables)
        {
            array.Add(new JsonObject
            {
                ["Name"]        = t.Name,
                ["DisplayName"] = t.DisplayName
            });
        }

        var root = new JsonObject { ["LkpTables"] = array };
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        File.WriteAllText(_filePath, root.ToJsonString(_jsonOptions));
    }
}
