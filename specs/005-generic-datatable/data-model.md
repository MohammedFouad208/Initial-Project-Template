# Data Model: Generic DataTable Solution

**Feature**: 005-generic-datatable
**Phase**: 1 — Design
**Date**: 2026-04-07

---

## Overview

This feature introduces three C# artefacts and one JavaScript config shape.
No database schema changes are required; the feature is purely a query-processing
and rendering layer on top of existing repositories.

---

## C# Entities (Application Layer)

### `DataTableRequest`

**File**: `AdminTemplate.Application/DTOs/DataTableRequest.cs`
**Purpose**: Represents the query parameters sent by the DataTables jQuery plugin to
a server-side endpoint. Model-bound automatically by ASP.NET Core from the request
query string.

| Property | Type | Required | Description |
|---|---|---|---|
| `Draw` | `int` | Yes | Echo counter sent by client; must be returned unchanged |
| `Start` | `int` | Yes | Zero-based offset of the first record to return |
| `Length` | `int` | Yes | Number of records per page (-1 = all) |
| `Search` | `DataTableSearch` | Yes | Nested object containing search terms |
| `Order` | `List<DataTableOrder>` | Yes | Sort instructions (first element used) |
| `Columns` | `List<DataTableColumn>` | Yes | Column metadata (used to resolve sort column name) |

**Nested type — `DataTableSearch`**:

| Property | Type | Description |
|---|---|---|
| `Value` | `string` | Global search term; empty string means no filter |
| `Regex` | `bool` | Whether to treat value as regex (DataTables always sends `false`) |

**Nested type — `DataTableOrder`**:

| Property | Type | Description |
|---|---|---|
| `Column` | `int` | Zero-based index into the `Columns` list |
| `Dir` | `string` | `"asc"` or `"desc"` |

**Nested type — `DataTableColumn`**:

| Property | Type | Description |
|---|---|---|
| `Data` | `string` | Property name of the data field (matches `columns[].data` in JS config) |
| `Searchable` | `bool` | Whether this column participates in global string search |
| `Orderable` | `bool` | Whether this column can be sorted |

**Validation rules**:
- `Draw` must be ≥ 1.
- `Start` must be ≥ 0.
- `Length` must be ≥ -1 and ≤ 1000 (max page size guard).

---

### `DataTableResponse<T>`

**File**: `AdminTemplate.Application/DTOs/DataTableResponse.cs`
**Purpose**: The JSON response envelope returned by server-side DataTable endpoints.
DataTables validates the presence of `draw`, `recordsTotal`, and `recordsFiltered`.

| Property | Type | Description |
|---|---|---|
| `Draw` | `int` | Echo of the `Draw` value received in the request |
| `RecordsTotal` | `int` | Total records in the table (before any filter) |
| `RecordsFiltered` | `int` | Records matching the current search filter |
| `Data` | `IEnumerable<T>` | The page of row objects |

**Serialisation note**: Property names must serialise as camelCase (`draw`, `recordsTotal`,
`recordsFiltered`, `data`). This is satisfied by configuring `JsonNamingPolicy.CamelCase`
globally in `Program.cs`, or by decorating each property with `[JsonPropertyName("...")]`.

---

## Static Helper (Web Layer)

### `DataTableHelper`

**File**: `AdminTemplate.Web/Helpers/DataTableHelper.cs`
**Purpose**: Generic static utility that applies DataTables request parameters (paging,
sorting, filtering) to an `IQueryable<T>` and returns a `DataTableResponse<T>`.

**Method signature**:
```csharp
public static DataTableResponse<T> GetDataTableResponse<T>(
    IQueryable<T> source,
    DataTableRequest request)
```

**Processing logic**:

1. **Count total**: `source.Count()` → `RecordsTotal`.
2. **Filter**: if `request.Search.Value` is non-empty, apply `.Where()` dynamically
   over all `string` properties of `T` marked searchable (reflected once, cached).
   Uses `string.Contains()` with `StringComparison.OrdinalIgnoreCase`.
3. **Count filtered**: after filter, `.Count()` → `RecordsFiltered`.
4. **Sort**: use `request.Order[0].Column` to look up `request.Columns[i].Data`;
   apply `OrderBy` / `OrderByDescending` via LINQ expression tree on property name.
   Falls back to first column ascending if sort column is invalid.
5. **Page**: `.Skip(request.Start).Take(request.Length)` (skipped if `Length == -1`).
6. **Return**: `new DataTableResponse<T> { Draw=request.Draw, RecordsTotal, RecordsFiltered, Data }`.

**Constraints**:
- Filtering applies only to `string` properties. Numeric/date filtering is out of scope.
- The method is intentionally synchronous (returns `DataTableResponse<T>`, not `Task<...>`).
  Callers may wrap in `Task.Run` if needed, but EF Core `IQueryable` evaluation timing
  is handled by the caller's service layer.
- No dependency injection — static class, zero DI cost.

---

## JavaScript Config Shape (`AppDataTable.init`)

**File**: `AdminTemplate.Web/wwwroot/js/datatable.js`
**Purpose**: Client-side JavaScript module. `AppDataTable` is a global object (assigned
to `window.AppDataTable`) exposing a single `init(config)` method.

### Config Object Properties

| Property | Type | Required | Default | Description |
|---|---|---|---|---|
| `tableId` | `string` | Yes | — | CSS selector `id` of the `<table>` element |
| `ajaxUrl` | `string` | Yes | — | URL for server-side data endpoint |
| `columns` | `Column[]` | Yes | — | Column definitions (see below) |
| `permissions` | `Permissions` | Yes | — | Object controlling button visibility |
| `createUrl` | `string` | No | `null` | URL for Create page (redirect); button absent if omitted |
| `editUrl` | `string` | No | `null` | URL template with `:id` placeholder; button absent if omitted |
| `deleteUrl` | `string` | No | `null` | URL template with `:id` placeholder; button absent if omitted |
| `objectName` | `string` | No | `"Record"` | Used in delete modal body: "Delete this {objectName}?" |
| `messages` | `Messages` | No | (defaults) | Overrides for UI string defaults |

### `Column` Object

| Property | Type | Required | Description |
|---|---|---|---|
| `data` | `string` | Yes | Property name in the server row object |
| `title` | `string` | Yes | Column header text |
| `orderable` | `bool` | No (default: `true`) | Whether the column is sortable |
| `searchable` | `bool` | No (default: `true`) | Whether the column participates in global search |
| `render` | `function` | No | Custom cell renderer: `(data, type, row) => string` |

### `Permissions` Object

| Property | Type | Default | Description |
|---|---|---|---|
| `canCreate` | `bool` | `false` | Show Create button in toolbar |
| `canUpdate` | `bool` | `false` | Show Edit button per row |
| `canDelete` | `bool` | `false` | Show Delete button per row |

### `Messages` Object (all optional; override defaults)

| Property | Default value |
|---|---|
| `emptyTable` | `"No records found."` |
| `loadingRecords` | `"Loading..."` |
| `errorTitle` | `"Failed to load data."` |
| `errorRetry` | `"Retry"` |
| `deleteTitle` | `"Confirm Delete"` |
| `deleteBody` | `"Are you sure you want to delete this {objectName}?"` |
| `deleteConfirm` | `"Delete"` |
| `deleteCancel` | `"Cancel"` |

---

## Razor Partial

### `_DeleteConfirmModal`

**File**: `AdminTemplate.Web/Views/Shared/_DeleteConfirmModal.cshtml`
**Purpose**: Bootstrap 5 modal used by `datatable.js` for delete confirmation.
Rendered once per page via `<partial name="_DeleteConfirmModal" />` in the layout or
in each view that includes a DataTable.

**DOM structure**:
```html
<div id="deleteConfirmModal" class="modal fade" ...>
  <div class="modal-dialog modal-dialog-centered">
    <div class="modal-content">
      <div class="modal-header border-0">
        <h5 class="modal-title" id="deleteConfirmLabel">Confirm Delete</h5>
        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
      </div>
      <div class="modal-body" id="deleteConfirmBody">
        <!-- JS writes: "Are you sure you want to delete this {objectName}?" -->
      </div>
      <div class="modal-footer border-0">
        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
        <button type="button" class="btn btn-danger" id="deleteConfirmBtn">Delete</button>
      </div>
    </div>
  </div>
</div>
```

**Key element IDs** (used by `datatable.js`):
- `deleteConfirmModal` — modal root (Bootstrap 5 target)
- `deleteConfirmBody` — JS sets inner text to the delete confirmation message
- `deleteConfirmBtn` — JS attaches click handler; stores `data-delete-url` and `data-record-id`

---

## State Transitions

For each DataTable instance, the wrapper `<div class="card table-card">` cycles through
these visual states:

```
[page load]
     │
     ▼
 LOADING  ──(XHR resolves with data)──▶  LOADED
     │
     ├──(XHR resolves with 0 rows)──▶  EMPTY
     │
     └──(XHR fails / non-2xx)────────▶  ERROR ──(retry clicked)──▶ LOADING
```

CSS classes applied to the wrapper:
- `dt-state-loading` — spinner visible in `tbody`, controls disabled
- `dt-state-empty` — custom empty-state message in `tbody`
- `dt-state-error` — error panel with Retry button replaces `tbody` content
- *(no class)* — normal rendered state
