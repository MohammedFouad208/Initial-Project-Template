# Implementation Plan: Role-Permission Management

**Branch**: `008-role-permission-management` | **Date**: April 12, 2026 | **Spec**: [spec.md](spec.md)  
**Input**: Feature specification from `specs/008-role-permission-management/spec.md`

## Summary

Build a permission matrix UI where Super Admins assign which objects and functions a role may access. The matrix rows and columns are driven exclusively by `permissions.json` (no hardcoding). Saves perform a full batch replacement of all permissions for the role. The feature is **pure Web-layer work** — all required service, repository, and infrastructure code already exists from Phases 4 and 7. Three new files are needed: a controller, a view model, and a Razor view.

## Technical Context

**Language/Version**: C# 12 / .NET 8  
**Primary Dependencies**: ASP.NET Core MVC 8, ASP.NET Core Identity (built-in), Entity Framework Core 8, Bootstrap 5.3, Font Awesome 6.7+  
**Storage**: SQL Server 2019+ — `RolePermissions` table (already migrated)  
**Testing**: Manual end-to-end via browser (no automated test project in this solution)  
**Target Platform**: Web server (IIS / Kestrel)  
**Project Type**: ASP.NET Core MVC web application  
**Performance Goals**: Matrix page load < 500 ms; Save < 500 ms (small data set: ≤ 20 objects × 10 functions)  
**Constraints**: Design system compliance (CSS logical properties, design tokens only); RTL support; CSRF on POST; `[HasPermission]` gate  
**Scale/Scope**: < 100 roles; ≤ 50 permission matrix cells per role

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|---|---|---|
| I. Clean Architecture | ✅ PASS | Controller in Web; logic in Application (PermissionService); Repository in Infrastructure — no layer skip |
| II. Dependency Inversion | ✅ PASS | Controller injects `IPermissionService`, `IPermissionProvider`, `IRoleService` — all Application-layer abstractions |
| III. Thin Controllers | ✅ PASS | Controller maps route → service call → ViewModel → View; no business logic inline |
| IV. Repository + Unit of Work | ✅ PASS | `PermissionRepository.DeleteByRoleIdAsync` + `AddRangeAsync` already wrap all DB operations |
| V. JSON-Driven Permissions | ✅ PASS | Matrix rows/columns come from `IPermissionProvider.GetAll()` which reads `permissions.json` |
| VI. Generic DataTable | N/A | This feature uses a custom matrix, not a DataTable — exception justified by feature type |
| VII. Permission-Aware UI | ✅ PASS | `[HasPermission("Role", "AssignPermissions")]` gates both GET and POST actions |
| VIII. Design System First | ✅ PASS | `.card`, `.form-check-input`, `.btn-primary`, `.btn-loading`, logical properties; no hardcoded colors |

**No violations. No Complexity Tracking section needed.**

**Post-design re-check**: ✅ All principles still pass after Phase 1 design.

## Project Structure

### Documentation (this feature)

```text
specs/008-role-permission-management/
├── plan.md              ← This file
├── research.md          ← Phase 0 output
├── data-model.md        ← Phase 1 output
├── quickstart.md        ← Phase 1 output
├── contracts/
│   └── role-permissions-api.md   ← Phase 1 output
└── tasks.md             ← Phase 2 output (created by /speckit.tasks)
```

### Source Code (files to CREATE)

```text
AdminTemplate.Web/
├── Controllers/
│   └── RolePermissionsController.cs          ← NEW
├── ViewModels/
│   └── RolePermissions/
│       └── RolePermissionsViewModel.cs       ← NEW
└── Views/
    └── RolePermissions/
        └── Index.cshtml                      ← NEW
```

### Source Code (files already in place — NO changes needed)

```text
AdminTemplate.Application/
├── Interfaces/
│   ├── IPermissionService.cs                 ✅ GetRolePermissionsAsync, SaveRolePermissionsAsync
│   └── IRoleService.cs                       ✅ GetByIdAsync
├── Services/
│   └── PermissionService.cs                  ✅ All methods implemented
├── DTOs/
│   ├── PermissionDto.cs                      ✅ record(ObjectName, FunctionName)
│   └── PermissionObjectDto.cs                ✅ record(Name, DisplayName, Functions)
└── Providers/
    └── IPermissionProvider.cs                ✅ GetAll()

AdminTemplate.Domain/
├── Entities/RolePermission.cs                ✅ Entity exists + migrated
└── Interfaces/IPermissionRepository.cs       ✅ DeleteByRoleIdAsync, AddRangeAsync

AdminTemplate.Infrastructure/
├── Repositories/PermissionRepository.cs      ✅ Full implementation
├── Providers/JsonPermissionProvider.cs       ✅ Reads /Config/permissions.json at startup
└── Extensions/InfrastructureServiceExtensions.cs  ✅ All services registered in DI

AdminTemplate.Web/
├── Filters/HasPermissionAttribute.cs         ✅ IAsyncAuthorizationFilter
├── Controllers/RolesController.cs            ✅ Has "Manage Permissions" link in Index view
└── Views/Roles/Index.cshtml                  ✅ Link to /RolePermissions/{id} already present
```

**Structure Decision**: Pure Web-layer addition to the existing Clean Architecture solution. No new projects, no new service interfaces, no migrations.

---

## Implementation Details

### RolePermissionsViewModel

```
AdminTemplate.Web/ViewModels/RolePermissions/RolePermissionsViewModel.cs

Properties:
  string RoleId                               — bound to hidden form field
  string RoleName                             — for page header
  IReadOnlyList<PermissionObjectDto> AllObjects — from IPermissionProvider.GetAll()
  HashSet<string> AssignedKeys               — "ObjectName|FunctionName" strings from DB
```

Checkbox state in Razor: `AssignedKeys.Contains($"{obj.Name}|{func}")`

### RolePermissionsController

```
GET  /RolePermissions/{roleId}   → Index(string roleId)
POST /RolePermissions/Save       → Save(string roleId, List<string> selectedPermissions)
```

**GET Index flow**:
1. `Guid.TryParse(roleId)` → 404 if invalid
2. `IRoleService.GetByIdAsync(roleId)` → 404 if not found
3. `IPermissionProvider.GetAll()` → all objects/functions
4. `IPermissionService.GetRolePermissionsAsync(guid)` → current assignments
5. Build `AssignedKeys` HashSet from assignments
6. Return `View(viewModel)`

**POST Save flow**:
1. `Guid.TryParse(roleId)` → 404 if invalid
2. Parse each `selectedPermissions[i]` item by splitting on `|` → `IEnumerable<PermissionDto>`
3. `IPermissionService.SaveRolePermissionsAsync(guid, permissions)`
4. `TempData["Success"] = "Permissions saved."` → `RedirectToAction(nameof(Index), new { roleId })`
5. On exception: `TempData["Error"] = "..."` → `RedirectToAction(nameof(Index), new { roleId })`

### Views/RolePermissions/Index.cshtml

Layout: `_Layout.cshtml`

Structure:
```
.page-header
  h1: "Manage Permissions: {RoleName}"
  breadcrumb: Home → Roles → Manage Permissions

[TempData["Success"]] → .alert.alert-success (border-inline-start: var(--success))
[TempData["Error"]]   → .alert.alert-danger  (border-inline-start: var(--danger))

<form method="post" asp-action="Save">
  __AntiForgeryToken (hidden)
  <input type="hidden" name="roleId" value="@Model.RoleId" />

  .card .permission-matrix-card
    .card-body table-responsive
      table.table.table-bordered.permission-matrix
        thead:
          th[sticky] "Object / Function"
          th[per function] — column header with toggle checkbox
        tbody:
          tr[per PermissionObject]
            td[sticky, object DisplayName]
            [Check All] .btn.btn-outline-secondary.btn-sm
            td[per function]
              .form-check .form-check-input
              name="selectedPermissions" value="@obj.Name|@func"
              checked if AssignedKeys.Contains(key)

  .mt-3
    .btn.btn-primary [type=submit] "Save Permissions"   ← gets .btn-loading from site.js
    .btn.btn-outline-secondary.ms-2 [back to Roles]
</form>
```

CSS for sticky column (in view `<style>` block or `.permission-matrix` in site.css):
```css
.permission-matrix td:first-child,
.permission-matrix th:first-child {
    position: sticky;
    inset-inline-start: 0;
    background: var(--bg-card);
    z-index: 1;
}
```

JS in `@section Scripts`:
- Row "Check All" button: toggles all checkboxes with matching `data-object` to `checked=true`
- Row "Uncheck All" button: toggles all checkboxes with matching `data-object` to `checked=false`
- Column header checkbox: on change, sets all `.col-{funcName}` checkboxes to same state
