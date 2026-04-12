# Quickstart: PHASE 6 — User Management

**Branch**: `006-user-management`  
**Date**: 2026-04-11  
**Prerequisite phases**: 1 (Foundation), 2 (Auth), 3 (Admin Layout), 4 (Permission Engine), 5 (Generic DataTable)

This guide covers everything needed to implement the User Management module from a clean checkout of the `006-user-management` branch.

---

## Prerequisites

Before starting, confirm these artifacts from earlier phases are in place:

- [ ] `AdminTemplate.Web/Filters/HasPermissionAttribute.cs` — custom auth filter (Phase 4)
- [ ] `AdminTemplate.Web/wwwroot/js/datatable.js` — `AppDataTable.init` JS object (Phase 5)
- [ ] `AdminTemplate.Web/Views/Shared/Error403.cshtml` — custom 403 page (Phase 4)
- [ ] `AdminTemplate.Web/Views/Shared/_Layout.cshtml` — admin layout with sidebar and topnav (Phase 3)
- [ ] `AdminTemplate.Web/wwwroot/css/site.css` — full design token layer (Phase 3)
- [ ] `AdminTemplate.Infrastructure/Extensions/InfrastructureServiceExtensions.cs` — `IUserService` already registered

**No new NuGet packages are needed for this phase.**  
**No new EF Core migration is needed** — `AspNetUsers` and `AspNetUserRoles` exist from Phase 1.

---

## Implementation Order

Work through the files in this order to avoid compilation errors between steps:

```
Step 1:  AdminTemplate.Application/DTOs/CreateUserDto.cs           — add IsActive field
Step 2:  AdminTemplate.Infrastructure/Repositories/UserRepository.cs — complete stubs
Step 3:  AdminTemplate.Application/Services/UserService.cs          — complete stubs
Step 4:  AdminTemplate.Web/Models/DataTableRequest.cs               — new (if not from Phase 5)
Step 5:  AdminTemplate.Web/Models/DataTableResponse.cs              — new (if not from Phase 5)
Step 6:  AdminTemplate.Web/ViewModels/Users/CreateUserViewModel.cs  — new
Step 7:  AdminTemplate.Web/ViewModels/Users/EditUserViewModel.cs    — new
Step 8:  AdminTemplate.Web/Controllers/UsersController.cs           — new
Step 9:  AdminTemplate.Web/Views/Users/Index.cshtml                 — new
Step 10: AdminTemplate.Web/Views/Users/Create.cshtml                — new
Step 11: AdminTemplate.Web/Views/Users/Edit.cshtml                  — new
Step 12: AdminTemplate.Web/Views/Shared/_Sidebar.cshtml             — add Users nav link
```

---

## Step-by-Step Implementation Notes

### Step 1 — Update CreateUserDto

`CreateUserDto` currently has no `IsActive` field. Add it as a positional parameter with default `true`:

```csharp
public record CreateUserDto(
    string FullName,
    string Email,
    string Password,
    IReadOnlyList<string> Roles,
    bool IsActive = true);   // ADD
```

---

### Step 2 — Complete UserRepository

Complete the following stubs in `AdminTemplate.Infrastructure/Repositories/UserRepository.cs`:

**`GetByIdAsync`**:
```csharp
public async Task<ApplicationUser?> GetByIdAsync(Guid id)
    => await _userManager.FindByIdAsync(id.ToString());
```

**`GetPagedAsync`**:
```csharp
public async Task<(IReadOnlyList<ApplicationUser> Items, int TotalCount)> GetPagedAsync(
    int pageIndex, int pageSize, string? searchTerm = null)
{
    var query = _userManager.Users.AsQueryable();
    if (!string.IsNullOrWhiteSpace(searchTerm))
        query = query.Where(u => u.FullName.Contains(searchTerm)
                              || u.Email!.Contains(searchTerm));
    var total = await query.CountAsync();
    var items = await query
        .OrderBy(u => u.FullName)
        .Skip(pageIndex * pageSize)
        .Take(pageSize)
        .ToListAsync();
    return (items, total);
}
```

**`UpdateAsync`**:
```csharp
public async Task UpdateAsync(ApplicationUser user)
    => await _userManager.UpdateAsync(user);
```

**`DeleteAsync`** *(soft delete via IsActive; kept for interface compliance)*:
```csharp
public async Task DeleteAsync(Guid id)
{
    var user = await GetByIdAsync(id);
    if (user is null) return;
    user.IsActive = false;
    await _userManager.UpdateAsync(user);
}
```

**`GetAllAsync`**:
```csharp
public async Task<IReadOnlyList<ApplicationUser>> GetAllAsync()
    => await _userManager.Users.OrderBy(u => u.FullName).ToListAsync();
```

---

### Step 3 — Complete UserService

Complete the following stubs in `AdminTemplate.Application/Services/UserService.cs`:

**`GetByIdAsync`**:
```csharp
public async Task<UserDto?> GetByIdAsync(string id)
{
    var user = await _userManager.FindByIdAsync(id);
    if (user is null) return null;
    var roles = await _userManager.GetRolesAsync(user);
    return new UserDto(user.Id.ToString(), user.FullName, user.Email!,
                       user.IsActive, roles.ToList().AsReadOnly(), user.CreatedAt);
}
```

**`GetPagedAsync`**:
```csharp
public async Task<(IReadOnlyList<UserDto> Items, int TotalCount)> GetPagedAsync(
    int pageIndex, int pageSize, string? searchTerm = null)
{
    var query = _userManager.Users.AsQueryable();
    if (!string.IsNullOrWhiteSpace(searchTerm))
        query = query.Where(u => u.FullName.Contains(searchTerm)
                              || u.Email!.Contains(searchTerm));
    var total = await query.CountAsync();
    var users = await query
        .OrderBy(u => u.FullName)
        .Skip(pageIndex * pageSize)
        .Take(pageSize)
        .ToListAsync();
    var dtos = new List<UserDto>();
    foreach (var u in users)
    {
        var roles = await _userManager.GetRolesAsync(u);
        dtos.Add(new UserDto(u.Id.ToString(), u.FullName, u.Email!,
                             u.IsActive, roles.ToList().AsReadOnly(), u.CreatedAt));
    }
    return (dtos, total);
}
```

**`CreateAsync`**:
```csharp
public async Task<IdentityResult> CreateAsync(CreateUserDto dto)
{
    var user = new ApplicationUser
    {
        FullName = dto.FullName,
        Email = dto.Email,
        UserName = dto.Email,
        IsActive = dto.IsActive
    };
    var result = await _userManager.CreateAsync(user, dto.Password);
    if (!result.Succeeded) return result;
    if (dto.Roles.Count > 0)
        await _userManager.AddToRolesAsync(user, dto.Roles);
    return result;
}
```

**`UpdateAsync`**:
```csharp
public async Task<IdentityResult> UpdateAsync(string id, UpdateUserDto dto)
{
    var user = await _userManager.FindByIdAsync(id);
    if (user is null)
        return IdentityResult.Failed(new IdentityError { Description = "User not found." });
    user.FullName = dto.FullName;
    user.Email = dto.Email;
    user.UserName = dto.Email;
    user.NormalizedEmail = dto.Email.ToUpperInvariant();
    user.NormalizedUserName = dto.Email.ToUpperInvariant();
    user.IsActive = dto.IsActive;
    var result = await _userManager.UpdateAsync(user);
    if (!result.Succeeded) return result;
    var currentRoles = await _userManager.GetRolesAsync(user);
    await _userManager.RemoveFromRolesAsync(user, currentRoles);
    if (dto.Roles.Count > 0)
        await _userManager.AddToRolesAsync(user, dto.Roles);
    return IdentityResult.Success;
}
```

**`SetActiveAsync`**:
```csharp
public async Task<IdentityResult> SetActiveAsync(string id, bool isActive)
{
    var user = await _userManager.FindByIdAsync(id);
    if (user is null)
        return IdentityResult.Failed(new IdentityError { Description = "User not found." });
    user.IsActive = isActive;
    return await _userManager.UpdateAsync(user);
}
```

---

### Steps 4 + 5 — DataTableRequest / DataTableResponse

If these were not created in Phase 5, create them in `AdminTemplate.Web/Models/`:

```csharp
// DataTableRequest.cs
public class DataTableRequest
{
    public int Draw { get; set; }
    public int Start { get; set; }
    public int Length { get; set; }
    public string? Search { get; set; }         // mapped from search[value]
    public int SortColumn { get; set; }
    public string SortDirection { get; set; } = "asc";
}

// DataTableResponse.cs
public class DataTableResponse<T>
{
    public int Draw { get; set; }
    public int RecordsTotal { get; set; }
    public int RecordsFiltered { get; set; }
    public IEnumerable<T> Data { get; set; } = Enumerable.Empty<T>();
}
```

---

### Steps 6 + 7 — ViewModels

Create `AdminTemplate.Web/ViewModels/Users/CreateUserViewModel.cs`:

```csharp
public class CreateUserViewModel
{
    [Required] [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required] [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required] [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required] [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public List<string> SelectedRoles { get; set; } = new();
    public List<RoleDto> AvailableRoles { get; set; } = new();  // populated by controller, not bound on POST
}
```

Create `AdminTemplate.Web/ViewModels/Users/EditUserViewModel.cs`:

```csharp
public class EditUserViewModel
{
    [Required]
    public string Id { get; set; } = string.Empty;

    [Required] [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required] [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public bool IsActive { get; set; }
    public List<string> SelectedRoles { get; set; } = new();
    public List<RoleDto> AvailableRoles { get; set; } = new();  // populated by controller, not bound on POST
}
```

`RoleDto` = `AdminTemplate.Application.DTOs.RoleDto` (already exists from Phase 5).

---

### Step 8 — UsersController

Key patterns (full action signatures in [contracts/users-controller.md](contracts/users-controller.md)):

- Decorate class with `[Authorize]`.
- Constructor-inject: `IUserService`, `IRoleService`, `IPermissionService`.
- For `GetData`: bind `[FromQuery] DataTableRequest request`; delegate paging to `IUserService.GetPagedAsync`; return `Json(DataTableResponse<UserDto>)`.
- Add Identity errors to ModelState on POST failures:
  ```csharp
  foreach (var error in result.Errors)
      ModelState.AddModelError(string.Empty, error.Description);
  ```
- Use `TempData["Success"]` for redirect-based notifications.
- `ToggleActive` returns `Json(new { success = true })` — no redirect.

---

### Step 9 — Views/Users/Index.cshtml

```razor
@{
    ViewData["Title"] = "Users";
    Layout = "_Layout";
}

<div class="page-header">
    <h1>Users</h1>
    <nav aria-label="breadcrumb">...</nav>
</div>

<div class="card table-card">
    <div class="card-body p-0">
        <table id="usersTable" class="table"></table>
    </div>
</div>

@section Scripts {
<script>
AppDataTable.init({
    tableId:     '#usersTable',
    ajaxUrl:     '@Url.Action("GetData", "Users")',
    columns: [
        { data: 'fullName', title: 'Name' },
        { data: 'email',    title: 'Email' },
        { data: 'roles',    title: 'Roles', orderable: false },
        { data: 'isActive', title: 'Status' }
    ],
    permissions: {
        canCreate: @Json.Serialize(ViewBag.CanCreate),
        canUpdate: @Json.Serialize(ViewBag.CanUpdate),
        canDelete: false
    },
    createUrl:  '@Url.Action("Create", "Users")',
    editUrl:    '@Url.Action("Edit",   "Users")',
    objectName: 'User'
});
</script>
}
```

> The Activate/Deactivate toggle is an additional action column rendered by `AppDataTable` or a custom column renderer — it posts to `/Users/ToggleActive` via fetch.

---

### Steps 10 + 11 — Create.cshtml / Edit.cshtml

Common patterns for both forms:

- `.card > .card-header + .card-body` wrapper.
- Each input field in `.field-icon-wrap` with `<i class="field-icon fa-solid ..."></i>`.
- Validation summary inside `.alert.alert-danger` with `style="border-inline-start: 4px solid var(--danger)"`.
- Submit in `.d-flex.justify-content-end` using `.btn.btn-primary.btn-lg`.
- `<partial name="_ValidationScriptsPartial" />` in `@section Scripts`.

**Edit.cshtml only**: include `<input asp-for="Id" type="hidden" />`.  
**Create.cshtml only**: include Password + ConfirmPassword fields.

---

### Step 12 — Sidebar Nav Link

Add to `_Sidebar.cshtml` in the management section:

```html
<li class="nav-item">
    <a class="nav-link @(ViewContext.RouteData.Values["controller"]?.ToString() == "Users" ? "active" : "")"
       asp-controller="Users" asp-action="Index">
        <i class="fa-solid fa-users"></i>
        <span class="nav-label">Users</span>
    </a>
</li>
```

---

## Verification Checklist

After completing all 12 steps, verify:

- [ ] `/Users` loads the DataTable with correct columns (Name, Email, Roles, Status, Actions)
- [ ] Server-side search filters on FullName and Email
- [ ] Column sort works server-side
- [ ] Create form saves new user with correct roles and redirects with success message
- [ ] Duplicate email shows validation error without data loss
- [ ] Password complexity error displays on Create
- [ ] Edit form pre-populates existing data; no password field visible
- [ ] Edit save updates name, email, roles, and active state
- [ ] ToggleActive AJAX updates badge without page reload
- [ ] Deactivated user cannot log in
- [ ] User without "User → Browse" gets 403 on `/Users`
- [ ] User without "User → Create" sees no Create button and gets 403 on direct URL to `/Users/Create`
- [ ] All POST actions protected with `[ValidateAntiForgeryToken]`
- [ ] No hardcoded hex colors or `margin-left`/`padding-right` in new views
- [ ] RTL toggle mirrors Users pages correctly
