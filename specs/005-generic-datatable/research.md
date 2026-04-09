# Research: Generic DataTable Solution

**Feature**: 005-generic-datatable
**Phase**: 0 — Unknowns resolved
**Date**: 2026-04-07

---

## 1. DataTables.net Server-Side Protocol

### Decision
Use the standard DataTables.net server-side processing protocol with jQuery AJAX.
DataTables sends a fixed set of parameters and expects a fixed JSON response shape.

### Request Parameters (sent by DataTables to `ajaxUrl`)

| Parameter | Type | Description |
|---|---|---|
| `draw` | int | Client-generated counter; echoed back to prevent out-of-order responses |
| `start` | int | Zero-based record offset for paging |
| `length` | int | Number of records per page (-1 = all) |
| `search[value]` | string | Global search string |
| `search[regex]` | bool | Whether the search is a regex (always `false` from DataTables UI) |
| `order[0][column]` | int | Zero-based column index to sort by |
| `order[0][dir]` | string | `"asc"` or `"desc"` |
| `columns[i][data]` | string | The `data` property of the i-th column config |
| `columns[i][searchable]` | bool | Whether this column participates in global search |
| `columns[i][orderable]` | bool | Whether this column can be sorted |

**Critical finding**: `search[value]` is the key used by the DataTables jQuery plugin.
ASP.NET Core model binding will auto-bind this if `DataTableRequest` uses a nested
`Search` class with a `Value` property.

### Response Shape (C# → JSON)

```json
{
  "draw": 3,
  "recordsTotal": 200,
  "recordsFiltered": 45,
  "data": [ { ... }, { ... } ]
}
```

Note: property names **must be camelCase** in the JSON response. ASP.NET Core's default
`System.Text.Json` serialiser uses camelCase by default when configured via
`AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase)`.
If that setting is absent, use `[JsonPropertyName("draw")]` attributes on the DTO.

**Rationale**: Strict adherence to this protocol is required — DataTables validates the
`draw` echo and discards out-of-order responses natively.

**Alternatives considered**: Custom protocol (rejected — breaks DataTables library
internals and binary compatibility with DataTables plugins).

---

## 2. DataTableHelper Layer Placement

### Decision
Place `DataTableHelper.cs` in `AdminTemplate.Web/Helpers/` as a **static class**.

### Rationale
- The helper parses `DataTableRequest` (an Application DTO) into LINQ operations
  against an `IQueryable<T>`.
- `IQueryable<T>` is in `System.Linq` — it is not EF-specific.
- However, the helper is consumed exclusively by MVC controllers; it has no domain
  meaning and no Application-layer service would ever call it.
- Placing it in Web keeps Infrastructure and Application layers free of web-tier
  request-processing concerns, consistent with Principle I (Clean Architecture) and
  Principle III (Thin Controllers).
- The helper is a **static utility**, not a service: no DI, no interface needed.

**Alternatives considered**:
- Application layer: rejected — mixing HTTP request parameter parsing into Application
  violates the "Application knows nothing about HTTP" invariant.
- Infrastructure layer: rejected — Infrastructure should not know about HTTP parameter
  conventions; it is purely a data-access layer.

---

## 3. URL ID Substitution Pattern

### Decision
Use a `:id` placeholder convention in `editUrl` and `deleteUrl` config properties.
`datatable.js` replaces `:id` with the row's `id` field value when building URLs.

```javascript
// Example config
editUrl: '/Users/Edit/:id'
deleteUrl: '/Users/Delete/:id'

// Runtime substitution in datatable.js
const url = config.editUrl.replace(':id', rowData.id);
```

### Rationale
- Simple string replacement; no regex needed.
- Makes config readable: developer can see the URL shape at a glance.
- Works for both anchor-style Edit (redirect) and POST-style Delete.
- The `id` field must be included in the AJAX response data (even if not displayed
  as a visible column). This is documented in quickstart.md.

**Alternatives considered**:
- Appending ID to URL (`/Users/Edit/` + `rowData.id`): rejected — less flexible if the
  ID is not the last segment (e.g., `/Users/{userId}/Roles`).
- Template literals with arbitrary property names (e.g., `{userId}`): rejected —
  over-engineered for single-ID scenarios; `:id` convention is widely understood.

---

## 4. CSRF Token Injection for AJAX Delete

### Decision
Read the anti-forgery token from `input[name="__RequestVerificationToken"]` injected
by `@Html.AntiForgeryToken()`. Send it as the `RequestVerificationToken` HTTP header.

```javascript
const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
if (!token) {
  console.warn('[AppDataTable] RequestVerificationToken not found on page.');
  return;
}
fetch(url, {
  method: 'POST',
  headers: { 'RequestVerificationToken': token }
});
```

ASP.NET Core's `[ValidateAntiForgeryToken]` attribute accepts the token from either:
- `Request.Form["__RequestVerificationToken"]` (form POST), OR
- `Request.Headers["RequestVerificationToken"]` (AJAX header)

The header approach is confirmed by the official Microsoft XSRF/CSRF documentation.

### Rationale
- The header approach works for `fetch`-based AJAX without any form submission.
- It avoids embedding the token in the request body (no `FormData` wrapper needed).
- Consistent with how DataTables does AJAX (no form involved in delete).
- The early-abort with console warning is the correct behaviour when the token is
  missing (per FR-010 in the spec) — it prevents a guaranteed 400 response.

**Alternatives considered**:
- Sending as `FormData` body: rejected — delete actions may not expect form-encoded
  bodies; JSON body with token embedded is non-standard for ASP.NET Core CSRF.
- Using a `<meta>` tag: rejected — adds a new convention not already used in the
  project; `@Html.AntiForgeryToken()` hidden input is the existing project pattern.

---

## 5. Bootstrap 5 Modal Lifecycle for Delete Confirmation

### Decision
Inject one shared `_DeleteConfirmModal.cshtml` partial at the bottom of pages that use
a DataTable. `datatable.js` populates the modal's body text and stores the pending
delete URL/ID on the confirm button's `data-*` attributes. The Bootstrap 5 `Modal` JS
API opens it imperatively.

```javascript
// datatable.js sets data attributes, then opens
confirmBtn.dataset.deleteUrl = resolvedUrl;
confirmBtn.dataset.recordId  = rowData.id;
new bootstrap.Modal(document.getElementById('deleteConfirmModal')).show();
```

### Rationale
- One modal element per page (not one per row) — DOM-efficient.
- Data is supplied at click time, not at render time, so stale state is impossible.
- Bootstrap `Modal` JS API is already loaded via `_Layout.cshtml`.
- The same modal is reused by all tables on a page (if a page has multiple tables).

**Alternatives considered**:
- Dynamically creating a modal element per delete click: rejected — leaks DOM nodes
  without careful cleanup; not simpler.
- An inline `<dialog>` element: rejected — requires additional CSS not in the design
  system; Bootstrap 5 modals are the established pattern.

---

## 6. Empty / Error / Loading State Implementation

### Decision
Override DataTables' `language` config option to customise the empty-state string.
Use DataTables' `preXhr` / `xhr` callbacks to toggle a CSS class on the wrapper for
loading and error states. Custom HTML is rendered by `datatable.js` via `drawCallback`.

| State | Mechanism |
|---|---|
| Loading | `preXhr` callback adds `.dt-loading` class to wrapper; removed in `xhr` callback |
| Empty | `language.emptyTable` config overrides the default DataTables text |
| Error | `xhr` callback checks status; if non-2xx, replaces table body with error HTML |

Styling uses only design token classes:
- Loading: spinner SVG or `fa-spinner fa-spin` inside the `tbody`
- Empty: centre-aligned `<div class="dt-empty-state">` using `var(--text-secondary)`
- Error: `<div class="alert" style="border-inline-start: 4px solid var(--danger)">` +
  Retry button (`.btn.btn-outline-danger.btn-sm`)

**Rationale**: DataTables' callback system is the standard extension point. These
hooks are non-breaking and survive DataTables library version updates.

**Alternatives considered**: Replacing DataTables' renderer entirely: rejected — too
much surface area and breaks library guarantees.
