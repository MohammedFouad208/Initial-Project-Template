using System.Security.Claims;
using AdminTemplate.Application.DTOs;
using AdminTemplate.Application.Interfaces;
using AdminTemplate.Web.Filters;
using AdminTemplate.Application.Common.DataTable;

using AdminTemplate.Web.ViewModels.Roles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AdminTemplate.Web.Controllers;

[Authorize]
public class RolesController : Controller
{
    private readonly IRoleService _roleService;
    private readonly IPermissionService _permissionService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public RolesController(IRoleService roleService, IPermissionService permissionService, IStringLocalizer<SharedResource> localizer)
    {
        _roleService       = roleService;
        _permissionService = permissionService;
        _localizer         = localizer;
    }

    // ─── Browse ───────────────────────────────────────────────────────────

    [HttpGet]
    [HasPermission("Role", "Browse")]
    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        ViewBag.CanCreate           = await _permissionService.UserHasPermissionAsync(userId, "Role", "Create");
        ViewBag.CanUpdate           = await _permissionService.UserHasPermissionAsync(userId, "Role", "Update");
        ViewBag.CanDelete           = await _permissionService.UserHasPermissionAsync(userId, "Role", "Delete");
        ViewBag.CanManagePermissions = await _permissionService.UserHasPermissionAsync(userId, "Role", "AssignPermissions");
        return View();
    }

    [HttpGet]
    [HasPermission("Role", "Browse")]
    public async Task<IActionResult> GetData([FromQuery] DataTableRequest request)
    {
        string[] columnMap = ["name", "description", "userCount", "permissionCount", "createdAt"];
        var sortCol = request.SortColumn >= 0 && request.SortColumn < columnMap.Length
            ? columnMap[request.SortColumn]
            : "name";

        var pageIndex = request.Length > 0 ? request.Start / request.Length : 0;

        var (items, total) = await _roleService.GetPagedAsync(
            pageIndex,
            request.Length,
            request.Search,
            sortCol,
            request.SortDirection);

        return Json(new DataTableResponse<RoleDto>
        {
            Draw            = request.Draw,
            RecordsTotal    = total,
            RecordsFiltered = total,
            Data            = items
        });
    }

    // ─── Create ───────────────────────────────────────────────────────────

    [HttpGet]
    [HasPermission("Role", "Create")]
    public IActionResult Create()
    {
        return View(new CreateRoleViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("Role", "Create")]
    public async Task<IActionResult> Create(CreateRoleViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var dto    = new CreateRoleDto(vm.Name.Trim(), vm.Description?.Trim());
        var result = await _roleService.CreateAsync(dto);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError("Name", error.Description);
            return View(vm);
        }

        TempData["ToastMessage"] = _localizer["Roles_Toast_Created"];
        TempData["ToastType"] = "success";
        return RedirectToAction(nameof(Index));
    }

    // ─── Edit ─────────────────────────────────────────────────────────────

    [HttpGet]
    [HasPermission("Role", "Update")]
    public async Task<IActionResult> Edit(string id)
    {
        var dto = await _roleService.GetByIdAsync(id);
        if (dto is null)
            return RedirectToAction(nameof(Index));

        var vm = new EditRoleViewModel
        {
            Id          = dto.Id,
            Name        = dto.Name,
            Description = dto.Description
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("Role", "Update")]
    public async Task<IActionResult> Edit(string id, EditRoleViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var dto    = new UpdateRoleDto(vm.Name.Trim(), vm.Description?.Trim());
        var result = await _roleService.UpdateAsync(id, dto);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError("Name", error.Description);
            return View(vm);
        }

        TempData["ToastMessage"] = _localizer["Roles_Toast_Updated"];
        TempData["ToastType"] = "success";
        return RedirectToAction(nameof(Index));
    }

    // ─── Delete ───────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("Role", "Delete")]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _roleService.DeleteAsync(id);

        if (result.Succeeded)
            return Json(new { success = true });

        return Json(new
        {
            success = false,
            message = result.Errors.FirstOrDefault()?.Description ?? _localizer["Roles_Error_DeleteFailed"]
        });
    }

    // ─── Helpers ──────────────────────────────────────────────────────────

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : Guid.Empty;
    }
}
