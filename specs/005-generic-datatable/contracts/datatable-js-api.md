# Contract: AppDataTable JavaScript API

**Feature**: 005-generic-datatable
**Version**: 1.0
**Date**: 2026-04-07
**Consumer**: Every management page Razor view
**Producer**: `datatable.js` (window.AppDataTable)

---

## Overview

`AppDataTable` is a global JavaScript object attached to `window`. It exposes a single
public method, `init(config)`, that fully initialises a DataTables.net instance with
permission-aware toolbar, action column, delete modal integration, and custom UX states.
No per-page jQuery DataTable initialisation is permitted (Principle VI).

---

## Method: `AppDataTable.init(config)`

### Signature

```javascript
AppDataTable.init({
  tableId:     string,             // required
  ajaxUrl:     string,             // required
  columns:     Column[],           // required, min 1
  permissions: Permissions,        // required
  createUrl:   string | null,      // optional
  editUrl:     string | null,      // optional, use :id placeholder
  deleteUrl:   string | null,      // optional, use :id placeholder
  objectName:  string,             // optional, default: "Record"
  messages:    Messages            // optional, override UI strings
})
```

### Return Value
`void`. The DataTables instance is internally managed; pages do not interact with it
directly (use the `dtable:deleted` custom DOM event if post-delete logic is needed).

---

## Types

### `Column`

```typescript
interface Column {
  data:       string;              // required — property name in server row object
  title:      string;              // required — column header text
  orderable?: boolean;             // optional, default true
  searchable?: boolean;            // optional, default true
  render?:    (data: any, type: string, row: object) => string;  // optional
}
```

### `Permissions`

```typescript
interface Permissions {
  canCreate: boolean;   // toolbar Create button
  canUpdate: boolean;   // per-row Edit button
  canDelete: boolean;   // per-row Delete button
}
```

### `Messages`

```typescript
interface Messages {
  emptyTable?:   string;   // default: "No records found."
  loadingRecords?: string; // default: "Loading..."
  errorTitle?:   string;   // default: "Failed to load data."
  errorRetry?:   string;   // default: "Retry"
  deleteTitle?:  string;   // default: "Confirm Delete"
  deleteBody?:   string;   // default: "Are you sure you want to delete this {objectName}?"
  deleteConfirm?: string;  // default: "Delete"
  deleteCancel?:  string;  // default: "Cancel"
}
```

---

## Behaviour Specification

### Toolbar Rendering

- Always rendered above the DataTables search/length controls.
- Create button (`.btn.btn-primary.btn-sm` with `<i class="fa fa-plus">`) is rendered
  **only** when `permissions.canCreate === true` AND `createUrl` is non-null.
- If both conditions are not met, the Create button is **absent from the DOM**.
- Clicking Create navigates to `createUrl` (full-page redirect).

### Action Column

- Appended as the last column with header "Actions".
- `orderable: false`, `searchable: false`.
- Per-row content:
  - Edit button (`.btn.btn-outline-secondary.btn-sm`): rendered when
    `permissions.canUpdate === true` AND `editUrl` is non-null.
  - Delete button (`.btn.btn-outline-danger.btn-sm`): rendered when
    `permissions.canDelete === true` AND `deleteUrl` is non-null.
  - Cell is empty if neither condition is met.
- Buttons absent from DOM (not CSS-hidden) when conditions are not met.

### URL Substitution

```
editUrl:   '/Users/Edit/:id'   → '/Users/Edit/abc123'
deleteUrl: '/Users/Delete/:id' → '/Users/Delete/abc123'
```
- `:id` is replaced with `row.id` (the `id` field of the server row object).
- The `id` field must be present in the AJAX response even if not in the visible columns.

### Delete Confirmation Flow

1. User clicks Delete button.
2. `datatable.js` populates `#deleteConfirmBody` with the configured message.
3. `datatable.js` stores resolved URL and row ID on `#deleteConfirmBtn` as
   `data-delete-url` and `data-record-id`.
4. Bootstrap 5 `Modal.show()` opens `#deleteConfirmModal`.
5. a) If user clicks Cancel or presses Escape: modal closes; no request sent.
   b) If user clicks Confirm:
      - Reads `RequestVerificationToken` from `input[name="__RequestVerificationToken"]`.
      - If token absent: `console.warn(...)` and abort — no request sent.
      - POSTs to `data-delete-url` with header `RequestVerificationToken: <token>`.
      - On 2xx: closes modal, removes row, redraws table, dispatches `dtable:deleted`.
      - On non-2xx: closes modal, shows error alert in the table card.

### Custom DOM Event

```javascript
tableElement.dispatchEvent(new CustomEvent('dtable:deleted', {
  detail: { id: rowData.id },
  bubbles: true
}));
```

Pages can listen: `document.getElementById('myTable').addEventListener('dtable:deleted', e => { ... })`.

### Null Column Values

Column cells with a `null` or `undefined` value from the server render as `—` (em-dash)
via a default `render` wrapper applied to every column that does not supply its own
`render` function.

### UX States

| State | Trigger | UI |
|---|---|---|
| Loading | XHR in flight | Spinner (`fa-spinner fa-spin`) in `tbody`; search + pagination disabled |
| Empty | XHR 200, `data.length === 0` | `.dt-empty-state` div centred in `tbody`; uses `var(--text-secondary)` |
| Error | XHR non-2xx or network failure | Error panel with `border-inline-start: 4px solid var(--danger)`; Retry button |
| Normal | XHR 200, `data.length > 0` | Standard DataTables rows |

---

## Prerequisites (Caller Responsibility)

The page that calls `AppDataTable.init()` MUST:
1. Include `@Html.AntiForgeryToken()` in the Razor view.
2. Include `<partial name="_DeleteConfirmModal" />` (or ensure it is in the layout).
3. Have jQuery and DataTables.net loaded before `datatable.js`.
4. Have Bootstrap 5 JS loaded (for `bootstrap.Modal`).
5. Include an `id` property in the AJAX response row objects (even if not a visible column).

---

## Breaking Changes Policy

Changes to `init()` config property names or removal of properties are **MAJOR** changes
and require a version bump to this contract and a global search for all callers.
Adding new optional properties is **MINOR** and backward-compatible.
