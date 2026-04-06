# Quickstart: Permission Engine

**Feature Branch**: `004-permission-engine`  
**Date**: 2026-04-06  
**Prerequisites**: Phase 1 (Foundation), Phase 2 (Auth Pages), and Phase 3 (Admin Layout) complete and running.

---

## What This Phase Delivers

After implementing Phase 4 you will have:

- A fully working `PermissionService` — all three methods implemented.
- A `[HasPermission("Object", "Function")]` attribute that can be placed on any controller action or class to enforce role-based access.
- Authenticated users without the required permission see a styled "Access Denied" page (HTTP 403) using the admin layout.
- Unauthenticated users are redirected to the login page (not shown a 403).
- A global `@inject IPermissionService PermissionService` available in all Razor views for conditional rendering.
- SuperAdmin users bypass all permission checks automatically.
- No new database migration required.

---

## Files to Create / Modify

| Action | Path |
|--------|------|
| MODIFY | `AdminTemplate.Domain/Interfaces/IUserRepository.cs` — add `GetRoleNamesAsync` |
| MODIFY | `AdminTemplate.Domain/Interfaces/IPermissionRepository.cs` — add `UserHasPermissionAsync` |
| MODIFY | `AdminTemplate.Infrastructure/Repositories/UserRepository.cs` — implement `GetRoleNamesAsync` |
| MODIFY | `AdminTemplate.Infrastructure/Repositories/PermissionRepository.cs` — implement `UserHasPermissionAsync` |
| MODIFY | `AdminTemplate.Application/Services/PermissionService.cs` — implement all 3 methods; add SuperAdmin constant |
| CREATE | `AdminTemplate.Application/Services/PermissionServiceExtensions.cs` — `CurrentUserHasPermissionAsync` extension |
| CREATE | `AdminTemplate.Web/Filters/HasPermissionAttribute.cs` — `IAsyncAuthorizationFilter` |
| CREATE | `AdminTemplate.Web/Views/Shared/Error403.cshtml` — styled 403 view |
| MODIFY | `AdminTemplate.Web/Views/_ViewImports.cshtml` — add `@inject IPermissionService PermissionService` |

---

## Implementation Steps (for `/speckit.implement`)

### Step 1 — Extend domain interfaces

**`AdminTemplate.Domain/Interfaces/IUserRepository.cs`** — append one method:
```csharp
Task<IReadOnlyList<string>> GetRoleNamesAsync(Guid userId);
```

**`AdminTemplate.Domain/Interfaces/IPermissionRepository.cs`** — append one method:
```csharp
Task<bool> UserHasPermissionAsync(Guid userId, string objectName, string functionName);
```

---

### Step 2 — Implement new repository methods

**`AdminTemplate.Infrastructure/Repositories/UserRepository.cs`** — add method:
```csharp
public async Task<IReadOnlyList<string>> GetRoleNamesAsync(Guid userId)
{
    var user = await _userManager.FindByIdAsync(userId.ToString());
    if (user is null) return [];
    var roles = await _userManager.GetRolesAsync(user);
    return roles.ToList().AsReadOnly();
}
```

**`AdminTemplate.Infrastructure/Repositories/PermissionRepository.cs`** — add method using EF Core LINQ:
```csharp
public async Task<bool> UserHasPermissionAsync(Guid userId, string objectName, string functionName)
{
    return await _context.UserRoles
        .Join(_context.RolePermissions,
            ur => ur.RoleId,
            rp => rp.RoleId,
            (ur, rp) => new { ur.UserId, rp.ObjectName, rp.FunctionName })
        .AnyAsync(x =>
            x.UserId == userId &&
            x.ObjectName == objectName &&
            x.FunctionName == functionName);
}
```

Note: `_context.UserRoles` is the `AspNetUserRoles` join table exposed by `IdentityDbContext`.

---

### Step 3 — Implement PermissionService

Replace the three `throw new NotImplementedException()` bodies in `AdminTemplate.Application/Services/PermissionService.cs`.

The class now requires two constructor-injected dependencies:
- `IUserRepository _userRepository`
- `IPermissionRepository _permissionRepository`

```csharp
private const string SuperAdminRoleName = "SuperAdmin";

public async Task<IReadOnlyList<PermissionDto>> GetRolePermissionsAsync(Guid roleId)
{
    var entities = await _permissionRepository.GetByRoleIdAsync(roleId);
    return entities.Select(e => new PermissionDto(e.ObjectName, e.FunctionName))
                   .ToList()
                   .AsReadOnly();
}

public async Task<bool> UserHasPermissionAsync(Guid userId, string objectName, string functionName)
{
    var roleNames = await _userRepository.GetRoleNamesAsync(userId);
    if (roleNames.Contains(SuperAdminRoleName, StringComparer.OrdinalIgnoreCase))
        return true;

    return await _permissionRepository.UserHasPermissionAsync(userId, objectName, functionName);
}

public async Task SaveRolePermissionsAsync(Guid roleId, IEnumerable<PermissionDto> permissions)
{
    await _permissionRepository.DeleteByRoleIdAsync(roleId);

    var entities = permissions.Select(p => new RolePermission
    {
        RoleId = roleId,
        ObjectName = p.ObjectName,
        FunctionName = p.FunctionName
    });

    await _permissionRepository.AddRangeAsync(entities);
}
```

---

### Step 4 — Create `PermissionServiceExtensions`

Create `AdminTemplate.Application/Services/PermissionServiceExtensions.cs`:

```csharp
using System.Security.Claims;
using AdminTemplate.Application.Interfaces;

namespace AdminTemplate.Application.Services;

public static class PermissionServiceExtensions
{
    public static async Task<bool> CurrentUserHasPermissionAsync(
        this IPermissionService service,
        ClaimsPrincipal user,
        string objectName,
        string functionName)
    {
        if (user.Identity?.IsAuthenticated != true)
            return false;

        var userIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
            return false;

        return await service.UserHasPermissionAsync(userId, objectName, functionName);
    }
}
```

---

### Step 5 — Create `HasPermissionAttribute`

Create `AdminTemplate.Web/Filters/HasPermissionAttribute.cs`:

```csharp
using System.Security.Claims;
using AdminTemplate.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace AdminTemplate.Web.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class HasPermissionAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string _objectName;
    private readonly string _functionName;

    public HasPermissionAttribute(string objectName, string functionName)
    {
        _objectName = objectName;
        _functionName = functionName;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        if (user.Identity?.IsAuthenticated != true)
        {
            context.Result = new ChallengeResult();
            return;
        }

        var userIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            context.Result = new ForbidResult();
            return;
        }

        var permissionService = context.HttpContext.RequestServices
            .GetRequiredService<IPermissionService>();

        var hasPermission = await permissionService.UserHasPermissionAsync(
            userId, _objectName, _functionName);

        if (!hasPermission)
        {
            context.Result = new ViewResult
            {
                ViewName = "~/Views/Shared/Error403.cshtml",
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
```

---

### Step 6 — Create `Error403.cshtml`

Create `AdminTemplate.Web/Views/Shared/Error403.cshtml`:

```html
@{
    ViewData["Title"] = "Access Denied";
    Layout = "_Layout";
}

<div class="container-fluid py-4">
    <div class="row justify-content-center">
        <div class="col-md-6 text-center">
            <div class="card shadow-sm border-0">
                <div class="card-body py-5">
                    <i class="fa fa-lock fa-4x text-muted mb-3"></i>
                    <h1 class="display-6 fw-bold">403 — Access Denied</h1>
                    <p class="text-muted mt-2">
                        You do not have permission to access this page.<br />
                        Contact your administrator if you believe this is an error.
                    </p>
                    <a asp-controller="Dashboard" asp-action="Index"
                       class="btn btn-primary mt-3">
                        <i class="fa fa-arrow-left me-1"></i> Back to Dashboard
                    </a>
                </div>
            </div>
        </div>
    </div>
</div>
```

---

### Step 7 — Register permission service injection in views

Locate `AdminTemplate.Web/Views/_ViewImports.cshtml` and add the following line:

```razor
@inject AdminTemplate.Application.Interfaces.IPermissionService PermissionService
```

Also add the using for the extension method:

```razor
@using AdminTemplate.Application.Services
```

This makes `PermissionService` and `CurrentUserHasPermissionAsync` available in all views.

---

### Step 8 — Verify wiring

1. Build the solution — expect 0 errors.
2. Log in as a non-SuperAdmin user with no permissions assigned.
3. Navigate directly to any future permission-protected URL (e.g., `/Users/Create`).
4. Decorate that action with `[HasPermission("User", "Create")]`.
5. Confirm the 403 page is displayed with the admin layout.
6. Log in as the SuperAdmin seed user — confirm the same URL is accessible.

---

## No Migration Required

The `RolePermissions` table was created in Phase 1. No `Add-Migration` or `Update-Database` is needed.
