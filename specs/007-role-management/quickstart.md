# Quickstart: PHASE 7 — Role Management

**Branch**: `007-role-management`  
**Date**: 2026-04-12  
**Prerequisite phases**: 1 (Foundation), 2 (Auth), 3 (Admin Layout), 4 (Permission Engine), 5 (Generic DataTable), 6 (User Management)

This guide covers everything needed to implement the Role Management module from a clean checkout of the `007-role-management` branch.

---

## Prerequisites

Before starting, confirm these artifacts from earlier phases are in place:

- [ ] `AdminTemplate.Web/Filters/HasPermissionAttribute.cs` — custom auth filter (Phase 4)
- [ ] `AdminTemplate.Web/wwwroot/js/datatable.js` — `AppDataTable.init` JS object (Phase 5)
- [ ] `AdminTemplate.Web/Views/Shared/_DeleteConfirmModal.cshtml` — delete confirmation modal (Phase 5)
- [ ] `AdminTemplate.Web/Views/Shared/Error403.cshtml` — custom 403 page (Phase 4)
- [ ] `AdminTemplate.Web/Views/Shared/_Layout.cshtml` — admin layout with sidebar and topnav (Phase 3)
- [ ] `AdminTemplate.Web/wwwroot/css/site.css` — full design token layer (Phase 3)
- [ ] `AdminTemplate.Web/Models/DataTableRequest.cs` and `DataTableResponse.cs` — from Phase 5/6
- [ ] `AdminTemplate.Infrastructure/Extensions/InfrastructureServiceExtensions.cs` — `IRoleService` already registered

**No new NuGet packages are needed for this phase.**  
**No new EF Core migration is needed** — `AspNetRoles`, `AspNetUserRoles`, and `RolePermissions` tables exist from Phase 1/4.

---

## Implementation Order

Work through the files in this order to avoid compilation errors between steps:

```
Step 1:  AdminTemplate.Domain/Interfaces/IPermissionRepository.cs     — add GetCountByRoleIdAsync
Step 2:  AdminTemplate.Domain/Interfaces/IRoleRepository.cs           — add GetUserCountAsync
Step 3:  AdminTemplate.Infrastructure/Repositories/PermissionRepository.cs — implement GetCountByRoleIdAsync
Step 4:  AdminTemplate.Infrastructure/Repositories/RoleRepository.cs  — complete all stubs + GetUserCountAsync
Step 5:  AdminTemplate.Application/Services/RoleService.cs            — complete all stubs
Step 6:  AdminTemplate.Web/ViewModels/Roles/CreateRoleViewModel.cs    — new
Step 7:  AdminTemplate.Web/ViewModels/Roles/EditRoleViewModel.cs      — new
Step 8:  AdminTemplate.Web/Controllers/RolesController.cs             — new
Step 9:  AdminTemplate.Web/Views/Roles/Index.cshtml                   — new
Step 10: AdminTemplate.Web/Views/Roles/Create.cshtml                  — new
Step 11: AdminTemplate.Web/Views/Roles/Edit.cshtml                    — new
Step 12: AdminTemplate.Web/Views/Shared/_Sidebar.cshtml               — add Roles nav link
```

---

## Step-by-Step Implementation Notes

### Step 1 — Update IPermissionRepository

Add one method to `AdminTemplate.Domain/Interfaces/IPermissionRepository.cs`:

```csharp
Task<int> GetCountByRoleIdAsync(Guid roleId);
```

---

### Step 2 — Update IRoleRepository

Add one method to `AdminTemplate.Domain/Interfaces/IRoleRepository.cs`:

```csharp
Task<int> GetUserCountAsync(Guid roleId);
```

---

### Step 3 — Implement GetCountByRoleIdAsync in PermissionRepository

In `AdminTemplate.Infrastructure/Repositories/PermissionRepository.cs`, add:

```csharp
public async Task<int> GetCountByRoleIdAsync(Guid roleId)
    => await _context.RolePermissions.CountAsync(rp => rp.RoleId == roleId);
```

`_context` is `ApplicationDbContext` injected in the constructor.

---

### Step 4 — Complete RoleRepository

Complete all `NotImplementedException` stubs in `AdminTemplate.Infrastructure/Repositories/RoleRepository.cs` and add `GetUserCountAsync`:

**`GetByIdAsync`**:
```csharp
public async Task<ApplicationRole?> GetByIdAsync(Guid id)
    => await _roleManager.FindByIdAsync(id.ToString());
```

**`GetAllAsync`**:
```csharp
public async Task<IReadOnlyList<ApplicationRole>> GetAllAsync()
    => await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
```

**`GetPagedAsync`** (sort by Name, Description, or CreatedAt — not by computed counts):
```csharp
public async Task<(IReadOnlyList<ApplicationRole> Items, int TotalCount)> GetPagedAsync(
    int pageIndex, int pageSize, string? searchTerm = null, string sortColumn = "name", string sortDirection = "asc")
{
    var query = _roleManager.Roles.AsQueryable();
    if (!string.IsNullOrWhiteSpace(searchTerm))
        query = query.Where(r => r.Name!.Contains(searchTerm)
                              || (r.Description != null && r.Description.Contains(searchTerm)));
    query = (sortColumn.ToLower(), sortDirection.ToLower()) switch
    {
        ("description", "asc")  => query.OrderBy(r => r.Description),
        ("description", "desc") => query.OrderByDescending(r => r.Description),
        ("createdat", "asc")    => query.OrderBy(r => r.CreatedAt),
        ("createdat", "desc")   => query.OrderByDescending(r => r.CreatedAt),
        (_, "desc")             => query.OrderByDescending(r => r.Name),
        _                       => query.OrderBy(r => r.Name),
    };
    var total = await query.CountAsync();
    var items = await query.Skip(pageIndex * pageSize).Take(pageSize).ToListAsync();
    return (items, total);
}
```

**`UpdateAsync`**:
```csharp
public async Task UpdateAsync(ApplicationRole role)
    => await _roleManager.UpdateAsync(role);
```

**`DeleteAsync`**:
```csharp
public async Task DeleteAsync(Guid id)
{
    var role = await _roleManager.FindByIdAsync(id.ToString());
    if (role is null) return;
    await _roleManager.DeleteAsync(role);
}
```

**`GetUserCountAsync`** (new):
```csharp
public async Task<int> GetUserCountAsync(Guid roleId)
{
    var role = await _roleManager.FindByIdAsync(roleId.ToString());
    if (role is null || string.IsNullOrWhiteSpace(role.Name)) return 0;
    var users = await _userManager.GetUsersInRoleAsync(role.Name);
    return users.Count;
}
```

---

### Step 5 — Complete RoleService

Complete all `NotImplementedException` stubs in `AdminTemplate.Application/Services/RoleService.cs`. Inject `IPermissionRepository` alongside `RoleManager`:

```csharp
private const string SuperAdminRoleName = "SuperAdmin";

private readonly RoleManager<ApplicationRole> _roleManager;
private readonly IRoleRepository _roleRepository;
private readonly IPermissionRepository _permissionRepository;
```

**`GetByIdAsync`**:
```csharp
public async Task<RoleDto?> GetByIdAsync(string id)
{
    if (!Guid.TryParse(id, out var guid)) return null;
    var role = await _roleRepository.GetByIdAsync(guid);
    if (role is null) return null;
    var userCount = await _roleRepository.GetUserCountAsync(guid);
    var permCount = await _permissionRepository.GetCountByRoleIdAsync(guid);
    return new RoleDto(role.Id.ToString(), role.Name!, role.Description, userCount, permCount, role.CreatedAt);
}
```

**`GetAllAsync`**:
```csharp
public async Task<IReadOnlyList<RoleDto>> GetAllAsync()
{
    var roles = await _roleRepository.GetAllAsync();
    var dtos = new List<RoleDto>(roles.Count);
    foreach (var role in roles)
    {
        var userCount = await _roleRepository.GetUserCountAsync(role.Id);
        var permCount = await _permissionRepository.GetCountByRoleIdAsync(role.Id);
        dtos.Add(new RoleDto(role.Id.ToString(), role.Name!, role.Description, userCount, permCount, role.CreatedAt));
    }
    return dtos.AsReadOnly();
}
```

**`GetPagedAsync`**:
```csharp
public async Task<(IReadOnlyList<RoleDto> Items, int TotalCount)> GetPagedAsync(
    int pageIndex, int pageSize, string? searchTerm = null, string sortColumn = "name", string sortDirection = "asc")
{
    var (roles, total) = await _roleRepository.GetPagedAsync(pageIndex, pageSize, searchTerm, sortColumn, sortDirection);
    var dtos = new List<RoleDto>(roles.Count);
    foreach (var role in roles)
    {
        var userCount = await _roleRepository.GetUserCountAsync(role.Id);
        var permCount = await _permissionRepository.GetCountByRoleIdAsync(role.Id);
        dtos.Add(new RoleDto(role.Id.ToString(), role.Name!, role.Description, userCount, permCount, role.CreatedAt));
    }
    return (dtos.AsReadOnly(), total);
}
```

**`CreateAsync`**:
```csharp
public async Task<IdentityResult> CreateAsync(CreateRoleDto dto)
{
    var role = new ApplicationRole(dto.Name.Trim())
    {
        Description = dto.Description?.Trim()
    };
    return await _roleManager.CreateAsync(role);
}
```

**`UpdateAsync`**:
```csharp
public async Task<IdentityResult> UpdateAsync(string id, UpdateRoleDto dto)
{
    if (!Guid.TryParse(id, out var guid))
        return IdentityResult.Failed(new IdentityError { Code = "InvalidId", Description = "Role not found." });
    var role = await _roleRepository.GetByIdAsync(guid);
    if (role is null)
        return IdentityResult.Failed(new IdentityError { Code = "NotFound", Description = "Role not found." });
    role.Name = dto.Name.Trim();
    role.Description = dto.Description?.Trim();
    return await _roleManager.UpdateAsync(role);
}
```

**`DeleteAsync`**:
```csharp
public async Task<IdentityResult> DeleteAsync(string id)
{
    if (!Guid.TryParse(id, out var guid))
        return IdentityResult.Failed(new IdentityError { Code = "InvalidId", Description = "Role not found." });
    var role = await _roleRepository.GetByIdAsync(guid);
    if (role is null)
        return IdentityResult.Failed(new IdentityError { Code = "NotFound", Description = "Role not found." });
    // Guard 1: SuperAdmin is protected
    if (string.Equals(role.Name, SuperAdminRoleName, StringComparison.OrdinalIgnoreCase))
        return IdentityResult.Failed(new IdentityError
        {
            Code = "ProtectedRole",
            Description = "The SuperAdmin role is protected and cannot be deleted."
        });
    // Guard 2: role has assigned users
    var hasUsers = await _roleRepository.HasUsersAsync(guid);
    if (hasUsers)
        return IdentityResult.Failed(new IdentityError
        {
            Code = "RoleHasUsers",
            Description = "This role cannot be deleted because it has assigned users. Remove all user assignments first."
        });
    await _roleRepository.DeleteAsync(guid);
    return IdentityResult.Success;
}
```

> **Note**: `IRoleService.DeleteAsync` signature must change from `Task<IdentityResult>` to `Task<IdentityResult>` — already correct. `IRoleRepository.GetPagedAsync` signature must be updated to accept `sortColumn` and `sortDirection` string parameters (see Step 4 above).

---

### Step 6 — CreateRoleViewModel

`AdminTemplate.Web/ViewModels/Roles/CreateRoleViewModel.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace AdminTemplate.Web.ViewModels.Roles;

public class CreateRoleViewModel
{
    [Required(ErrorMessage = "Role name is required.")]
    [MaxLength(100, ErrorMessage = "Role name cannot exceed 100 characters.")]
    [Display(Name = "Role Name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    [Display(Name = "Description")]
    public string? Description { get; set; }
}
```

---

### Step 7 — EditRoleViewModel

`AdminTemplate.Web/ViewModels/Roles/EditRoleViewModel.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace AdminTemplate.Web.ViewModels.Roles;

public class EditRoleViewModel
{
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role name is required.")]
    [MaxLength(100, ErrorMessage = "Role name cannot exceed 100 characters.")]
    [Display(Name = "Role Name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    [Display(Name = "Description")]
    public string? Description { get; set; }
}
```

---

### Step 8 — RolesController

Key design decisions:
- Decorated with `[Authorize]` at class level.
- `GetData` maps `SortColumn` index → field name string (0→"name", 1→"description", 4→"createdAt"; indices 2 and 3 are non-orderable).
- `Delete` returns JSON, not a redirect, consistent with Phase 5/6 DataTable delete pattern.
- `TotalCountAsync` (used on Dashboard) already works through `GetTotalCountAsync()`.

```csharp
[Authorize]
public class RolesController : Controller
{
    private readonly IRoleService _roleService;
    private readonly IPermissionService _permissionService;

    // Inject both services via constructor
    
    [HasPermission("Role", "Browse")]
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var uid = Guid.Parse(userId);
        ViewBag.CanCreate = await _permissionService.UserHasPermissionAsync(uid, "Role", "Create");
        ViewBag.CanUpdate = await _permissionService.UserHasPermissionAsync(uid, "Role", "Update");
        ViewBag.CanDelete = await _permissionService.UserHasPermissionAsync(uid, "Role", "Delete");
        ViewBag.CanManagePermissions = await _permissionService.UserHasPermissionAsync(uid, "Role", "AssignPermissions");
        return View();
    }

    [HasPermission("Role", "Browse")]
    public async Task<IActionResult> GetData([FromQuery] DataTableRequest request)
    {
        string[] columnMap = ["name", "description", "userCount", "permissionCount", "createdAt"];
        var sortCol = request.SortColumn >= 0 && request.SortColumn < columnMap.Length
            ? columnMap[request.SortColumn] : "name";
        var pageIndex = request.Start / (request.Length > 0 ? request.Length : 10);
        var (items, total) = await _roleService.GetPagedAsync(pageIndex, request.Length, request.Search, sortCol, request.SortDirection);
        return Json(new DataTableResponse<RoleDto>
        {
            Draw = request.Draw,
            RecordsTotal = total,
            RecordsFiltered = total,
            Data = items.ToList()
        });
    }
    
    // Create GET/POST, Edit GET/POST, Delete POST — per contracts doc
}
```

---

### Step 9-11 — Views

All three views follow the same patterns established in Phase 6:

- **Index.cshtml**: `.page-header` + `AppDataTable.init({...})`. Actions column uses custom render function with Edit (`.btn-outline-secondary`), Delete (`.btn-outline-danger`), and Manage Permissions (`.btn-outline-primary` + `fa-key`) buttons.
- **Create.cshtml / Edit.cshtml**: `.card` wrapper, `.form-label` + `.form-control` inputs with focus ring, `.btn.btn-primary` submit, `.alert.alert-danger` with `border-inline-start: 4px solid var(--danger)` for validation errors. No `.field-icon-wrap` needed (roles have no icon-input fields in the spec).

---

### Step 12 — Sidebar Nav Link

Add a Roles entry to `AdminTemplate.Web/Views/Shared/_Sidebar.cshtml` after the Users link:

```html
<li class="nav-item @(ViewContext.RouteData.Values["controller"]?.ToString() == "Roles" ? "active" : "")
           @(canBrowseRoles ? "" : "d-none")">
    <a class="nav-link" href="/Roles">
        <i class="fa fa-shield-halved"></i>
        <span>Roles</span>
    </a>
</li>
```

`canBrowseRoles` is resolved in `_Layout.cshtml` or passed from each controller action's ViewBag, consistent with the existing sidebar permission-aware pattern.

---

## IRoleService Interface Signature Note

The existing `IRoleService.GetPagedAsync` signature is:
```csharp
Task<(IReadOnlyList<RoleDto> Items, int TotalCount)> GetPagedAsync(int pageIndex, int pageSize, string? searchTerm = null);
```

This must be updated to include sort parameters:
```csharp
Task<(IReadOnlyList<RoleDto> Items, int TotalCount)> GetPagedAsync(
    int pageIndex, int pageSize, string? searchTerm = null,
    string sortColumn = "name", string sortDirection = "asc");
```

Update `IRoleService`, `RoleService`, and `IRoleRepository` + `RoleRepository` signatures together.
