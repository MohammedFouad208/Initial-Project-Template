# Tasks: Generic DataTable Solution

**Branch**: `005-generic-datatable`
**Input**: Design documents from `specs/005-generic-datatable/`
**Prerequisites**: plan.md ✅ spec.md ✅ research.md ✅ data-model.md ✅ contracts/ ✅ quickstart.md ✅

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no unresolved dependencies)
- **[Story]**: User story this task belongs to (US1–US4)
- All file paths are absolute from the repository root

---

## Phase 1: Setup

**Purpose**: Wire DataTables.net library into the layout so all subsequent phases can be tested in the browser

- [ ] T001 Add DataTables.net CSS and JS CDN links (or NuGet/npm bundle) to `AdminTemplate.Web/Views/Shared/_Layout.cshtml` after Bootstrap CSS/JS

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The two Application-layer DTOs and the Web-layer helper that every user story depends on. No user story task can be implemented until these are in place.

**⚠️ CRITICAL**: US1 through US4 all depend on `DataTableRequest`, `DataTableResponse<T>`, and `DataTableHelper` being present.

- [ ] T002 [P] Create `AdminTemplate.Application/DTOs/DataTableRequest.cs` with nested `DataTableSearch`, `DataTableOrder`, and `DataTableColumn` classes per data-model.md
- [ ] T003 [P] Create `AdminTemplate.Application/DTOs/DataTableResponse.cs` as a generic class `DataTableResponse<T>` with `Draw`, `RecordsTotal`, `RecordsFiltered`, `Data` properties decorated with `[JsonPropertyName]` attributes for camelCase serialisation
- [ ] T004 Create `AdminTemplate.Web/Helpers/DataTableHelper.cs` static class with `GetDataTableResponse<T>(IQueryable<T> source, DataTableRequest request)` method implementing: total count → string filter via reflection → filtered count → sort via expression tree → paging → return DTO (depends on T002, T003)

**Checkpoint**: DTOs compile, `DataTableHelper` compiles. `dotnet build` passes with 0 errors.

---

## Phase 3: User Story 1 — Core Table Init (Priority: P1) 🎯 MVP

**Goal**: Any page can call `AppDataTable.init(config)` and get a fully functional server-side DataTable rendering columns, pagination, search, and sort.

**Independent Test**: Navigate to `/Dashboard` (or any page), open browser DevTools, call `AppDataTable.init({ tableId: 'testTable', ajaxUrl: '/test', columns: [{data:'id', title:'ID'}], permissions: {canCreate:false, canUpdate:false, canDelete:false} })` in the console — verify DataTables initialises without JS errors and fires an AJAX request to `/test`.

- [ ] T005 [P] [US1] Create `AdminTemplate.Web/wwwroot/js/datatable.js` — scaffold `window.AppDataTable = {}` global object with `init(config)` method stub; merge default `messages` into `config`
- [ ] T006 [US1] Implement DataTables.net initialisation inside `AppDataTable.init()`: configure `serverSide: true`, `ajax` with `ajaxUrl`, `columns` from config, `language.emptyTable` from `config.messages.emptyTable` in `AdminTemplate.Web/wwwroot/js/datatable.js`
- [ ] T007 [US1] Implement toolbar rendering in `AppDataTable.init()`: inject a `<div class="d-flex justify-content-between align-items-center mb-3">` wrapper above the table containing the DataTables search/length controls in `AdminTemplate.Web/wwwroot/js/datatable.js`
- [ ] T008 [US1] Implement null/undefined column value guard: wrap every column definition with a default `render` function that returns `'—'` when `data == null || data === undefined`, falling back to any caller-supplied `render` in `AdminTemplate.Web/wwwroot/js/datatable.js`

**Checkpoint**: US1 independently testable — `AppDataTable.init()` renders a table, fires server-side AJAX, handles null values.

---

## Phase 4: User Story 2 — Permission-Aware Toolbar & Action Column (Priority: P1)

**Goal**: Create button and per-row Edit/Delete buttons are absent from the DOM when the corresponding permission flag is `false`; present and correctly styled when `true`.

**Independent Test**: Inspect the rendered DOM after calling `AppDataTable.init()` with `canCreate: false` — assert no `<button>` or `<a>` element with class `btn-primary` exists in the toolbar area. Repeat with `canCreate: true` and assert the Create button is present.

- [ ] T009 [US2] Implement Create button rendering in the toolbar inside `AppDataTable.init()`: conditionally append `.btn.btn-primary.btn-sm` with `<i class="fa fa-plus">` icon and a redirect click handler to `config.createUrl` — only when `permissions.canCreate === true && config.createUrl` in `AdminTemplate.Web/wwwroot/js/datatable.js`
- [ ] T010 [US2] Implement action column appended as last column in `AppDataTable.init()`: `orderable: false`, `searchable: false`; per-row `render` callback that conditionally builds Edit button (`.btn.btn-outline-secondary.btn-sm`) and/or Delete button (`.btn.btn-outline-danger.btn-sm`) based on `permissions.canUpdate` / `permissions.canDelete` and presence of `editUrl` / `deleteUrl`; both absent from DOM when conditions not met in `AdminTemplate.Web/wwwroot/js/datatable.js`
- [ ] T011 [US2] Implement Edit button click handler: replace `:id` in `config.editUrl` with `row.id`; navigate via `window.location.href` in `AdminTemplate.Web/wwwroot/js/datatable.js`

**Checkpoint**: US2 independently testable — DOM audit confirms buttons absent/present per permission flag.

---

## Phase 5: User Story 3 — Delete Confirmation Modal (Priority: P2)

**Goal**: Clicking Delete opens a Bootstrap 5 modal (not `window.confirm()`); confirmation POSTs with CSRF token; success removes row and redraws; failure shows error alert.

**Independent Test**: Click a Delete button on a table row; assert Bootstrap modal opens with correct body text; click Cancel; assert no network request was made; click Delete again → Confirm; assert a `POST` request was sent to `deleteUrl` with `RequestVerificationToken` header; assert the row is removed from the DOM.

- [ ] T012 [P] [US3] Create `AdminTemplate.Web/Views/Shared/_DeleteConfirmModal.cshtml` — Bootstrap 5 modal markup with `id="deleteConfirmModal"`, `id="deleteConfirmBody"`, `id="deleteConfirmBtn"` (`.btn.btn-danger`), and a `.btn.btn-secondary` Cancel button per data-model.md DOM structure
- [ ] T013 [US3] Implement Delete button click handler in the action column render callback: populate `#deleteConfirmBody` text using `config.messages.deleteBody` with `{objectName}` substituted; store resolved `deleteUrl` and `row.id` on `#deleteConfirmBtn` as `data-delete-url` / `data-record-id`; call `bootstrap.Modal.getOrCreateInstance(...)show()` in `AdminTemplate.Web/wwwroot/js/datatable.js` (depends on T012)
- [ ] T014 [US3] Implement `#deleteConfirmBtn` click handler: read `RequestVerificationToken` from `input[name="__RequestVerificationToken"]`; abort with `console.warn` if missing; POST to `data-delete-url` with token header; on 2xx hide modal, remove row, redraw table, dispatch `dtable:deleted` custom event; on non-2xx hide modal, inject `.alert` with `border-inline-start: 4px solid var(--danger)` above table in `AdminTemplate.Web/wwwroot/js/datatable.js`

**Checkpoint**: US3 independently testable — full delete flow works with modal, CSRF token, and row removal.

---

## Phase 6: User Story 4 — Loading, Empty & Error States (Priority: P3)

**Goal**: While data is fetching a spinner is visible; zero-result responses show a custom empty-state message; AJAX failures show an error panel with a Retry button — all styled to design tokens only.

**Independent Test**: In browser DevTools, throttle network and navigate to a DataTable page — assert spinner appears in the table body. Simulate a 500 response — assert error panel with Retry button renders. Click Retry and restore network — assert rows appear.

- [ ] T015 [US4] Implement loading state in `AppDataTable.init()`: hook into DataTables `preXhr` to show `<span class="fa fa-spinner fa-spin">` centred in `tbody` and disable search/pagination inputs; hook into `xhr` callback to clean up loading indicator in `AdminTemplate.Web/wwwroot/js/datatable.js`
- [ ] T016 [US4] Implement error state in the `xhr` callback: detect non-2xx `e.xhr.status`; replace table body content with an error panel `<div class="alert" style="border-inline-start:4px solid var(--danger);background:var(--danger-light)">` containing `config.messages.errorTitle` and a Retry button (`.btn.btn-outline-danger.btn-sm`) that calls `table.ajax.reload()` in `AdminTemplate.Web/wwwroot/js/datatable.js`
- [ ] T017 [US4] Override DataTables `language.emptyTable` to render custom `.dt-empty-state` markup centred in `tbody` using `var(--text-secondary)` colour when the server response contains zero records in `AdminTemplate.Web/wwwroot/js/datatable.js`

**Checkpoint**: US4 independently testable — all three states render correctly and match design system tokens.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Wire the layout, validate the full integration end-to-end, and ensure RTL and edge-case behaviour.

- [ ] T018 [P] Add `<partial name="_DeleteConfirmModal" />` reference comment to `AdminTemplate.Web/Views/Shared/_Layout.cshtml` (or document the pattern in quickstart.md — layout-level inclusion vs per-view inclusion decision)
- [ ] T019 [P] Verify DataTables CSS overrides in `AdminTemplate.Web/wwwroot/css/site.css`: ensure `.dataTables_wrapper` inputs and buttons inherit design token styles (border-radius, `var(--border-color)`, focus ring); add overrides if any DataTables default styles leak through
- [ ] T020 Validate RTL parity: load a DataTable page with `dir="rtl"` active and verify toolbar layout, action column buttons, and pagination controls mirror correctly without any `left`/`right` CSS property violations

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1 (Setup: T001)
    └── Phase 2 (Foundational: T002–T004)
            └── Phase 3 (US1: T005–T008) ← MVP: deliver here
                    └── Phase 4 (US2: T009–T011)
                            └── Phase 5 (US3: T012–T014)
                                    └── Phase 6 (US4: T015–T017)
                                                └── Phase 7 (Polish: T018–T020)
```

### User Story Dependencies

- **US1 (P1)**: Requires Foundational complete (T002–T004)
- **US2 (P1)**: Requires US1 complete — action column builds on `init()` structure
- **US3 (P2)**: Requires US2 complete — delete handler extends action column render callback
- **US4 (P3)**: Requires US1 complete — hooks into DataTables XHR callbacks added in T006

### Parallel Opportunities per Story

**Phase 2 (Foundational)**:
```
T002 (DataTableRequest.cs)  ─┐
T003 (DataTableResponse.cs) ─┴─► T004 (DataTableHelper.cs)
```

**Phase 5 (US3)**:
```
T012 (_DeleteConfirmModal.cshtml) ─┐
                                    ├─► T013 (delete click handler) ─► T014 (confirm handler)
```

**Phase 7 (Polish)**:
```
T018 (layout wiring) ─┐
T019 (CSS overrides)  ─┴─► T020 (RTL validation)
```

---

## Implementation Strategy

### MVP Scope (Phases 1–3 + Phase 4)

Deliver US1 + US2 first:
- `DataTableRequest.cs`, `DataTableResponse.cs`, `DataTableHelper.cs`
- `datatable.js` with `init()`, server-side AJAX, toolbar, action column, permission enforcement

This unblocks Phase 6 (User Management), Phase 7 (Role Management), and Phase 8
(Role-Permission Matrix) to begin. US3 and US4 can be layered in before or during
those phases.

### Incremental Delivery

1. **Increment 1** (MVP): T001 → T002 → T003 → T004 → T005 → T006 → T007 → T008 → T009 → T010 → T011
2. **Increment 2**: T012 → T013 → T014
3. **Increment 3**: T015 → T016 → T017
4. **Polish**: T018, T019 (parallel) → T020

---

## Task Summary

| Phase | Story | Tasks | Parallelisable |
|---|---|---|---|
| Phase 1: Setup | — | T001 | — |
| Phase 2: Foundational | — | T002–T004 | T002, T003 in parallel |
| Phase 3: US1 Core Table | US1 (P1) | T005–T008 | T005 independent |
| Phase 4: US2 Permissions | US1+US2 (P1) | T009–T011 | — |
| Phase 5: US3 Delete Modal | US3 (P2) | T012–T014 | T012 in parallel |
| Phase 6: US4 UX States | US4 (P3) | T015–T017 | — |
| Phase 7: Polish | — | T018–T020 | T018, T019 in parallel |
| **Total** | | **20 tasks** | **6 parallel opportunities** |
