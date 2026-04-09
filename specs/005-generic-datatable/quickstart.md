# Quickstart: Generic DataTable Solution

**Feature**: 005-generic-datatable
**Date**: 2026-04-07
**Audience**: Developer implementing a new management page (Phase 6, 7, 8, etc.)

---

## Overview

This guide shows how to add a fully functional, permission-aware, server-side DataTable
to any management page in five steps. Each step maps to specific deliverables from
this feature.

---

## Prerequisites

The following are already present after this feature is implemented:

- `AdminTemplate.Application/DTOs/DataTableRequest.cs` — request DTO
- `AdminTemplate.Application/DTOs/DataTableResponse.cs` — response DTO
- `AdminTemplate.Web/Helpers/DataTableHelper.cs` — static server-side helper
- `AdminTemplate.Web/wwwroot/js/datatable.js` — `AppDataTable` JS module
- `AdminTemplate.Web/Views/Shared/_DeleteConfirmModal.cshtml` — delete confirmation modal
- `datatable.js` and DataTables.net CSS/JS loaded in `_Layout.cshtml`

---

## Step 1 — Add the `<table>` element to the view

```html
<!-- Views/Roles/Index.cshtml -->
@model RolesIndexViewModel

<div class="page-header d-flex justify-content-between align-items-center mb-4">
    <div>
        <h1 class="page-title">Roles</h1>
        <nav aria-label="breadcrumb">
            <ol class="breadcrumb">
                <li class="breadcrumb-item"><a asp-controller="Dashboard" asp-action="Index">Dashboard</a></li>
                <li class="breadcrumb-item active">Roles</li>
            </ol>
        </nav>
    </div>
</div>

<div class="card table-card">
    <div class="card-body">
        <table id="rolesTable" class="table table-hover w-100"></table>
    </div>
</div>

@Html.AntiForgeryToken()
<partial name="_DeleteConfirmModal" />
```

**Rules**:
- Wrap the table in `.card.table-card` (design system requirement).
- The `<table>` must have a unique `id` matching the `tableId` in the JS config.
- `@Html.AntiForgeryToken()` is mandatory if `deleteUrl` is configured.
- `<partial name="_DeleteConfirmModal" />` must be on the page if `deleteUrl` is configured.

---

## Step 2 — Add a `GetData` action to the controller

```csharp
// Controllers/RolesController.cs
[HttpGet]
[HasPermission("Role", "Browse")]
public IActionResult GetData([FromQuery] DataTableRequest request)
{
    var query = _roleService.GetQueryable(); // returns IQueryable<RoleDto>
    var response = DataTableHelper.GetDataTableResponse(query, request);
    return Json(response);
}
```

**Rules**:
- Decorate with `[HasPermission("ObjectName", "Browse")]` — never expose unauthenticated.
- Return `Json(response)` — not `Ok(response)` (preserves camelCase behaviour).
- The `IQueryable<T>` must project to a DTO that includes an `id` property.

---

## Step 3 — Resolve permissions server-side

```csharp
// Controllers/RolesController.cs
[HttpGet]
[HasPermission("Role", "Browse")]
public async Task<IActionResult> Index()
{
    var userId = User.GetUserId(); // extension method
    var permissions = await _permissionService.GetUserPermissionsAsync(userId, "Role");

    var vm = new RolesIndexViewModel
    {
        CanCreate = permissions.Contains("Create"),
        CanUpdate = permissions.Contains("Update"),
        CanDelete = permissions.Contains("Delete")
    };
    return View(vm);
}
```

The ViewModel carries the resolved permission flags into the view. They are then
serialised into the JavaScript config object (Step 4).

---

## Step 4 — Initialise the DataTable with `AppDataTable.init()`

```html
<!-- at the bottom of Views/Roles/Index.cshtml -->
@section Scripts {
<script>
AppDataTable.init({
    tableId:    'rolesTable',
    ajaxUrl:    '@Url.Action("GetData", "Roles")',
    columns: [
        { data: 'name',        title: 'Name' },
        { data: 'description', title: 'Description' },
        { data: 'userCount',   title: 'Users' },
        {
            data: 'isActive',
            title: 'Status',
            orderable: false,
            render: (val) => val
                ? '<span class="badge badge-soft badge-soft-success">Active</span>'
                : '<span class="badge badge-soft badge-soft-danger">Inactive</span>'
        }
    ],
    permissions: {
        canCreate: @Json.Serialize(Model.CanCreate),
        canUpdate: @Json.Serialize(Model.CanUpdate),
        canDelete: @Json.Serialize(Model.CanDelete)
    },
    createUrl:  '@Url.Action("Create", "Roles")',
    editUrl:    '@Url.Action("Edit",   "Roles")/:id',
    deleteUrl:  '@Url.Action("Delete", "Roles")/:id',
    objectName: 'Role'
});
</script>
}
```

**Rules**:
- Permissions come from the ViewModel — never hard-code `true` in the JS.
- Use `@Url.Action()` for all URLs — never hard-code paths.
- Use `:id` placeholder in `editUrl` and `deleteUrl`.
- Status/badge columns must use a `render` callback with `.badge-soft` classes.
- `objectName` is used in the delete modal body text.

---

## Step 5 — Add `GetQueryable()` to the service (if not already present)

```csharp
// Application/Interfaces/IRoleService.cs
IQueryable<RoleDto> GetQueryable();

// Infrastructure/Services/RoleService.cs
public IQueryable<RoleDto> GetQueryable()
{
    return _context.Roles
        .Select(r => new RoleDto
        {
            Id          = r.Id,
            Name        = r.Name,
            Description = r.Description,
            UserCount   = r.Users.Count,
            IsActive    = r.IsActive
        });
}
```

**Rules**:
- Projection must include `Id` (even if not in the visible columns list).
- The DTO must use PascalCase property names; serialisation to camelCase is handled globally.
- `GetQueryable()` must return `IQueryable<T>` (not `IEnumerable<T>`) so
  `DataTableHelper` can defer execution until after filtering and paging.

---

## Expected Result

After completing Steps 1–5:

- The page loads with the table header and no rows visible (loading spinner shown).
- DataTables fetches the first page of data from `GetData`.
- Rows render with the correct columns and badge states.
- The Create button appears in the toolbar if `CanCreate` is true (absent from DOM otherwise).
- Edit and Delete buttons appear per row if the respective permissions are true.
- Clicking Delete opens the Bootstrap 5 modal naming "Role"; confirming sends a POST
  with the CSRF token and removes the row on success.
- Typing in the search box filters results within 400 ms.
- Clicking a column header sorts results.
- Pagination works across all pages of data.
- Switching to RTL layout mirrors the toolbar, table controls, and pagination correctly.

---

## Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| `AppDataTable is not defined` | `datatable.js` not loaded | Check `_Layout.cshtml` script bundle order |
| Table renders but no data loads | `ajaxUrl` returning non-JSON or 403 | Check `[HasPermission]` attribute and verify action returns `Json(response)` |
| Delete POST returns 400 | Anti-forgery token missing | Add `@Html.AntiForgeryToken()` to the view |
| Create/Edit buttons missing even with `canCreate: true` | `createUrl` or `editUrl` is `null`/omitted | Provide the URL in the config alongside the permission flag |
| Buttons visible when permission is false | Permissions not resolved server-side | Check ViewModel — permissions must come from the server, not JS literals |
| Row IDs are `undefined` in edit/delete URLs | `id` missing from DTO | Ensure DTO projection includes `Id` |
