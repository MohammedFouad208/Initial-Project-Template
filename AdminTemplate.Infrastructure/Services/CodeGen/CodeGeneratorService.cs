using System.Text.Json;
using System.Text.Json.Nodes;
using AdminTemplate.Application.DTOs.CodeGen;
using AdminTemplate.Application.Interfaces.CodeGen;
using Scriban;
using Scriban.Runtime;

namespace AdminTemplate.Infrastructure.Services.CodeGen;

public class CodeGeneratorService : ICodeGeneratorService
{
    private readonly string _templatesPath;

    public CodeGeneratorService(string templatesPath)
    {
        _templatesPath = templatesPath;
    }

    public async Task<GenerationResult> GenerateAsync(EntityDefinitionDto entity, string projectRootPath)
    {
        var generatedFiles = new List<string>();

        try
        {
            CheckWriteAccess(projectRootPath);

            var model = BuildScribanModel(entity);

            // Model
            var modelOutput = await RenderTemplateAsync("Model.tpl", model);
            var modelPath = Path.Combine(projectRootPath, "AdminTemplate.Domain", "Entities", $"{entity.Name}.cs");
            await WriteProtectedAsync(modelPath, modelOutput);
            generatedFiles.Add(modelPath);

            // Controller
            var controllerOutput = await RenderTemplateAsync("Controller.tpl", model);
            var controllerPath = Path.Combine(projectRootPath, "AdminTemplate.Web", "Controllers", $"{entity.Name}Controller.cs");
            await WriteProtectedAsync(controllerPath, controllerOutput);
            generatedFiles.Add(controllerPath);

            // Views folder
            var viewsDir = Path.Combine(projectRootPath, "AdminTemplate.Web", "Views", entity.Name);
            Directory.CreateDirectory(viewsDir);

            // Index view
            var indexOutput = await RenderTemplateAsync("Index.tpl", model);
            var indexPath = Path.Combine(viewsDir, "Index.cshtml");
            await WriteProtectedAsync(indexPath, indexOutput);
            generatedFiles.Add(indexPath);

            // Create view
            var createOutput = await RenderTemplateAsync("Create.tpl", model);
            var createPath = Path.Combine(viewsDir, "Create.cshtml");
            await WriteProtectedAsync(createPath, createOutput);
            generatedFiles.Add(createPath);

            // Details view
            var detailsOutput = await RenderTemplateAsync("Details.tpl", model);
            var detailsPath = Path.Combine(viewsDir, "Details.cshtml");
            await WriteProtectedAsync(detailsPath, detailsOutput);
            generatedFiles.Add(detailsPath);

            // DetailsViewModel
            var viewModelsDir = Path.Combine(projectRootPath, "AdminTemplate.Web", "Models", "ViewModels");
            Directory.CreateDirectory(viewModelsDir);
            var detailsVmOutput = await RenderTemplateAsync("DetailsViewModel.tpl", model);
            var detailsVmPath = Path.Combine(viewModelsDir, $"{entity.Name}DetailsViewModel.cs");
            await WriteProtectedAsync(detailsVmPath, detailsVmOutput);
            generatedFiles.Add(detailsVmPath);

            // DbContext — add DbSet
            var dbContextPath = Path.Combine(projectRootPath, "AdminTemplate.Infrastructure", "Data", "ApplicationDbContext.cs");
            AddDbSetToContext(dbContextPath, entity.Name);

            // Objects.json — add sidebar entry
            var objectsJsonPath = Path.Combine(projectRootPath, "AdminTemplate.Web", "Config", "Objects.json");
            AddEntryToObjectsJson(objectsJsonPath, entity.Name);

            // permissions.json — register entity so it appears in role-permission management
            var permissionsJsonPath = Path.Combine(projectRootPath, "AdminTemplate.Web", "Config", "permissions.json");
            AddEntryToPermissionsJson(permissionsJsonPath, entity.Name);

            return new GenerationResult(true, null, generatedFiles);
        }
        catch (Exception ex)
        {
            return new GenerationResult(false, ex.Message, generatedFiles);
        }
    }

    public Task DeleteGeneratedAsync(EntityDefinitionDto entity, string projectRootPath)
    {
        // Generated source files
        var filesToDelete = new[]
        {
            Path.Combine(projectRootPath, "AdminTemplate.Domain",    "Entities",    $"{entity.Name}.cs"),
            Path.Combine(projectRootPath, "AdminTemplate.Web",       "Controllers", $"{entity.Name}Controller.cs"),
            Path.Combine(projectRootPath, "AdminTemplate.Web",       "Models", "ViewModels", $"{entity.Name}DetailsViewModel.cs"),
        };

        foreach (var file in filesToDelete)
        {
            if (File.Exists(file)) File.Delete(file);
        }

        // Views folder
        var viewsDir = Path.Combine(projectRootPath, "AdminTemplate.Web", "Views", entity.Name);
        if (Directory.Exists(viewsDir)) Directory.Delete(viewsDir, recursive: true);

        var dbContextPath   = Path.Combine(projectRootPath, "AdminTemplate.Infrastructure", "Data", "ApplicationDbContext.cs");
        var objectsJsonPath = Path.Combine(projectRootPath, "AdminTemplate.Web", "Config", "Objects.json");
        var permissionsPath = Path.Combine(projectRootPath, "AdminTemplate.Web", "Config", "permissions.json");

        RemoveDbSetFromContext(dbContextPath, entity.Name);
        RemoveEntryFromJson(objectsJsonPath, "Objects",           entity.Name);
        RemoveEntryFromJson(permissionsPath, "PermissionObjects", entity.Name);

        return Task.CompletedTask;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<string> RenderTemplateAsync(string templateFile, ScriptObject model)
    {
        var templatePath = Path.Combine(_templatesPath, templateFile);

        if (!File.Exists(templatePath))
            throw new FileNotFoundException($"Template not found: {templateFile}", templatePath);

        var templateContent = await File.ReadAllTextAsync(templatePath);
        var template = Template.Parse(templateContent);

        if (template.HasErrors)
        {
            var errors = string.Join("; ", template.Messages.Select(m => m.Message));
            throw new InvalidOperationException($"Template '{templateFile}' has errors: {errors}");
        }

        var context = new TemplateContext { StrictVariables = false };
        context.PushGlobal(model);

        return await template.RenderAsync(context);
    }

    private static ScriptObject BuildScribanModel(EntityDefinitionDto entity)
    {
        var obj = new ScriptObject();
        obj["generated_at"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

        var entityObj = new ScriptObject();
        entityObj["name"]        = entity.Name;
        entityObj["description"] = entity.Description ?? string.Empty;
        entityObj["table_name"]  = entity.TableName ?? entity.Name + "s";

        var columns = new ScriptArray();
        foreach (var col in entity.Columns.OrderBy(c => c.SortOrder))
        {
            var colObj = new ScriptObject();
            colObj["name"]          = col.Name;
            colObj["data_type"]     = col.DataType.ToLowerInvariant();
            colObj["cs_type"]       = MapToCsType(col.DataType);
            colObj["is_required"]   = col.IsRequired;
            colObj["is_unique"]     = col.IsUnique;
            colObj["max_length"]    = col.MaxLength;
            colObj["sort_order"]    = col.SortOrder;
            colObj["default_value"] = col.DefaultValue;
            colObj["show_in_list"]   = col.ShowInList;
            colObj["show_in_form"]   = col.ShowInForm;
            colObj["use_in_search"]  = col.UseInSearch;
            columns.Add(colObj);
        }
        entityObj["columns"] = columns;

        var relations = new ScriptArray();
        foreach (var rel in entity.Relations)
        {
            var relObj = new ScriptObject();
            relObj["related_entity_name"]       = rel.RelatedEntityName;
            relObj["foreign_key_name"]           = rel.ForeignKeyName;
            relObj["navigation_property_name"]   = rel.NavigationPropertyName;
            relObj["display_column"]             = rel.DisplayColumn;
            relObj["relation_type"]              = rel.RelationType;
            relations.Add(relObj);
        }
        entityObj["relations"] = relations;

        obj["entity"] = entityObj;
        return obj;
    }

    private static string MapToCsType(string dataType) => dataType.ToLowerInvariant() switch
    {
        "string"   => "string",
        "int"      => "int",
        "long"     => "long",
        "decimal"  => "decimal",
        "double"   => "double",
        "bool"     => "bool",
        "datetime" => "DateTime",
        "guid"     => "Guid",
        _          => "string"
    };

    private static void CheckWriteAccess(string path)
    {
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"Project root not found: {path}");

        var testFile = Path.Combine(path, ".write_test_" + Guid.NewGuid());
        try
        {
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
        }
        catch (UnauthorizedAccessException)
        {
            throw new UnauthorizedAccessException($"No write access to project path: {path}");
        }
    }

    private static string Pluralize(string name)
    {
        if (name.EndsWith("y", StringComparison.OrdinalIgnoreCase) && name.Length > 1)
            return name[..^1] + "ies";
        if (name.EndsWith("s", StringComparison.OrdinalIgnoreCase))
            return name + "es";
        return name + "s";
    }

    private static void AddDbSetToContext(string dbContextPath, string entityName)
    {
        if (!File.Exists(dbContextPath)) return;

        var source = File.ReadAllText(dbContextPath);
        var dbSetLine = $"    public DbSet<{entityName}> {Pluralize(entityName)} => Set<{entityName}>();";

        if (source.Contains($"DbSet<{entityName}>")) return;

        // Insert after the last existing DbSet declaration
        var lastDbSet = source.LastIndexOf("public DbSet<");
        if (lastDbSet < 0) return;

        var lineEnd = source.IndexOf('\n', lastDbSet);
        if (lineEnd < 0) return;

        source = source.Insert(lineEnd + 1, dbSetLine + "\n");
        File.WriteAllText(dbContextPath, source);
    }

    private static void AddEntryToObjectsJson(string objectsJsonPath, string entityName)
    {
        if (!File.Exists(objectsJsonPath)) return;

        var json = File.ReadAllText(objectsJsonPath);
        var root = JsonNode.Parse(json)?.AsObject();
        if (root is null) return;

        var array = root["Objects"]?.AsArray();
        if (array is null) return;

        // Skip if already present
        foreach (var item in array)
        {
            if (string.Equals(item?["Name"]?.GetValue<string>(), entityName, StringComparison.OrdinalIgnoreCase))
                return;
        }

        var entry = new JsonObject
        {
            ["Name"]        = entityName,
            ["DisplayName"] = Pluralize(entityName),
            ["Icon"]        = "fa-solid fa-table",
            ["Controller"]  = entityName,
            ["Functions"]   = new JsonArray("Browse", "Create", "Update", "Delete")
        };

        array.Add(entry);

        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(objectsJsonPath, root.ToJsonString(options));
    }

    private static void AddEntryToPermissionsJson(string permissionsJsonPath, string entityName)
    {
        if (!File.Exists(permissionsJsonPath)) return;

        var json = File.ReadAllText(permissionsJsonPath);
        var root = JsonNode.Parse(json)?.AsObject();
        if (root is null) return;

        var array = root["PermissionObjects"]?.AsArray();
        if (array is null) return;

        // Skip if already present
        foreach (var item in array)
        {
            if (string.Equals(item?["Name"]?.GetValue<string>(), entityName, StringComparison.OrdinalIgnoreCase))
                return;
        }

        var entry = new JsonObject
        {
            ["Name"]        = entityName,
            ["DisplayName"] = Pluralize(entityName),
            ["Functions"]   = new JsonArray("Browse", "Create", "Update", "Delete")
        };

        array.Add(entry);

        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(permissionsJsonPath, root.ToJsonString(options));
    }

    private static void RemoveDbSetFromContext(string dbContextPath, string entityName)
    {
        if (!File.Exists(dbContextPath)) return;

        var lines = File.ReadAllLines(dbContextPath).ToList();
        lines.RemoveAll(l => l.Contains($"DbSet<{entityName}>"));
        File.WriteAllLines(dbContextPath, lines);
    }

    private static void RemoveEntryFromJson(string jsonPath, string arrayKey, string entityName)
    {
        if (!File.Exists(jsonPath)) return;

        var json = File.ReadAllText(jsonPath);
        var root = JsonNode.Parse(json)?.AsObject();
        if (root is null) return;

        var array = root[arrayKey]?.AsArray();
        if (array is null) return;

        var toRemove = array
            .Select((node, i) => (node, i))
            .Where(x => string.Equals(x.node?["Name"]?.GetValue<string>(), entityName, StringComparison.OrdinalIgnoreCase))
            .Select(x => x.i)
            .OrderDescending()
            .ToList();

        foreach (var i in toRemove)
            array.RemoveAt(i);

        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(jsonPath, root.ToJsonString(options));
    }

    // Only writes if file doesn't already have a [GeneratedCode] guard meaning it was hand-edited
    private static async Task WriteProtectedAsync(string path, string content)
    {
        if (File.Exists(path))
        {
            var existing = await File.ReadAllTextAsync(path);
            // If the file contains the "no-regenerate" marker, skip it
            if (existing.Contains("// <no-regenerate>"))
                return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, content);
    }
}
