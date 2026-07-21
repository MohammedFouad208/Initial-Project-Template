using AdminTemplate.Application.DTOs.CodeGen;
using AdminTemplate.Application.Interfaces.CodeGen;
using AdminTemplate.Application.Providers;
using AdminTemplate.Infrastructure.Data;
using AdminTemplate.Web.Areas.SystemAdmin.Models;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

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
    private readonly IStringLocalizer<SharedResource> _localizer;

    public EntityBuilderController(
        IEntityBuilderService entityBuilder,
        ICodeGeneratorService codeGenerator,
        IEntityPermissionSeeder permissionSeeder,
        IAppRestartService appRestart,
        ILkpProvider lkpProvider,
        ApplicationDbContext db,
        IWebHostEnvironment env,
        IStringLocalizer<SharedResource> localizer)
    {
        _entityBuilder    = entityBuilder;
        _codeGenerator    = codeGenerator;
        _permissionSeeder = permissionSeeder;
        _appRestart       = appRestart;
        _lkpProvider      = lkpProvider;
        _db               = db;
        _env              = env;
        _localizer        = localizer;
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

        TempData["Success"] = string.Format(_localizer["Toast_EntityCreated"], entity.Name);
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
            TempData["Error"] = _localizer["Error_EntityNotFoundOrGenerated"];
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = string.Format(_localizer["Toast_EntityUpdated"], updated.Name);
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
            return Json(new { success = false, message = _localizer["Error_EntityNotFound"].Value });

        var projectRoot = Path.GetFullPath(Path.Combine(_env.ContentRootPath, ".."));

        // 1. Generate code files. Writing the entity class + adding the DbSet makes the
        //    new entity part of the EF Core model, so EF can scaffold the table itself.
        var result = await _codeGenerator.GenerateAsync(entity, projectRoot);
        if (!result.Success)
            return Json(new { success = false, message = result.Error });

        // 2. Seed permissions
        await _permissionSeeder.SeedPermissionsAsync(entity.Name);

        // 3. Mark entity as generated in DB
        await _entityBuilder.MarkGeneratedAsync(id);

        // Release the DB connection before the long-running migration/build — prevents SqlException timeout
        await _db.Database.CloseConnectionAsync();

        // 4. Create an EF Core migration from the new entity and apply it to the database.
        //    The table is created by EF (model diff) — never by raw SQL.
        var (migrationOk, migrationOutput) =
            await RunMigrationAsync($"Create{entity.Name}_{DateTime.UtcNow:yyyyMMddHHmmss}", projectRoot);
        if (!migrationOk)
            return Json(new { success = false, message = migrationOutput });

        return Json(new
        {
            success = true,
            message = string.Format(_localizer["Toast_EntityGenerated"], entity.Name, result.GeneratedFiles.Count),
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
            return Json(new { success = false, message = _localizer["Error_EntityNotFound"].Value });

        var projectRoot = Path.GetFullPath(Path.Combine(_env.ContentRootPath, ".."));

        // Remove generated files, DbSet, Objects.json, permissions.json entries.
        // Removing the entity class + DbSet takes the entity out of the EF model.
        await _codeGenerator.DeleteGeneratedAsync(entity, projectRoot);

        // Remove the entity definition metadata from the DB
        await _entityBuilder.DeleteEntityAsync(id, projectRoot);

        // If this is a lkp entity, remove it from Lkp.json
        if (entity.TableName is not null)
            _lkpProvider.RemoveTable(entity.TableName);

        // Release the DB connection before the long-running migration/build — prevents SqlException timeout
        await _db.Database.CloseConnectionAsync();

        // Drop the table via a reverse EF migration. EF diffs the model (now missing the
        // entity) and scaffolds a DropTable migration — never a raw DROP TABLE.
        // Pending (never-generated) entities have no table/DbSet, so there is nothing to drop.
        if (entity.IsGenerated)
        {
            var (migrationOk, migrationOutput) =
                await RunMigrationAsync($"Drop{entity.Name}_{DateTime.UtcNow:yyyyMMddHHmmss}", projectRoot);
            if (!migrationOk)
                return Json(new { success = false, message = migrationOutput });
        }

        return Json(new { success = true, message = string.Format(_localizer["Toast_EntityDeleted"], entity.Name) });
    }

    // ── Create Lkp ───────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateLkp([FromForm] string lkpName)
    {
        if (string.IsNullOrWhiteSpace(lkpName) ||
            !System.Text.RegularExpressions.Regex.IsMatch(lkpName.Trim(), @"^[A-Za-z][A-Za-z0-9_]*$"))
            return Json(new { success = false, message = _localizer["Error_InvalidLkpName"].Value });

        var name        = lkpName.Trim();
        var tableName   = $"Lkp_{name}";
        var displayName = System.Text.RegularExpressions.Regex.Replace(name, "([A-Z])", " $1").Trim();

        var projectRoot   = Path.GetFullPath(Path.Combine(_env.ContentRootPath, ".."));
        var entityPath    = Path.Combine(projectRoot, "AdminTemplate.Domain", "Entities", $"{tableName}.cs");
        var dbContextPath = Path.Combine(projectRoot, "AdminTemplate.Infrastructure", "Data", "ApplicationDbContext.cs");

        // 1. Write the domain entity class and register its DbSet so the table becomes
        //    part of the EF Core model — the table is then created by an EF migration,
        //    not by raw SQL.
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

        AddLkpDbSetToContext(dbContextPath, tableName);

        // 2. Register in EntityDefinitions so it appears in Entity Builder
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
            await _entityBuilder.MarkGeneratedAsync(created.Id);
        }

        // 3. Add to Lkp.json
        _lkpProvider.AddTable(tableName, displayName);

        // Release the DB connection before the long-running migration/build
        await _db.Database.CloseConnectionAsync();

        // 4. Create the table via an EF Core migration
        var (migrationOk, migrationOutput) =
            await RunMigrationAsync($"Create{tableName}_{DateTime.UtcNow:yyyyMMddHHmmss}", projectRoot);
        if (!migrationOk)
            return Json(new { success = false, message = migrationOutput });

        return Json(new { success = true, message = string.Format(_localizer["Toast_LkpCreated"], displayName) });
    }

    // ── Delete Lkp ───────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteLkp([FromForm] string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName) ||
            !System.Text.RegularExpressions.Regex.IsMatch(tableName.Trim(), @"^[A-Za-z][A-Za-z0-9_]*$"))
            return Json(new { success = false, message = _localizer["Error_InvalidTableName"].Value });

        var name = tableName.Trim();

        var projectRoot   = Path.GetFullPath(Path.Combine(_env.ContentRootPath, ".."));
        var entityPath    = Path.Combine(projectRoot, "AdminTemplate.Domain", "Entities", $"{name}.cs");
        var dbContextPath = Path.Combine(projectRoot, "AdminTemplate.Infrastructure", "Data", "ApplicationDbContext.cs");

        // 1. Remove the domain entity class and its DbSet so the table leaves the EF model.
        if (System.IO.File.Exists(entityPath))
            System.IO.File.Delete(entityPath);
        RemoveLkpDbSetFromContext(dbContextPath, name);

        // 2. Remove from Lkp.json
        _lkpProvider.RemoveTable(name);

        // 3. Remove EntityDefinition metadata if present
        var entityDef = (await _entityBuilder.GetAllAsync())
            .FirstOrDefault(e => string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase));
        if (entityDef is not null)
            await _entityBuilder.DeleteEntityAsync(entityDef.Id, projectRoot);

        // Release the DB connection before the long-running migration/build
        await _db.Database.CloseConnectionAsync();

        // 4. Drop the table via a reverse EF migration (model diff → DropTable).
        var (migrationOk, migrationOutput) =
            await RunMigrationAsync($"Drop{name}_{DateTime.UtcNow:yyyyMMddHHmmss}", projectRoot);
        if (!migrationOk)
            return Json(new { success = false, message = migrationOutput });

        return Json(new { success = true, message = string.Format(_localizer["Toast_LkpDeleted"], name) });
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

    // ── EF Migration Orchestration ─────────────────────────────────────────────

    // Scaffolds a migration from the current EF model and applies it to the database.
    // EF derives the schema change (CreateTable / DropTable) by diffing the model against
    // the snapshot, so tables are always created/dropped through EF — never raw SQL.
    private async Task<(bool ok, string output)> RunMigrationAsync(string migrationName, string projectRoot)
    {
        var (addOut, addErr, addExit) = await RunDotnetEfAsync(
            $"migrations add {migrationName} --project AdminTemplate.Infrastructure --startup-project AdminTemplate.Web",
            projectRoot);

        // If the model already matches the database there is nothing to apply — treat as success.
        if ((addOut + addErr).Contains("No changes were detected", StringComparison.OrdinalIgnoreCase))
            return (true, addOut);

        if (addExit != 0)
            return (false, $"migrations add failed:\n{addOut}\n{addErr}");

        var (updOut, updErr, updExit) = await RunDotnetEfAsync(
            "database update --project AdminTemplate.Infrastructure --startup-project AdminTemplate.Web",
            projectRoot);

        if (updExit != 0)
            return (false, $"database update failed:\n{updOut}\n{updErr}");

        return (true, addOut + "\n" + updOut);
    }

    private static async Task<(string output, string error, int exitCode)> RunDotnetEfAsync(string args, string workingDir)
    {
        using var process = new System.Diagnostics.Process();
        process.StartInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName               = "dotnet",
            // Build in Release: the running app locks bin\Debug, so a Debug build
            // would fail with MSB3021 (file in use) while the site is up.
            Arguments              = $"ef {args} --configuration Release",
            WorkingDirectory       = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            CreateNoWindow         = true
        };
        process.Start();
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask  = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return (await outputTask, await errorTask, process.ExitCode);
    }

}
