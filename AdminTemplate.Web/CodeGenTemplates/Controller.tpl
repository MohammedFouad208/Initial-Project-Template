using System.Security.Claims;
using AdminTemplate.Application.Common.DataTable;
using AdminTemplate.Application.Interfaces;
using AdminTemplate.Domain.Entities;
using AdminTemplate.Infrastructure.Data;
using AdminTemplate.Web.Filters;
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
    private readonly I{{ entity.name }}Service _service;
    private readonly IPermissionService _permissionService;
    {{- if entity.relations | array.size > 0 }}
    private readonly ApplicationDbContext _db;
    {{- end }}

    public {{ entity.name }}Controller(
        I{{ entity.name }}Service service,
        IPermissionService permissionService{{- if entity.relations | array.size > 0 }},
        ApplicationDbContext db{{- end }})
    {
        _service           = service;
        _permissionService = permissionService;
        {{- if entity.relations | array.size > 0 }}
        _db                = db;
        {{- end }}
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
        var (items, total) = await _service.GetPagedAsync(request, request.Filters);

        return Json(new DataTableResponse<object>
        {
            Draw            = request.Draw,
            RecordsTotal    = total,
            RecordsFiltered = total,
            Data            = items.ToList()
        });
    }

    // ─── Details ─────────────────────────────────────────────────────────────

    [HttpGet]
    [HasPermission("{{ entity.name }}", "Browse")]
    public async Task<IActionResult> Details(Guid id)
    {
        var item = await _service.GetByIdAsync(id);
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
            await _service.CreateAsync(model);
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
        var item = await _service.GetByIdAsync(id);
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

        try
        {
            var updated = await _service.UpdateAsync(id, model);
            if (!updated) return RedirectToAction(nameof(Index));
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
        var deleted = await _service.DeleteAsync(id);
        if (!deleted)
            return Json(new { success = false, message = "Record not found." });

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
