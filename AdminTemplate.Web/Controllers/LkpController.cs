using System.Security.Claims;
using AdminTemplate.Application.Interfaces;
using AdminTemplate.Application.Providers;
using AdminTemplate.Infrastructure.Data;
using AdminTemplate.Web.Filters;
using AdminTemplate.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AdminTemplate.Web.Controllers;

[Authorize]
[HasPermission("Lkp", "Browse")]
public class LkpController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ILkpProvider _lkpProvider;
    private readonly IPermissionService _permissionService;

    public LkpController(ApplicationDbContext db, ILkpProvider lkpProvider, IPermissionService permissionService)
    {
        _db             = db;
        _lkpProvider    = lkpProvider;
        _permissionService = permissionService;
    }

    // ── Index ────────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        ViewBag.CanCreate = await _permissionService.UserHasPermissionAsync(userId, "Lkp", "Create");
        ViewBag.CanUpdate = await _permissionService.UserHasPermissionAsync(userId, "Lkp", "Update");
        ViewBag.CanDelete = await _permissionService.UserHasPermissionAsync(userId, "Lkp", "Delete");
        ViewBag.Tables    = _lkpProvider.GetAll();
        return View();
    }

    // ── GetData (server-side datatable for a specific lkp table) ─────────────

    [HttpGet]
    public async Task<IActionResult> GetData([FromQuery] DataTableRequest request, [FromQuery] string table)
    {
        if (!IsValidTableName(table))
            return BadRequest("Invalid table name.");

        if (!TableExists(table))
            return Json(new DataTableResponse<object> { Draw = request.Draw, RecordsTotal = 0, RecordsFiltered = 0, Data = [] });

        var nameFilter   = request.Filters.TryGetValue("name",   out var nf) ? nf?.Trim() : null;
        var nameEnFilter = request.Filters.TryGetValue("nameEn", out var ef) ? ef?.Trim() : null;

        var whereClause = BuildWhereClause(nameFilter, nameEnFilter);
        var orderClause = BuildOrderClause(request.SortColumn, request.SortDirection);

        var countSql = $"SELECT COUNT(*) FROM [{table}]{whereClause}";
        var dataSql  = $"""
            SELECT Id, Name, NameEn, IsActive, CreatedAt
            FROM [{table}]{whereClause}
            {orderClause}
            OFFSET {request.Start} ROWS FETCH NEXT {request.Length} ROWS ONLY
            """;

        int total;
        using (var cmd = _db.Database.GetDbConnection().CreateCommand())
        {
            await _db.Database.OpenConnectionAsync();
            cmd.CommandText = countSql;
            AddFilterParameters(cmd, nameFilter, nameEnFilter);
            total = (int)(await cmd.ExecuteScalarAsync() ?? 0);
        }

        var items = new List<object>();
        using (var cmd = _db.Database.GetDbConnection().CreateCommand())
        {
            cmd.CommandText = dataSql;
            AddFilterParameters(cmd, nameFilter, nameEnFilter);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(new
                {
                    id        = reader.GetGuid(0),
                    name      = reader.IsDBNull(1) ? null : reader.GetString(1),
                    nameEn    = reader.IsDBNull(2) ? null : reader.GetString(2),
                    isActive  = reader.IsDBNull(3) ? true : reader.GetBoolean(3),
                    createdAt = reader.IsDBNull(4) ? (DateTime?)null : reader.GetDateTime(4)
                });
            }
        }

        return Json(new DataTableResponse<object>
        {
            Draw            = request.Draw,
            RecordsTotal    = total,
            RecordsFiltered = total,
            Data            = items
        });
    }

    // ── Create ───────────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("Lkp", "Create")]
    public async Task<IActionResult> Create([FromForm] string table, [FromForm] string name, [FromForm] string nameEn, [FromForm] bool isActive = true)
    {
        if (!IsValidTableName(table))
            return Json(new { success = false, message = "Invalid table name." });

        if (string.IsNullOrWhiteSpace(name))
            return Json(new { success = false, message = "Name is required." });

        if (!TableExists(table))
            return Json(new { success = false, message = $"Table '{table}' does not exist." });

        var id  = Guid.NewGuid();
        var sql = $"INSERT INTO [{table}] (Id, Name, NameEn, IsActive, CreatedAt) VALUES (@Id, @Name, @NameEn, @IsActive, @CreatedAt)";
        await _db.Database.ExecuteSqlRawAsync(sql,
            new SqlParameter("@Id",        id),
            new SqlParameter("@Name",      name.Trim()),
            new SqlParameter("@NameEn",    string.IsNullOrWhiteSpace(nameEn) ? (object)DBNull.Value : nameEn.Trim()),
            new SqlParameter("@IsActive",  isActive),
            new SqlParameter("@CreatedAt", DateTime.UtcNow));

        return Json(new { success = true });
    }

    // ── Edit ─────────────────────────────────────────────────────────────────

    [HttpGet]
    [HasPermission("Lkp", "Update")]
    public async Task<IActionResult> GetRow([FromQuery] string table, [FromQuery] Guid id)
    {
        if (!IsValidTableName(table) || !TableExists(table))
            return NotFound();

        var sql = $"SELECT Id, Name, NameEn, IsActive FROM [{table}] WHERE Id = @Id";
        using var cmd = _db.Database.GetDbConnection().CreateCommand();
        await _db.Database.OpenConnectionAsync();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new SqlParameter("@Id", id));
        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return NotFound();

        return Json(new
        {
            id       = reader.GetGuid(0),
            name     = reader.IsDBNull(1) ? null : reader.GetString(1),
            nameEn   = reader.IsDBNull(2) ? null : reader.GetString(2),
            isActive = reader.IsDBNull(3) ? true : reader.GetBoolean(3)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("Lkp", "Update")]
    public async Task<IActionResult> Edit([FromForm] string table, [FromForm] Guid id, [FromForm] string name, [FromForm] string nameEn, [FromForm] bool isActive = true)
    {
        if (!IsValidTableName(table))
            return Json(new { success = false, message = "Invalid table name." });

        if (string.IsNullOrWhiteSpace(name))
            return Json(new { success = false, message = "Name is required." });

        if (!TableExists(table))
            return Json(new { success = false, message = $"Table '{table}' does not exist." });

        var sql = $"UPDATE [{table}] SET Name = @Name, NameEn = @NameEn, IsActive = @IsActive WHERE Id = @Id";
        await _db.Database.ExecuteSqlRawAsync(sql,
            new SqlParameter("@Name",     name.Trim()),
            new SqlParameter("@NameEn",   string.IsNullOrWhiteSpace(nameEn) ? (object)DBNull.Value : nameEn.Trim()),
            new SqlParameter("@IsActive", isActive),
            new SqlParameter("@Id",       id));

        return Json(new { success = true });
    }

    // ── Delete ───────────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("Lkp", "Delete")]
    public async Task<IActionResult> Delete([FromForm] string table, [FromForm] Guid id)
    {
        if (!IsValidTableName(table) || !TableExists(table))
            return Json(new { success = false, message = "Table not found." });

        await _db.Database.ExecuteSqlRawAsync(
            $"DELETE FROM [{table}] WHERE Id = @Id",
            new SqlParameter("@Id", id));

        return Json(new { success = true });
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : Guid.Empty;
    }

    private bool TableExists(string table) =>
        _db.Database
            .SqlQueryRaw<int>($"SELECT COUNT(*) AS [Value] FROM sys.tables WHERE name = '{table}'")
            .AsEnumerable()
            .FirstOrDefault() > 0;

    private static bool IsValidTableName(string? table)
    {
        if (string.IsNullOrWhiteSpace(table)) return false;
        // Only allow alphanumeric + underscore (no SQL injection via table name)
        return System.Text.RegularExpressions.Regex.IsMatch(table, @"^[A-Za-z][A-Za-z0-9_]*$");
    }

    private static string BuildWhereClause(string? name, string? nameEn)
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(name))   parts.Add("Name   LIKE '%' + @Name   + '%'");
        if (!string.IsNullOrEmpty(nameEn)) parts.Add("NameEn LIKE '%' + @NameEn + '%'");
        return parts.Count > 0 ? " WHERE " + string.Join(" AND ", parts) : string.Empty;
    }

    private static string BuildOrderClause(int sortColumn, string sortDirection)
    {
        var dir = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
        var col = sortColumn switch { 1 => "NameEn", 2 => "IsActive", 3 => "CreatedAt", _ => "Name" };
        return $"ORDER BY {col} {dir}";
    }

    private static void AddFilterParameters(System.Data.Common.DbCommand cmd, string? name, string? nameEn)
    {
        if (!string.IsNullOrEmpty(name))
            cmd.Parameters.Add(new SqlParameter("@Name", name));
        if (!string.IsNullOrEmpty(nameEn))
            cmd.Parameters.Add(new SqlParameter("@NameEn", nameEn));
    }
}
