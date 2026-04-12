# Contract: UsersController

**Branch**: `006-user-management`  
**Date**: 2026-04-11  
**File**: `AdminTemplate.Web/Controllers/UsersController.cs`  
**Decorated with**: `[Authorize]` on class; per-action `[HasPermission]` attributes

This document defines every public-facing action exposed by `UsersController` — its URL, HTTP method, required permission, inputs, and outputs. It is the authoritative contract for the view layer and any JavaScript that calls these endpoints.

---

## Actions

### GET /Users

| Property | Value |
|---|---|
| Route | `GET /Users` or `GET /Users/Index` |
| Permission | `[HasPermission("User", "Browse")]` |
| Returns | `Views/Users/Index.cshtml` |

**Purpose**: Render the Users list page. The permission flags for Create, Update, and Delete are resolved here and passed into `AppDataTable.init` via the view.

**Controller logic**:
1. Resolve `canCreate`, `canUpdate`, `canDelete` using `IPermissionService.UserHasPermissionAsync`.
2. Pass them to the view via a strongly-typed ViewModel or `ViewBag`.

**View receives**:
```
canCreate   : bool
canUpdate   : bool
canDelete   : bool
```
*(Serialized to JSON inline in the Razor view for `AppDataTable.init`.)*

---

### GET /Users/GetData

| Property | Value |
|---|---|
| Route | `GET /Users/GetData` |
| Permission | `[HasPermission("User", "Browse")]` |
| Input | `DataTableRequest` (query string — bound `[FromQuery]`) |
| Returns | `JSON` — `DataTableResponse<UserRowDto>` |

**Purpose**: Server-side DataTables endpoint. Called by `AppDataTable` JS on every page load, search, sort, and page change.

**Input model** (`DataTableRequest`):

| Field | Type | Notes |
|---|---|---|
| `Draw` | `int` | Echo back to DataTables |
| `Start` | `int` | Row offset (0-based) |
| `Length` | `int` | Page size |
| `Search` | `string?` | Free-text search value |
| `SortColumn` | `int` | Column index for sort |
| `SortDirection` | `string` | `"asc"` or `"desc"` |

**Response shape** (`DataTableResponse<UserRowDto>`):

```json
{
  "draw": 3,
  "recordsTotal": 142,
  "recordsFiltered": 12,
  "data": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "fullName": "Alice Johnson",
      "email": "alice@example.com",
      "isActive": true,
      "roles": ["Admin", "Viewer"],
      "createdAt": "2025-11-01T08:00:00Z"
    }
  ]
}
```

**Search scope**: `FullName` and `Email` fields (case-insensitive `Contains`).  
**Sort columns**: Index maps to `[FullName(0), Email(1), IsActive(2), CreatedAt(3)]`.  
**Filtering**: No hard filter on IsActive — all users are returned regardless of status.

---

### GET /Users/Create

| Property | Value |
|---|---|
| Route | `GET /Users/Create` |
| Permission | `[HasPermission("User", "Create")]` |
| Returns | `Views/Users/Create.cshtml` with `CreateUserViewModel` |

**Controller logic**:
1. Load all available roles via `IRoleService.GetAllAsync()`.
2. Populate `AvailableRoles` on the ViewModel.
3. Default `IsActive = true`.

---

### POST /Users/Create

| Property | Value |
|---|---|
| Route | `POST /Users/Create` |
| Permission | `[HasPermission("User", "Create")]` |
| CSRF | `[ValidateAntiForgeryToken]` |
| Input | `CreateUserViewModel` (form body) |
| On success | Redirect to `GET /Users` with TempData success message |
| On failure | Return `Views/Users/Create.cshtml` with ModelState errors |

**Validation order**:
1. `ModelState.IsValid` — DataAnnotations (FullName, Email, Password, ConfirmPassword).
2. Email uniqueness — checked by `UserManager` inside `UserService.CreateAsync`; add Identity errors to `ModelState`.
3. Password complexity — enforced by Identity; errors surfaced via `IdentityResult.Errors`.

**On success**: `TempData["Success"] = "User created successfully."`

---

### GET /Users/Edit/{id}

| Property | Value |
|---|---|
| Route | `GET /Users/Edit/{id}` |
| Permission | `[HasPermission("User", "Update")]` |
| Input | `id` : route string (GUID) |
| Returns | `Views/Users/Edit.cshtml` with `EditUserViewModel` |
| On not found | Redirect to `GET /Users` |

**Controller logic**:
1. Call `IUserService.GetByIdAsync(id)` → returns `UserDto?`.
2. If null → redirect to Index.
3. Map `UserDto` → `EditUserViewModel`.
4. Load `AvailableRoles` via `IRoleService.GetAllAsync()`.
5. Pre-select `SelectedRoles` from `UserDto.Roles`.

---

### POST /Users/Edit/{id}

| Property | Value |
|---|---|
| Route | `POST /Users/Edit/{id}` |
| Permission | `[HasPermission("User", "Update")]` |
| CSRF | `[ValidateAntiForgeryToken]` |
| Input | `EditUserViewModel` (form body) |
| On success | Redirect to `GET /Users` with TempData success message |
| On failure | Return `Views/Users/Edit.cshtml` with ModelState errors |

**Validation order**:
1. `ModelState.IsValid`.
2. Email uniqueness (excluding current user) — checked by `UserService.UpdateAsync`; Identity errors added to `ModelState`.

---

### POST /Users/ToggleActive

| Property | Value |
|---|---|
| Route | `POST /Users/ToggleActive` |
| Permission | `[HasPermission("User", "Update")]` |
| CSRF | `[ValidateAntiForgeryToken]` |
| Input | `id` : form field (GUID string); `isActive` : form field (bool) |
| Returns | `JSON` `{ "success": true }` or `{ "success": false, "error": "..." }` |

**Purpose**: Inline activate/deactivate from the DataTable action column. Called via AJAX from `datatable.js` or a per-row button click handler on `Index.cshtml`.

**Controller logic**:
1. Call `IUserService.SetActiveAsync(id, isActive)`.
2. Return JSON with `success` boolean.
3. Do NOT redirect — caller updates the badge client-side on success.

**Response shape**:
```json
{ "success": true }
```
or on error:
```json
{ "success": false, "error": "User not found." }
```

---

## Permission Summary

| Action | Required Permission |
|---|---|
| Index | User → Browse |
| GetData | User → Browse |
| Create (GET + POST) | User → Create |
| Edit (GET + POST) | User → Update |
| ToggleActive (POST) | User → Update |

---

## Error Handling

| Scenario | Behaviour |
|---|---|
| User not found on Edit GET | Redirect to `GET /Users` |
| Identity errors on Create/Edit POST | Added to `ModelState`; form re-shown |
| Missing permission on any action | `HasPermissionAttribute` returns `Error403.cshtml` (HTTP 403) |
| Unauthenticated request | Redirected to Login by Identity middleware |
| Server error on ToggleActive | JSON `{ "success": false, "error": "..." }` |

---

## Notes

- No `DELETE /Users/{id}` action is exposed — deactivation (soft delete) is the only removal mechanism.
- The `ToggleActive` action is purposely not routed through `AppDataTable.deleteUrl` because it is not a delete operation; it is a state toggle.
- `DataTableRequest` and `DataTableResponse<T>` are defined in `AdminTemplate.Web/Models/`. If these were generated in Phase 5, use them as-is.
</content>
</invoke>