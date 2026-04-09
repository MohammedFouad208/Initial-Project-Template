# Feature Specification: Generic DataTable Solution

**Feature Branch**: `005-generic-datatable`
**Created**: 2026-04-07
**Status**: Draft
**Input**: PHASE 5 — Generic DataTable Solution from Plans/Plan.md

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Developer Initialises a Fully Functional Data Table in Five Lines (Priority: P1)

A developer building a new management page (e.g., Users, Roles, Employees) needs a
paginated, searchable, sortable table that loads data from the server without writing
boilerplate JavaScript. They call `AppDataTable.init({ ... })` with a config object and
the table renders immediately — toolbar, column headers, rows, and pagination controls
included.

**Why this priority**: This is the core deliverable. Every subsequent management page
(Phases 6, 7, 8) depends on it. Without a working `init()`, no management page can be built.

**Independent Test**: Create a bare HTML page that references `datatable.js`, calls
`AppDataTable.init()` with a mock AJAX endpoint, and verify that the table renders rows,
responds to search input, and navigates pages — without any other application code.

**Acceptance Scenarios**:

1. **Given** a page that calls `AppDataTable.init({ tableId, ajaxUrl, columns, ... })`,
   **When** the page loads,
   **Then** the table renders with the correct column headers, fetches the first page of
   data from `ajaxUrl`, and displays rows matching the `columns` config.

2. **Given** a rendered table,
   **When** the user types in the search input,
   **Then** a new server request is made within 400 ms including the search term, the
   table rows update to match results, and the page resets to page 1.

3. **Given** a rendered table with multiple pages of data,
   **When** the user clicks a column header to sort,
   **Then** a new server request is made with the correct `order[0][column]` and
   `order[0][dir]` parameters, and rows update accordingly.

4. **Given** a rendered table,
   **When** the server returns zero rows,
   **Then** an empty-state message is displayed centred in the table body (no raw
   "No data available" DataTables default text).

---

### User Story 2 — Administrator Sees Only the Action Buttons Their Role Permits (Priority: P1)

An admin user whose role has only "Browse" permission on an object visits the management
page for that object. The Create button in the toolbar and the Edit/Delete buttons in
every row must be absent — not just hidden by CSS — so the user cannot discover or invoke
them without the correct permission.

**Why this priority**: Permission-aware UI is a security requirement and a core
constitution principle. It must work correctly before any management page goes live.

**Independent Test**: Render the table twice on a test page — once with
`canCreate: false, canUpdate: false, canDelete: true` and once with all `true`.
Verify the DOM contains exactly the expected buttons in each case.

**Acceptance Scenarios**:

1. **Given** a table initialised with `canCreate: false`,
   **When** the page renders,
   **Then** no Create button exists anywhere in the toolbar DOM.

2. **Given** a table initialised with `canUpdate: false, canDelete: false`,
   **When** rows are rendered,
   **Then** the action column cell for each row is empty (no Edit or Delete button).

3. **Given** a table initialised with `canCreate: true, canUpdate: true, canDelete: true`,
   **When** the page renders with data,
   **Then** the Create button appears in the toolbar and each row has an Edit button and
   a Delete button.

4. **Given** a table initialised with mixed permissions (`canCreate: true, canDelete: false`),
   **When** rows are rendered,
   **Then** each row has an Edit button but no Delete button, and the Create button is
   present in the toolbar.

---

### User Story 3 — Administrator Deletes a Record via a Styled Confirmation Modal (Priority: P2)

An admin clicks the Delete button on a row. Instead of a raw browser `confirm()` dialog,
a styled Bootstrap 5 modal appears naming the record (or the object type) and requiring
an explicit confirmation. Upon confirmation, the delete request is sent with the
anti-forgery token; upon cancellation, nothing changes.

**Why this priority**: The modal must exist before any real delete operation is wired up
(Phases 6–7). A raw `window.confirm()` violates the design system rule.

**Independent Test**: Click a Delete button, verify the Bootstrap modal opens, check that
the modal contains the object name, click Cancel, verify no network request was made,
click Delete again, click Confirm, verify a POST to `deleteUrl` was made with the
anti-forgery token header.

**Acceptance Scenarios**:

1. **Given** a table row with a Delete button,
   **When** the user clicks Delete,
   **Then** a Bootstrap 5 modal opens with a danger-styled confirm button and a Cancel
   button; no network request is made yet.

2. **Given** the delete confirmation modal is open,
   **When** the user clicks Cancel or presses Escape,
   **Then** the modal closes and no request is sent to `deleteUrl`.

3. **Given** the delete confirmation modal is open,
   **When** the user clicks the danger Confirm button,
   **Then** a POST request is sent to `deleteUrl` with the row ID and the
   `RequestVerificationToken` header; the modal closes; the table row is removed and the
   table refreshes.

4. **Given** the delete request returns a non-2xx HTTP status,
   **When** the confirmation was submitted,
   **Then** the modal closes, an error alert is shown (using `var(--danger)` styling), and
   the row remains in the table.

---

### User Story 4 — Page Shows Loading, Empty, and Error States Gracefully (Priority: P3)

While data is loading, the table body shows a skeleton/spinner. If the AJAX request fails,
an inline error state with a retry button appears. All three states are styled using the
design token layer — no raw DataTables default UI.

**Why this priority**: These states are polish but must be built into the generic solution
so every management page automatically benefits. They can be added after the core table
and permission system work.

**Independent Test**: Simulate a delayed response (loading state), a 500 response (error
state), and an empty 200 response (empty state) via a mock endpoint; verify each state
renders the correct UI.

**Acceptance Scenarios**:

1. **Given** an AJAX call that has not yet resolved,
   **When** the table is waiting for data,
   **Then** a loading skeleton or spinner is visible in the table body area; the search
   input and pagination are disabled.

2. **Given** the AJAX call returns a network error or 5xx status,
   **When** the table attempts to load,
   **Then** an error message styled with `var(--danger-light)` and `border-inline-start:
   4px solid var(--danger)` is shown, along with a "Retry" button.

3. **Given** the retry button is clicked,
   **When** the retry request succeeds,
   **Then** the error state is replaced by the rendered table rows.

---

### Edge Cases

- **Large dataset**: Server must receive correct `start` and `length` parameters on every
  page change; client must not buffer all rows.
- **Column with null data**: A column whose server value is `null` or `undefined` must
  render an em-dash `—` rather than throwing a JS error or showing "null".
- **Delete of last row on a page**: After deletion the table should reload the previous
  page if the current page is now empty, rather than showing an empty page.
- **Concurrent deletes**: If two tabs delete the same record, the second delete response
  (404) must show an error message, not crash the table.
- **RTL direction**: Action-column buttons, toolbar layout, and pagination controls must
  all mirror correctly when `dir="rtl"` is active.
- **Zero columns configured**: If `columns` array is empty, the table must not crash; it
  must show an empty-state message.
- **CSRF token missing**: If `RequestVerificationToken` input is absent from the page, the
  delete request must fail early with a console warning rather than sending a tokenless
  request.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The solution MUST expose a single global object `AppDataTable` with an
  `init(config)` method that any page can call to render a server-side DataTable.

- **FR-002**: `init()` MUST accept a config object with the following properties:
  `tableId` (string), `ajaxUrl` (string), `columns` (array of `{ data, title }` objects),
  `permissions` (object with `canCreate`, `canUpdate`, `canDelete` booleans),
  `createUrl` (string), `editUrl` (string, may include `:id` placeholder),
  `deleteUrl` (string, may include `:id` placeholder), and `objectName` (string for
  dialog labels). All URL properties are optional; omitting one suppresses the
  corresponding button regardless of the permission flag.

- **FR-003**: The table MUST request data from `ajaxUrl` server-side, sending the
  standard DataTables parameters: `draw`, `start`, `length`, `search[value]`,
  `order[0][column]`, and `order[0][dir]`.

- **FR-004**: The server-side controller helper `GetDataTableResponse<T>` MUST return a
  JSON object with `draw`, `recordsTotal`, `recordsFiltered`, and `data` array — the
  DataTables server-side format.

- **FR-005**: When `canCreate` is `true` and `createUrl` is provided, a Create button
  styled as `.btn.btn-primary.btn-sm` with a `fa-plus` icon MUST appear above the table.
  When `canCreate` is `false` or `createUrl` is absent, the button MUST NOT be rendered.

- **FR-006**: An action column MUST be appended as the last column. Each row MUST render:
  - An Edit button (`.btn.btn-outline-secondary.btn-sm`) when `canUpdate` is `true` and
    `editUrl` is provided.
  - A Delete button (`.btn.btn-outline-danger.btn-sm`) when `canDelete` is `true` and
    `deleteUrl` is provided.
  - Nothing if neither condition is met.

- **FR-007**: The table wrapper MUST be a `<div class="card table-card">` containing the
  DataTables-rendered `<table>`. Column headers MUST receive the `.table th` style
  (uppercase, `var(--text-secondary)` color).

- **FR-008**: Clicking a Delete button MUST open a Bootstrap 5 modal (not `window.confirm()`)
  that names the object type (`objectName`), contains a danger-styled confirm button, and
  a secondary Cancel button.

- **FR-009**: On delete confirmation, the solution MUST POST to `deleteUrl` (with the row
  ID substituted) and include the `RequestVerificationToken` from the page as a request
  header. On success (2xx), it MUST remove the row and redraw the table. On failure, it
  MUST display an error alert styled with design tokens.

- **FR-010**: The anti-forgery token MUST be read from the hidden
  `input[name="__RequestVerificationToken"]` element that ASP.NET Core injects via
  `@Html.AntiForgeryToken()`. The solution MUST warn in the console if the token element
  is not found rather than sending a tokenless request.

- **FR-011**: While an AJAX request is in flight, the table body MUST display a loading
  indicator. When the response returns zero records, a custom empty-state message MUST be
  displayed. When the AJAX request fails, an error state with a Retry button MUST be
  displayed.

- **FR-012**: All text strings shown by the solution (empty state, error state, delete
  modal labels) MUST be configurable via an optional `messages` property on the config
  object, with sensible English defaults.

- **FR-013**: The solution MUST emit a custom DOM event `dtable:deleted` on the table
  element after a successful delete, carrying `{ id }` in `detail`, so pages can attach
  custom post-delete behaviour.

- **FR-014**: Null or undefined column values MUST render as an em-dash `—` rather than
  the literal string "null" or a blank cell.

### Key Entities

- **DataTable Config**: The configuration object passed to `AppDataTable.init()`.
  Key attributes: `tableId`, `ajaxUrl`, `columns[]`, `permissions{}`, URLs, `objectName`,
  optional `messages{}`.

- **DataTable Response (server)**: JSON returned by the controller helper.
  Key attributes: `draw` (int), `recordsTotal` (int), `recordsFiltered` (int),
  `data` (array of row objects).

- **DataTable Request (server)**: Query parameters sent by the client.
  Key attributes: `draw`, `start`, `length`, `search.value`, `order[0].column`,
  `order[0].dir`.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Any new management page can produce a fully functional, paginated,
  searchable, permission-aware DataTable by adding one `<table>` element and calling
  `AppDataTable.init()` with a config object of 8 or fewer properties — requiring zero
  additional JavaScript.

- **SC-002**: Create, Edit, and Delete buttons are absent from the DOM (not merely hidden)
  when the corresponding permission flag is `false`, verifiable by inspecting the
  rendered HTML.

- **SC-003**: A delete operation completes (request sent, row removed, table redrawn)
  within 1 second of the user clicking the modal Confirm button under normal network
  conditions.

- **SC-004**: The loading state, empty state, and error state each render correctly and
  are visually consistent with the design system (no raw DataTables default UI visible)
  across all three states.

- **SC-005**: The solution works identically in both `dir="ltr"` and `dir="rtl"` layout
  modes with no additional configuration — toolbar, buttons, and pagination all mirror.

- **SC-006**: The `RequestVerificationToken` is included on every delete POST; attempts
  to omit it are caught and logged before any request is sent.

---

## Assumptions

- Bootstrap 5.3 and jQuery (DataTables dependency) are already loaded on every admin page
  via `_Layout.cshtml`; `AppDataTable` does not bundle or load them.
- The DataTables.net library (1.13+) is loaded before `datatable.js`; the solution does
  not perform a version check.
- Every management page that uses `AppDataTable.init()` includes
  `@Html.AntiForgeryToken()` in its Razor view, producing the hidden token input.
- Server endpoints follow the standard DataTables server-side protocol (draw/start/length/
  search/order parameters) and return the standard response shape.
- Edit navigation uses a full-page redirect to `editUrl` (with the row ID appended or
  substituted), not an in-page modal — modal editing is out of scope for this phase.
- The generic `DataTableHelper.cs` controller helper processes `IQueryable<T>` and handles
  filtering on string properties only by default; numeric and date filtering is not
  required for this phase.
- The `objectName` config property is used solely in dialog labels (e.g., "Delete Role?");
  it is not used for permission lookups.
- Status-column badge rendering (`.badge-soft`) is the responsibility of each page's
  column `render` callback, not of `AppDataTable` itself; the solution provides a
  documented hook point.
- RTL support requires only that the solution uses Bootstrap utility classes and design
  token variables; it does not ship its own RTL-specific CSS.
