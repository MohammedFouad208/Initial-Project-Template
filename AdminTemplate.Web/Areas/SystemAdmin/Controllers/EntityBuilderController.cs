using AdminTemplate.Application.DTOs.CodeGen;
using AdminTemplate.Application.Interfaces.CodeGen;
using AdminTemplate.Application.Providers;
using AdminTemplate.Infrastructure.Data;
using AdminTemplate.Web.Areas.SystemAdmin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdminTemplate.Web.Areas.SystemAdmin.Controllers;

[Area("SystemAdmin")]
[Authorize(Policy = "SuperAdminOnly")]
public class EntityBuilderController : Controller
{
    private readonly IEntityBuilderService _entityBuilder;
    private readonly ICodeGeneratorService _codeGenerator;
    private readonly IEntityPermissionSeeder _permissionSeeder;
    private readonly IAppRestartService _appRestart;
    private readonly ILkpProvider _lkpProvider;
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;

    public EntityBuilderController(
        IEntityBuilderService entityBuilder,
        ICodeGeneratorService codeGenerator,
        IEntityPermissionSeeder permissionSeeder,
        IAppRestartService appRestart,
        ILkpProvider lkpProvider,
        ApplicationDbContext db,
        IWebHostEnvironment env)
    {
        _entityBuilder    = entityBuilder;
        _codeGenerator    = codeGenerator;
        _permissionSeeder = permissionSeeder;
        _appRestart       = appRestart;
        _lkpProvider      = lkpProvider;
        _db               = db;
        _env              = env;
    }

    // ── Browse ───────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var entities = await _entityBuilder.GetAllAsync();
        return View(entities);
    }

    // ── Create ───────────────────────────────────────────────────────────────

    [HttpGet]
    public IActionResult Create()
    {
        ViewBag.EntityId = (Guid?)null;
        return View(new CreateEntityViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateEntityViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.EntityId = (Guid?)null;
            return View(vm);
        }

        var dto = new CreateEntityDto(
            vm.Name,
            vm.Description,
            vm.TableName,
            vm.Columns.Select(c => new CreateEntityColumnDto(
                c.Name, c.DataType, c.IsRequired, c.IsUnique,
                c.MaxLength, c.SortOrder, c.DefaultValue, c.ShowInList, c.ShowInForm, c.UseInSearch)).ToList(),
            vm.Relations.Select(r => new CreateEntityRelationDto(
                r.RelatedEntityName, r.ForeignKeyName, r.NavigationPropertyName,
                r.DisplayColumn, r.RelationType)).ToList());

        var entity = await _entityBuilder.CreateEntityAsync(dto);

        TempData["Success"] = $"Entity '{entity.Name}' created successfully.";
        return RedirectToAction(nameof(Details), new { id = entity.Id });
    }

    // ── Edit ─────────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var entity = await _entityBuilder.GetByIdAsync(id);
        if (entity is null || entity.IsGenerated)
            return RedirectToAction(nameof(Details), new { id });

        var vm = new CreateEntityViewModel
        {
            Name        = entity.Name,
            Description = entity.Description,
            TableName   = entity.TableName,
            Columns     = entity.Columns.OrderBy(c => c.SortOrder).Select(c => new ColumnViewModel
            {
                Name         = c.Name,
                DataType     = c.DataType,
                IsRequired   = c.IsRequired,
                IsUnique     = c.IsUnique,
                MaxLength    = c.MaxLength,
                SortOrder    = c.SortOrder,
                DefaultValue = c.DefaultValue,
                ShowInList   = c.ShowInList,
                ShowInForm   = c.ShowInForm,
                UseInSearch  = c.UseInSearch
            }).ToList(),
            Relations = entity.Relations.Select(r => new RelationViewModel
            {
                RelatedEntityName      = r.RelatedEntityName,
                ForeignKeyName         = r.ForeignKeyName,
                NavigationPropertyName = r.NavigationPropertyName,
                DisplayColumn          = r.DisplayColumn,
                RelationType           = r.RelationType
            }).ToList()
        };

        ViewBag.EntityId = id;
        return View("Create", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, CreateEntityViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.EntityId = id;
            return View("Create", vm);
        }

        var dto = new CreateEntityDto(
            vm.Name,
            vm.Description,
            vm.TableName,
            vm.Columns.Select(c => new CreateEntityColumnDto(
                c.Name, c.DataType, c.IsRequired, c.IsUnique,
                c.MaxLength, c.SortOrder, c.DefaultValue, c.ShowInList, c.ShowInForm, c.UseInSearch)).ToList(),
            vm.Relations.Select(r => new CreateEntityRelationDto(
                r.RelatedEntityName, r.ForeignKeyName, r.NavigationPropertyName,
                r.DisplayColumn, r.RelationType)).ToList());

        var updated = await _entityBuilder.UpdateEntityAsync(id, dto);
        if (updated is null)
        {
            TempData["Error"] = "Entity not found or already generated — cannot edit.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = $"Entity '{updated.Name}' updated successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // ── Details ──────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var entity = await _entityBuilder.GetByIdAsync(id);
        if (entity is null) return RedirectToAction(nameof(Index));
        return View(entity);
    }

    // ── Generate ─────────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(Guid id)
    {
        var entity = await _entityBuilder.GetByIdAsync(id);
        if (entity is null)
            return Json(new { success = false, message = "Entity not found." });

        // 1. Generate SQL and execute
        var sql = await _entityBuilder.GenerateSqlAsync(entity);
        try
        {
            await _entityBuilder.ExecuteSqlAsync(sql);
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"SQL error: {ex.Message}" });
        }

        // 2. Generate code files
        var projectRoot = Path.GetFullPath(Path.Combine(_env.ContentRootPath, ".."));
        var result = await _codeGenerator.GenerateAsync(entity, projectRoot);

        if (!result.Success)
            return Json(new { success = false, message = result.Error });

        // 3. Seed permissions
        await _permissionSeeder.SeedPermissionsAsync(entity.Name);

        // 4. Mark entity as generated in DB
        await _entityBuilder.MarkGeneratedAsync(id);

        // Release the DB connection before the long-running build — prevents SqlException timeout
        await _db.Database.CloseConnectionAsync();

        // 5. Build project and restart so new controller/views are picked up
        try
        {
            await _appRestart.BuildAndRestartAsync(projectRoot);
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Build failed: {ex.Message}" });
        }

        return Json(new
        {
            success = true,
            message = $"Entity '{entity.Name}' generated successfully. {result.GeneratedFiles.Count} files created.",
            files = result.GeneratedFiles
        });
    }

    // ── Delete ───────────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _entityBuilder.GetByIdAsync(id);
        if (entity is null)
            return Json(new { success = false, message = "Entity not found." });

        var projectRoot = Path.GetFullPath(Path.Combine(_env.ContentRootPath, ".."));

        // Remove generated files, DbSet, Objects.json, permissions.json entries
        await _codeGenerator.DeleteGeneratedAsync(entity, projectRoot);

        // Drop table and remove entity definition from DB
        await _entityBuilder.DeleteEntityAsync(id);

        // If this is a lkp entity, remove it from Lkp.json
        if (entity.TableName is not null)
            _lkpProvider.RemoveTable(entity.TableName);

        // Release the DB connection before the long-running build — prevents SqlException timeout
        await _db.Database.CloseConnectionAsync();

        // Rebuild so removed controller/views are no longer referenced
        try
        {
            await _appRestart.BuildAndRestartAsync(projectRoot);
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Entity deleted but build failed: {ex.Message}" });
        }

        return Json(new { success = true, message = $"Entity '{entity.Name}' and all generated artifacts deleted." });
    }

    // ── Create Lkp ───────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateLkp([FromForm] string lkpName)
    {
        if (string.IsNullOrWhiteSpace(lkpName) ||
            !System.Text.RegularExpressions.Regex.IsMatch(lkpName.Trim(), @"^[A-Za-z][A-Za-z0-9_]*$"))
            return Json(new { success = false, message = "Invalid name. Use PascalCase letters and digits only." });

        var name        = lkpName.Trim();
        var tableName   = $"Lkp_{name}";
        var displayName = System.Text.RegularExpressions.Regex.Replace(name, "([A-Z])", " $1").Trim();

        var createSql = $"""
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '{tableName}')
            BEGIN
                CREATE TABLE [{tableName}] (
                    [Id]        UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
                    [Name]      NVARCHAR(200)    NOT NULL,
                    [NameEn]    NVARCHAR(200)    NULL,
                    [IsActive]  BIT              NOT NULL DEFAULT 1,
                    [CreatedAt] DATETIME2        NOT NULL DEFAULT GETUTCDATE()
                );
            END
            """;

        try
        {
            await _db.Database.ExecuteSqlRawAsync(createSql);
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"SQL error: {ex.Message}" });
        }

        // Register in EntityDefinitions so it appears in Entity Builder
        var alreadyExists = (await _entityBuilder.GetAllAsync())
            .Any(e => string.Equals(e.Name, tableName, StringComparison.OrdinalIgnoreCase));

        if (!alreadyExists)
        {
            var dto = new CreateEntityDto(
                tableName,
                $"Lookup table for {displayName}",
                tableName,
                [
                    new CreateEntityColumnDto("Name",     "string", true,  false, 200, 0, null, true,  true,  true),
                    new CreateEntityColumnDto("NameEn",   "string", false, false, 200, 1, null, true,  true,  true),
                    new CreateEntityColumnDto("IsActive", "bool",   true,  false, null, 2, null, true, true,  false),
                ],
                []);

            var created = await _entityBuilder.CreateEntityAsync(dto);

            // Mark as generated immediately — table already exists, no code-gen needed
            await _entityBuilder.MarkGeneratedAsync(created.Id);
        }

        var projectRoot  = Path.GetFullPath(Path.Combine(_env.ContentRootPath, ".."));
        var entityPath   = Path.Combine(projectRoot, "AdminTemplate.Domain", "Entities", $"{tableName}.cs");
        var dbContextPath = Path.Combine(projectRoot, "AdminTemplate.Infrastructure", "Data", "ApplicationDbContext.cs");

        // Write domain entity class
        if (!System.IO.File.Exists(entityPath))
        {
            var entityContent = $$$"""
                using AdminTemplate.Domain.Common;
                using System.ComponentModel.DataAnnotations;
                using System.ComponentModel.DataAnnotations.Schema;

                namespace AdminTemplate.Domain.Entities;

                [Table("{{{tableName}}}")]
                public class {{{tableName}}} : BaseEntity
                {
                    [Required]
                    [MaxLength(200)]
                    public string Name { get; set; } = string.Empty;

                    [MaxLength(200)]
                    public string? NameEn { get; set; }

                    public bool IsActive { get; set; } = true;
                }
                """;
            await System.IO.File.WriteAllTextAsync(entityPath, entityContent);
        }

        // Add DbSet to ApplicationDbContext
        AddLkpDbSetToContext(dbContextPath, tableName);

        // Add to Lkp.json
        _lkpProvider.AddTable(tableName, displayName);

        return Json(new { success = true, message = $"Lookup table '{displayName}' created successfully." });
    }

    // ── Delete Lkp ───────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteLkp([FromForm] string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName) ||
            !System.Text.RegularExpressions.Regex.IsMatch(tableName.Trim(), @"^[A-Za-z][A-Za-z0-9_]*$"))
            return Json(new { success = false, message = "Invalid table name." });

        var name = tableName.Trim();

        try
        {
            await _db.Database.ExecuteSqlRawAsync(
                $"IF EXISTS (SELECT * FROM sys.tables WHERE name = '{name}') DROP TABLE [{name}]");
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"SQL error: {ex.Message}" });
        }

        _lkpProvider.RemoveTable(name);

        // Remove EntityDefinition record if present
        var entityDef = (await _entityBuilder.GetAllAsync())
            .FirstOrDefault(e => string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase));
        if (entityDef is not null)
            await _entityBuilder.DeleteEntityAsync(entityDef.Id);

        var projectRoot   = Path.GetFullPath(Path.Combine(_env.ContentRootPath, ".."));
        var entityPath    = Path.Combine(projectRoot, "AdminTemplate.Domain", "Entities", $"{name}.cs");
        var dbContextPath = Path.Combine(projectRoot, "AdminTemplate.Infrastructure", "Data", "ApplicationDbContext.cs");

        // Remove domain entity class
        if (System.IO.File.Exists(entityPath))
            System.IO.File.Delete(entityPath);

        // Remove DbSet from ApplicationDbContext
        RemoveLkpDbSetFromContext(dbContextPath, name);

        return Json(new { success = true, message = $"Lookup table '{name}' deleted." });
    }

    // ── Lkp domain helpers ───────────────────────────────────────────────────

    private static void AddLkpDbSetToContext(string dbContextPath, string tableName)
    {
        if (!System.IO.File.Exists(dbContextPath)) return;

        var source = System.IO.File.ReadAllText(dbContextPath);
        if (source.Contains($"DbSet<{tableName}>")) return;

        var dbSetLine = $"    public DbSet<{tableName}> {tableName} => Set<{tableName}>();";
        var lastDbSet = source.LastIndexOf("public DbSet<");
        if (lastDbSet < 0) return;

        var lineEnd = source.IndexOf('\n', lastDbSet);
        if (lineEnd < 0) return;

        source = source.Insert(lineEnd + 1, dbSetLine + "\n");
        System.IO.File.WriteAllText(dbContextPath, source);
    }

    private static void RemoveLkpDbSetFromContext(string dbContextPath, string tableName)
    {
        if (!System.IO.File.Exists(dbContextPath)) return;

        var lines = System.IO.File.ReadAllLines(dbContextPath).ToList();
        lines.RemoveAll(l => l.Contains($"DbSet<{tableName}>"));
        System.IO.File.WriteAllLines(dbContextPath, lines);
    }

    // ── Preview SQL ──────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> PreviewSql(Guid id)
    {
        var entity = await _entityBuilder.GetByIdAsync(id);
        if (entity is null) return NotFound();
        var sql = await _entityBuilder.GenerateSqlAsync(entity);
        return Content(sql, "text/plain");
    }

}
