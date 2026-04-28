using System.Security.Claims;
using AdminTemplate.Application.Interfaces;
using AdminTemplate.Domain.Entities;
using AdminTemplate.Infrastructure.Data;
using AdminTemplate.Web.Filters;
using AdminTemplate.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AdminTemplate.Web.Controllers;

/// <summary>
/// Auto-generated controller for: {{ entity.name }}
/// Generated at: {{ generated_at }}
/// </summary>
[Authorize]
public partial class {{ entity.name }}Controller : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IPermissionService _permissionService;

    public {{ entity.name }}Controller(ApplicationDbContext db, IPermissionService permissionService)
    {
        _db                = db;
        _permissionService = permissionService;
    }

    // ─── Browse ──────────────────────────────────────────────────────────────

    [HttpGet]
    [HasPermission("{{ entity.name }}", "Browse")]
    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        ViewBag.CanCreate = await _permissionService.UserHasPermissionAsync(userId, "{{ entity.name }}", "Create");
        ViewBag.CanUpdate = await _permissionService.UserHasPermissionAsync(userId, "{{ entity.name }}", "Update");
        ViewBag.CanDelete = await _permissionService.UserHasPermissionAsync(userId, "{{ entity.name }}", "Delete");
        return View();
    }

    [HttpGet]
    [HasPermission("{{ entity.name }}", "Browse")]
    public async Task<IActionResult> GetData([FromQuery] DataTableRequest request)
    {
        // Column map — order must match the columns array in Index.cshtml
        string[] columnMap =
        [
            {{- for col in entity.columns }}
            {{- if col.show_in_list }}
            "{{ col.name | string.slice 0 1 | string.downcase }}{{ col.name | string.slice 1 }}",
            {{- end }}
            {{- end }}
        ];

        var sortCol = request.SortColumn >= 0 && request.SortColumn < columnMap.Length
            ? columnMap[request.SortColumn]
            : columnMap.Length > 0 ? columnMap[0] : "id";

        var pageIndex = request.Length > 0 ? request.Start / request.Length : 0;

        var query = _db.Set<{{ entity.name }}>()
            {{- for rel in entity.relations }}
            .Include(x => x.{{ rel.navigation_property_name }})
            {{- end }}
            .AsQueryable();

        // ── Search ────────────────────────────────────────────────────────────
        {{- for col in entity.columns }}
        {{- if col.use_in_search }}
        if (request.Filters.TryGetValue("{{ col.name | string.slice 0 1 | string.downcase }}{{ col.name | string.slice 1 }}", out var filter{{ col.name }}) && !string.IsNullOrWhiteSpace(filter{{ col.name }}))
        {
            var term{{ col.name }} = filter{{ col.name }}.Trim().ToLower();
            query = query.Where(x => x.{{ col.name }}.ToLower().Contains(term{{ col.name }}));
        }
        {{- end }}
        {{- end }}
        {{- for rel in entity.relations }}
        if (request.Filters.TryGetValue("rel_{{ rel.navigation_property_name | string.slice 0 1 | string.downcase }}{{ rel.navigation_property_name | string.slice 1 }}", out var filterRel{{ rel.navigation_property_name }}) && !string.IsNullOrWhiteSpace(filterRel{{ rel.navigation_property_name }}))
        {
            if (Guid.TryParse(filterRel{{ rel.navigation_property_name }}.Trim(), out var relId{{ rel.navigation_property_name }}))
                query = query.Where(x => x.{{ rel.foreign_key_name }} == relId{{ rel.navigation_property_name }});
        }
        {{- end }}

        var total = await query.CountAsync();

        // ── Sort ──────────────────────────────────────────────────────────────
        query = sortCol switch
        {
            {{- for col in entity.columns }}
            {{- if col.show_in_list }}
            "{{ col.name | string.slice 0 1 | string.downcase }}{{ col.name | string.slice 1 }}" =>
                request.SortDirection == "desc"
                    ? query.OrderByDescending(x => x.{{ col.name }})
                    : query.OrderBy(x => x.{{ col.name }}),
            {{- end }}
            {{- end }}
            _ => query.OrderBy(x => x.CreatedAt)
        };

        var items = await query
            .Skip(pageIndex * request.Length)
            .Take(request.Length)
            .Select(x => new
            {
                x.Id,
                {{- for col in entity.columns }}
                {{- if col.show_in_list }}
                x.{{ col.name }},
                {{- end }}
                {{- end }}
                {{- for rel in entity.relations }}
                {{ rel.navigation_property_name }}{{ rel.display_column }} = x.{{ rel.navigation_property_name }} != null
                    ? x.{{ rel.navigation_property_name }}.{{ rel.display_column }}
                    : null,
                {{- end }}
            })
            .ToListAsync();

        return Json(new DataTableResponse<object>
        {
            Draw            = request.Draw,
            RecordsTotal    = total,
            RecordsFiltered = total,
            Data            = items
        });
    }

    // ─── Details ─────────────────────────────────────────────────────────────

    [HttpGet]
    [HasPermission("{{ entity.name }}", "Browse")]
    public async Task<IActionResult> Details(Guid id)
    {
        var item = await _db.Set<{{ entity.name }}>()
            {{- for rel in entity.relations }}
            .Include(x => x.{{ rel.navigation_property_name }})
            {{- end }}
            .FirstOrDefaultAsync(x => x.Id == id);

        if (item is null) return RedirectToAction(nameof(Index));

        var userId = GetCurrentUserId();
        var vm = new AdminTemplate.Web.Models.ViewModels.{{ entity.name }}DetailsViewModel
        {
            Id        = item.Id,
            CreatedAt = item.CreatedAt,
            {{- for col in entity.columns }}
            {{ col.name }} = item.{{ col.name }},
            {{- end }}
            {{- for rel in entity.relations }}
            {{ rel.navigation_property_name }}Display = item.{{ rel.navigation_property_name }}?.{{ rel.display_column }} ?? "—",
            {{- end }}
        };

        ViewBag.CanUpdate = await _permissionService.UserHasPermissionAsync(userId, "{{ entity.name }}", "Update");
        ViewBag.CanDelete = await _permissionService.UserHasPermissionAsync(userId, "{{ entity.name }}", "Delete");
        return View(vm);
    }

    // ─── Create ──────────────────────────────────────────────────────────────

    [HttpGet]
    [HasPermission("{{ entity.name }}", "Create")]
    public async Task<IActionResult> Create()
    {
        {{- for rel in entity.relations }}
        ViewBag.{{ rel.related_entity_name }}List = new SelectList(
            await _db.Set<{{ rel.related_entity_name }}>().OrderBy(x => x.{{ rel.display_column }}).ToListAsync(),
            "Id", "{{ rel.display_column }}");
        {{- end }}
        ViewBag.IsEdit = false;
        return View(new {{ entity.name }}());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("{{ entity.name }}", "Create")]
    public async Task<IActionResult> Create({{ entity.name }} model)
    {
        if (!ModelState.IsValid)
        {
            {{- for rel in entity.relations }}
            ViewBag.{{ rel.related_entity_name }}List = new SelectList(
                await _db.Set<{{ rel.related_entity_name }}>().OrderBy(x => x.{{ rel.display_column }}).ToListAsync(),
                "Id", "{{ rel.display_column }}", model.{{ rel.foreign_key_name }});
            {{- end }}
            ViewBag.IsEdit = false;
            return View(model);
        }

        try
        {
            model.Id        = Guid.NewGuid();
            model.CreatedAt = DateTime.UtcNow;
            _db.Set<{{ entity.name }}>().Add(model);
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            {{- for rel in entity.relations }}
            ViewBag.{{ rel.related_entity_name }}List = new SelectList(
                await _db.Set<{{ rel.related_entity_name }}>().OrderBy(x => x.{{ rel.display_column }}).ToListAsync(),
                "Id", "{{ rel.display_column }}", model.{{ rel.foreign_key_name }});
            {{- end }}
            ViewBag.IsEdit = false;
            ModelState.AddModelError(string.Empty, $"An error occurred while saving: {ex.Message}");
            return View(model);
        }

        TempData["Success"] = "{{ entity.name }} created successfully.";
        return RedirectToAction(nameof(Index));
    }

    // ─── Edit ────────────────────────────────────────────────────────────────

    [HttpGet]
    [HasPermission("{{ entity.name }}", "Update")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var item = await _db.Set<{{ entity.name }}>().FindAsync(id);
        if (item is null) return RedirectToAction(nameof(Index));

        {{- for rel in entity.relations }}
        ViewBag.{{ rel.related_entity_name }}List = new SelectList(
            await _db.Set<{{ rel.related_entity_name }}>().OrderBy(x => x.{{ rel.display_column }}).ToListAsync(),
            "Id", "{{ rel.display_column }}", item.{{ rel.foreign_key_name }});
        {{- end }}
        ViewBag.IsEdit  = true;
        ViewBag.ItemId  = id;
        return View("Create", item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("{{ entity.name }}", "Update")]
    public async Task<IActionResult> Edit(Guid id, {{ entity.name }} model)
    {
        if (!ModelState.IsValid)
        {
            {{- for rel in entity.relations }}
            ViewBag.{{ rel.related_entity_name }}List = new SelectList(
                await _db.Set<{{ rel.related_entity_name }}>().OrderBy(x => x.{{ rel.display_column }}).ToListAsync(),
                "Id", "{{ rel.display_column }}", model.{{ rel.foreign_key_name }});
            {{- end }}
            ViewBag.IsEdit = true;
            ViewBag.ItemId = id;
            return View("Create", model);
        }

        var existing = await _db.Set<{{ entity.name }}>().FindAsync(id);
        if (existing is null) return RedirectToAction(nameof(Index));

        try
        {
            {{- for col in entity.columns }}
            existing.{{ col.name }} = model.{{ col.name }};
            {{- end }}
            {{- for rel in entity.relations }}
            existing.{{ rel.foreign_key_name }} = model.{{ rel.foreign_key_name }};
            {{- end }}

            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            {{- for rel in entity.relations }}
            ViewBag.{{ rel.related_entity_name }}List = new SelectList(
                await _db.Set<{{ rel.related_entity_name }}>().OrderBy(x => x.{{ rel.display_column }}).ToListAsync(),
                "Id", "{{ rel.display_column }}", model.{{ rel.foreign_key_name }});
            {{- end }}
            ViewBag.IsEdit = true;
            ViewBag.ItemId = id;
            ModelState.AddModelError(string.Empty, $"An error occurred while saving: {ex.Message}");
            return View("Create", model);
        }

        TempData["Success"] = "{{ entity.name }} updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    // ─── Delete ──────────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("{{ entity.name }}", "Delete")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var item = await _db.Set<{{ entity.name }}>().FindAsync(id);
        if (item is null)
            return Json(new { success = false, message = "Record not found." });

        _db.Set<{{ entity.name }}>().Remove(item);
        await _db.SaveChangesAsync();

        return Json(new { success = true });
    }

    // ─── Lookup endpoints (for relation dropdowns in search toolbar) ─────────

    {{- for rel in entity.relations }}
    [HttpGet]
    [HasPermission("{{ entity.name }}", "Browse")]
    public async Task<IActionResult> GetLookup{{ rel.related_entity_name }}()
    {
        var items = await _db.Set<{{ rel.related_entity_name }}>()
            .OrderBy(x => x.{{ rel.display_column }})
            .Select(x => new { id = x.Id, text = x.{{ rel.display_column }} })
            .ToListAsync();
        return Json(items);
    }
    {{- end }}

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : Guid.Empty;
    }
}
