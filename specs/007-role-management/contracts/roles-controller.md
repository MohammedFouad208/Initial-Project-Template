# Contract: RolesController

**Branch**: `007-role-management`  
**Date**: 2026-04-12  
**File**: `AdminTemplate.Web/Controllers/RolesController.cs`  
**Decorated with**: `[Authorize]` on class; per-action `[HasPermission]` attributes

This document defines every public-facing action exposed by `RolesController` — its URL, HTTP method, required permission, inputs, and outputs.

---

## Actions

### GET /Roles

| Property | Value |
|---|---|
| Route | `GET /Roles` or `GET /Roles/Index` |
| Permission | `[HasPermission("Role", "Browse")]` |
| Returns | `Views/Roles/Index.cshtml` |

**Purpose**: Render the Roles list page. Permission flags for Create, Update, Delete, and AssignPermissions are resolved here and passed to `AppDataTable.init` via inline JSON in the view.

**Controller logic**:
1. Resolve `canCreate`, `canUpdate`, `canDelete`, `canManagePermissions` using `IPermissionService.UserHasPermissionAsync`.
2. Pass them to the view via `ViewBag` (consistent with Phase 6 Users pattern).

**View receives** (via `ViewBag`):
```
ViewBag.CanCreate            : bool
ViewBag.CanUpdate            : bool
ViewBag.CanDelete            : bool
ViewBag.CanManagePermissions : bool
```
*(Serialized to JSON inline in the Razor view for `AppDataTable.init`.)*

---

### GET /Roles/GetData

| Property | Value |
|---|---|
| Route | `GET /Roles/GetData` |
| Permission | `[HasPermission("Role", "Browse")]` |
| Input | `DataTableRequest` (query string — `[FromQuery]`) |
| Returns | `JSON` — `DataTableResponse<RoleDto>` |

**Purpose**: Server-side DataTables endpoint. Called by `AppDataTable` JS on every page load, search, sort, and page change.

**Input model** (`DataTableRequest` — existing from Phase 5/6):

| Field | Type | Notes |
|---|---|---|
| `Draw` | `int` | Echo back to DataTables |
| `Start` | `int` | Row offset (0-based) |
| `Length` | `int` | Page size |
| `Search` | `string?` | Free-text search value — applied to Name and Description |
| `SortColumn` | `int` | Column index (0 = Name, 1 = Description, 2 = UserCount*, 3 = PermissionCount*, 4 = CreatedAt) |
| `SortDirection` | `string` | `"asc"` or `"desc"` |

*UserCount (index 2) and PermissionCount (index 3) are `orderable: false` in the DataTable config — these column index values will not be sent for those columns.*

**Response shape** (`DataTableResponse<RoleDto>`):

```json
{
  "draw": 2,
  "recordsTotal": 5,
  "recordsFiltered": 3,
  "data": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "name": "Admin",
      "description": "Standard administrator",
      "userCount": 4,
      "permissionCount": 12,
      "createdAt": "2026-01-10T08:00:00Z"
    }
  ]
}
```

**Search scope**: `Name` and `Description` fields (case-insensitive `Contains`).

---

### GET /Roles/Create

| Property | Value |
|---|---|
| Route | `GET /Roles/Create` |
| Permission | `[HasPermission("Role", "Create")]` |
| Returns | `Views/Roles/Create.cshtml` with empty `CreateRoleViewModel` |

---

### POST /Roles/Create

| Property | Value |
|---|---|
| Route | `POST /Roles/Create` |
| Permission | `[HasPermission("Role", "Create")]` |
| CSRF | `[ValidateAntiForgeryToken]` |
| Input | `CreateRoleViewModel` (form body) |
| On success | `RedirectToAction("Index")` |
| On failure | Return `Views/Roles/Create.cshtml` with `ModelState` errors |

**Validation flow**:
1. Check `ModelState.IsValid` — return view with errors if not.
2. Map ViewModel → `CreateRoleDto` (trim `Name`).
3. Call `IRoleService.CreateAsync(dto)`.
4. If `IdentityResult.Succeeded` → redirect.
5. If failed → add each `IdentityError` to `ModelState["Name"]` and return view.

---

### GET /Roles/Edit/{id}

| Property | Value |
|---|---|
| Route | `GET /Roles/Edit/{id}` |
| Permission | `[HasPermission("Role", "Update")]` |
| Input | `id` (route, GUID string) |
| On success | `Views/Roles/Edit.cshtml` with `EditRoleViewModel` pre-filled |
| On not found | `RedirectToAction("Index")` (or 404) |

---

### POST /Roles/Edit/{id}

| Property | Value |
|---|---|
| Route | `POST /Roles/Edit/{id}` |
| Permission | `[HasPermission("Role", "Update")]` |
| CSRF | `[ValidateAntiForgeryToken]` |
| Input | `EditRoleViewModel` (form body) + `id` (route) |
| On success | `RedirectToAction("Index")` |
| On failure | Return `Views/Roles/Edit.cshtml` with `ModelState` errors |

**Validation flow** mirrors POST /Roles/Create. Maps to `UpdateRoleDto`.

---

### POST /Roles/Delete/{id}

| Property | Value |
|---|---|
| Route | `POST /Roles/Delete/{id}` |
| Permission | `[HasPermission("Role", "Delete")]` |
| CSRF | Anti-forgery token sent by `datatable.js` header injection (Phase 5) |
| Input | `id` (route, GUID string) |
| On success | `JSON { "success": true }` |
| On guard failure | `JSON { "success": false, "message": "..." }` |

**Guard flow**:
1. Call `IRoleService.DeleteAsync(id)`.
2. Service checks SuperAdmin guard → returns `IdentityResult.Failed` with `ProtectedRole` code.
3. Service checks `HasUsersAsync` → returns `IdentityResult.Failed` with `RoleHasUsers` code.
4. If either failure → controller returns `Json(new { success = false, message = error.Description })`.
5. On success → `Json(new { success = true })`.

**Error messages** (as displayed in the delete modal):
- SuperAdmin guard: *"The SuperAdmin role is protected and cannot be deleted."*
- Has users guard: *"This role cannot be deleted because it has assigned users. Remove all user assignments first."*
