# Contract: DataTable AJAX Endpoint

**Feature**: 005-generic-datatable
**Version**: 1.0
**Date**: 2026-04-07
**Consumer**: `AppDataTable` (datatable.js) — browser client
**Producer**: Each management page controller action (e.g., `UsersController.GetData`)

---

## Overview

Every management page that uses `AppDataTable.init()` exposes a server-side JSON
endpoint that receives DataTables query parameters and returns a standard DataTables
response envelope. The helper `DataTableHelper.GetDataTableResponse<T>()` implements
the server-side logic; the contract below defines the wire format.

---

## Request

**Method**: GET (DataTables default; can be overridden to POST in advanced config)
**Content-Type**: N/A (query string parameters)

### URL Example
```
GET /Users/GetData?draw=1&start=0&length=10&search%5Bvalue%5D=john&order%5B0%5D%5Bcolumn%5D=1&order%5B0%5D%5Bdir%5D=asc
```

### Query String Parameters

| Parameter | Type | Required | Description |
|---|---|---|---|
| `draw` | integer | Yes | Client echo counter (≥ 1) |
| `start` | integer | Yes | Zero-based record offset |
| `length` | integer | Yes | Records per page (-1 = all; capped at 1000) |
| `search[value]` | string | Yes | Global search term (empty string = no filter) |
| `search[regex]` | boolean | Yes | Always `false` from DataTables UI |
| `order[0][column]` | integer | Yes | Zero-based column index to sort by |
| `order[0][dir]` | string | Yes | `"asc"` or `"desc"` |
| `columns[i][data]` | string | Yes (per column) | Property name for column `i` |
| `columns[i][searchable]` | boolean | Yes (per column) | Participates in global search |
| `columns[i][orderable]` | boolean | Yes (per column) | Can be sorted |

### Model Binding
ASP.NET Core automatically binds the above to `DataTableRequest` via the nested
class structure (`Search.Value`, `Order[0].Column`, `Columns[i].Data`).

---

## Response

**Status codes**:
- `200 OK` — successful query (including zero results)
- `400 Bad Request` — invalid request parameters (e.g., `Length > 1000`)
- `401 Unauthorized` — user not authenticated
- `403 Forbidden` — user lacks Browse permission for this object
- `500 Internal Server Error` — unexpected server fault (logged; not surfaced to client)

### Success Response Body

**Content-Type**: `application/json`

```json
{
  "draw": 1,
  "recordsTotal": 150,
  "recordsFiltered": 12,
  "data": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "fullName": "Alice Smith",
      "email": "alice@example.com",
      "isActive": true
    }
  ]
}
```

### Response Field Definitions

| Field | Type | Description |
|---|---|---|
| `draw` | integer | Echo of the `draw` value received in the request |
| `recordsTotal` | integer | Total rows in table before any filter |
| `recordsFiltered` | integer | Rows matching the current `search[value]` filter |
| `data` | array | Page of row objects; each object must include `id` |

### Error Response Body (non-2xx)

```json
{
  "error": "An unexpected error occurred."
}
```

---

## Constraints

- The `id` property MUST be present in every row object in `data`, even if not
  displayed as a visible column. It is used by `datatable.js` for edit/delete URL
  substitution.
- `recordsTotal` and `recordsFiltered` MUST reflect counts at the time of the request;
  they are used by DataTables to render pagination controls accurately.
- Response MUST be scoped to the authenticated user's accessible data; server enforces
  `[Authorize]` and `[HasPermission("Object", "Browse")]`.

---

## Security

- Endpoint MUST be decorated with `[Authorize]` and `[HasPermission("...", "Browse")]`.
- No sensitive fields (e.g., password hashes, tokens) may appear in `data`.
- The endpoint is a GET; it MUST be idempotent (read-only).
- User input in `search[value]` is passed to `string.Contains()` — no SQL injection
  risk when using EF Core with parameterised queries.
