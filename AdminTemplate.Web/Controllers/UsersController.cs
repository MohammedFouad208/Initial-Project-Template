using System.Security.Claims;
using AdminTemplate.Application.DTOs;
using AdminTemplate.Application.Interfaces;
using AdminTemplate.Web.Filters;
using AdminTemplate.Application.Common.DataTable;

using AdminTemplate.Web.ViewModels.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AdminTemplate.Web.Controllers;

[Authorize]
public class UsersController : Controller
{
    private readonly IUserService _userService;
    private readonly IRoleService _roleService;
    private readonly IPermissionService _permissionService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public UsersController(
        IUserService userService,
        IRoleService roleService,
        IPermissionService permissionService,
        IStringLocalizer<SharedResource> localizer)
    {
        _userService = userService;
        _roleService = roleService;
        _permissionService = permissionService;
        _localizer = localizer;
    }

    [HttpGet]
    [HasPermission("User", "Browse")]
    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        ViewBag.CanCreate = await _permissionService.UserHasPermissionAsync(userId, "User", "Create");
        ViewBag.CanUpdate = await _permissionService.UserHasPermissionAsync(userId, "User", "Update");
        ViewBag.CanDelete = await _permissionService.UserHasPermissionAsync(userId, "User", "Delete");
        return View();
    }

    [HttpGet]
    [HasPermission("User", "Browse")]
    public async Task<IActionResult> GetData([FromQuery] DataTableRequest request)
    {
        var (items, total) = await _userService.GetPagedAsync(
            request.Start,
            request.Length,
            request.Search);

        return Json(new DataTableResponse<Application.DTOs.UserDto>
        {
            Draw            = request.Draw,
            RecordsTotal    = total,
            RecordsFiltered = total,
            Data            = items
        });
    }

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : Guid.Empty;
    }

    // ─── Create ───────────────────────────────────────────────────────────

    [HttpGet]
    [HasPermission("User", "Create")]
    public async Task<IActionResult> Create()
    {
        var vm = new CreateUserViewModel
        {
            IsActive       = true,
            AvailableRoles = (await _roleService.GetAllAsync()).ToList()
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("User", "Create")]
    public async Task<IActionResult> Create(CreateUserViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.AvailableRoles = (await _roleService.GetAllAsync()).ToList();
            return View(vm);
        }

        var dto = new CreateUserDto(
            vm.FullName,
            vm.Email,
            vm.Password,
            vm.SelectedRoles.AsReadOnly(),
            vm.IsActive);

        var result = await _userService.CreateAsync(dto);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            vm.AvailableRoles = (await _roleService.GetAllAsync()).ToList();
            return View(vm);
        }

        TempData["ToastMessage"] = _localizer["Users_Toast_Created"];
        TempData["ToastType"] = "success";
        return RedirectToAction(nameof(Index));
    }

    // ─── Edit ─────────────────────────────────────────────────────────────

    [HttpGet]
    [HasPermission("User", "Update")]
    public async Task<IActionResult> Edit(string id)
    {
        var dto = await _userService.GetByIdAsync(id);
        if (dto is null)
            return RedirectToAction(nameof(Index));

        var vm = new EditUserViewModel
        {
            Id             = dto.Id,
            FullName       = dto.FullName,
            Email          = dto.Email,
            IsActive       = dto.IsActive,
            SelectedRoles  = dto.Roles.ToList(),
            AvailableRoles = (await _roleService.GetAllAsync()).ToList()
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("User", "Update")]
    public async Task<IActionResult> Edit(string id, EditUserViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.AvailableRoles = (await _roleService.GetAllAsync()).ToList();
            return View(vm);
        }

        var dto = new UpdateUserDto(
            vm.FullName,
            vm.Email,
            vm.IsActive,
            vm.SelectedRoles.AsReadOnly());

        var result = await _userService.UpdateAsync(id, dto);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            vm.AvailableRoles = (await _roleService.GetAllAsync()).ToList();
            return View(vm);
        }

        TempData["ToastMessage"] = _localizer["Users_Toast_Updated"];
        TempData["ToastType"] = "success";
        return RedirectToAction(nameof(Index));
    }

    // ─── ToggleActive ─────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("User", "Update")]
    public async Task<IActionResult> ToggleActive(string id, bool isActive)
    {
        var result = await _userService.SetActiveAsync(id, isActive);
        if (result.Succeeded)
            return Json(new { success = true });

        var error = result.Errors.FirstOrDefault()?.Description ?? _localizer["Users_Error_UpdateStatusFailed"];
        return Json(new { success = false, error });
    }

    // ─── Delete ───────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("User", "Delete")]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _userService.DeleteAsync(id);

        if (result.Succeeded)
            return Json(new { success = true });

        return Json(new
        {
            success = false,
            message = result.Errors.FirstOrDefault()?.Description ?? _localizer["Users_Error_DeleteFailed"]
        });
    }
}
