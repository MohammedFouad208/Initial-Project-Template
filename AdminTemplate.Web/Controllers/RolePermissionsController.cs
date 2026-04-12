using AdminTemplate.Application.DTOs;
using AdminTemplate.Application.Interfaces;
using AdminTemplate.Application.Providers;
using AdminTemplate.Web.Filters;
using AdminTemplate.Web.ViewModels.RolePermissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminTemplate.Web.Controllers;

[Authorize]
public class RolePermissionsController : Controller
{
    private readonly IRoleService _roleService;
    private readonly IPermissionService _permissionService;
    private readonly IPermissionProvider _permissionProvider;

    public RolePermissionsController(
        IRoleService roleService,
        IPermissionService permissionService,
        IPermissionProvider permissionProvider)
    {
        _roleService        = roleService;
        _permissionService  = permissionService;
        _permissionProvider = permissionProvider;
    }

    // ─── GET /RolePermissions/{roleId} ────────────────────────────────────

    [HttpGet]
    [HasPermission("Role", "AssignPermissions")]
    public async Task<IActionResult> Index(string roleId)
    {
        if (!Guid.TryParse(roleId, out var guid))
            return NotFound();

        var role = await _roleService.GetByIdAsync(roleId);
        if (role is null)
            return NotFound();

        var allObjects = _permissionProvider.GetAll();
        var assigned   = await _permissionService.GetRolePermissionsAsync(guid);
        var keys       = assigned
            .Select(p => $"{p.ObjectName}|{p.FunctionName}")
            .ToHashSet();

        var vm = new RolePermissionsViewModel
        {
            RoleId       = roleId,
            RoleName     = role.Name,
            AllObjects   = allObjects,
            AssignedKeys = keys,
        };

        return View(vm);
    }

    // ─── POST /RolePermissions/Save ───────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("Role", "AssignPermissions")]
    public async Task<IActionResult> Save(string roleId, List<string>? selectedPermissions)
    {
        if (!Guid.TryParse(roleId, out var guid))
            return NotFound();

        try
        {
            var permissions = (selectedPermissions ?? [])
                .Select(item => item.Split('|'))
                .Where(parts => parts.Length == 2)
                .Select(parts => new PermissionDto(parts[0], parts[1]));

            await _permissionService.SaveRolePermissionsAsync(guid, permissions);
            TempData["Success"] = "Permissions saved successfully.";
        }
        catch (Exception)
        {
            TempData["Error"] = "An error occurred while saving permissions.";
        }

        return RedirectToAction(nameof(Index), new { roleId });
    }
}
