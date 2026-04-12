# Research: PHASE 6 — User Management

**Phase**: 0 — Pre-Design Research  
**Branch**: `006-user-management`  
**Date**: 2026-04-09  
**Purpose**: Resolve all NEEDS CLARIFICATION items and establish best-practice decisions before Phase 1 design

---

## R-001 — DataTables.net Server-Side Parameter Binding in ASP.NET Core MVC

**Question**: How should the `UsersController.GetData` action bind the DataTables.net server-side request parameters (`draw`, `start`, `length`, `search[value]`, `order[0][column]`, `order[0][dir]`) in ASP.NET Core MVC?

**Decision**: Introduce a shared `DataTableRequest` model in `AdminTemplate.Web/Models/DataTableRequest.cs` with `[FromQuery]` binding. The generic DataTable JS (Phase 5) already passes these as query parameters via `method: 'GET'`. The controller action returns a `DataTableResponse<T>` plain object serialized as JSON.

**Rationale**: ASP.NET Core's model binder does not natively understand the nested DataTables parameter naming convention (`columns[0][data]`, `order[0][column]`). Using a flat DTO that captures `draw`, `start`, `length`, `search` (mapped from `search[value]`), `sortColumn`, and `sortDirection` is the established pattern for MVC DataTables integration. The infrastructure helper `DataTableHelper.GetDataTableResponse<T>` (Phase 5 task 5.6) handles the generic slicing; `GetData` only applies domain-specific filtering before delegating.

**Alternatives considered**:
- NuGet `DatatableJS` / `DataTables.AspNet` packages — rejected because they add a third-party dependency for trivial parameter binding; a lightweight DTO is sufficient.
- POST instead of GET — rejected because existing generic `datatable.js` is configured GET; changing that would break Phase 5 contract.

---

## R-002 — Deactivated User Login Rejection

**Question**: How should the application prevent a user with `IsActive = false` from logging in? The `AccountController` uses ASP.NET Core Identity's `SignInManager.PasswordSignInAsync`, which does not natively check custom fields like `IsActive`.

**Decision**: Override the `UserClaimsPrincipalFactory` or implement a custom `IUserValidator<ApplicationUser>` that checks `IsActive` before sign-in succeeds. The preferred approach for MVC cookie auth is to add a custom check in the `Login` POST action immediately after `SignInManager.PasswordSignInAsync` succeeds — if the resolved user has `IsActive == false`, call `SignInManager.SignOutAsync()` and return a `ModelState` error.

**Rationale**: Injecting the check at the controller level (Phase 2 `AccountController`) is the least-invasive, most transparent approach. It does not require overriding Identity internals and is clearly visible to future maintainers. The check is: `var user = await _userManager.FindByEmailAsync(model.Email); if (user is null || !user.IsActive) → fail with "Account is inactive"`.

**Alternatives considered**:
- Custom `IUserValidator` — overly complex; validators are for the creation/update pipeline, not sign-in.
- `CanSignInAsync` override via `IUserClaimsPrincipalFactory` — applies to claims generation, not sign-in authorization.
- `SecurityStampValidator` / `SignInManager` override — possible but requires subclassing Identity internals, violating simplicity.

**Impact on this phase**: `AccountController` is a Phase 2 artifact. Phase 6 only needs to ensure `SetActiveAsync` in `UserService` correctly sets `IsActive`. The login guard is documented here so Phase 9 (Polish) can add the `AccountController` patch if not already done.

---

## R-003 — Avatar Initials Rendering Strategy

**Question**: The spec requires each row in the Users list to show an initials circle avatar (consistent with the topnav avatar pattern). Should initials be rendered server-side in the ViewModel/DTO or client-side in the DataTable column renderer?

**Decision**: Render initials client-side inside the DataTable column renderer function. The `GetData` endpoint returns `fullName` in the JSON. The column definition for the name column provides a `render` function that extracts the first letter of each word (up to 2) and wraps them in `<span class="topnav-avatar topnav-avatar-sm">`.

**Rationale**: Server-side computation of initials would require adding a derived property to `UserDto`, which is unnecessary allocation. The DataTable column renderer already has the `fullName` value and can compute initials with a one-liner: `name.split(' ').map(w=>w[0]).slice(0,2).join('').toUpperCase()`. This matches exactly the avatar pattern used in `_Topnav.cshtml`.

**Alternatives considered**:
- `UserDto.Initials` computed property — adds noise to a DTO; initials are a pure display concern.
- CSS `::before` pseudo-element with `content: attr(data-initials)` — requires adding a `data-initials` attribute to the DOM; less readable.

---

## R-004 — Role Assignment: Replace-All vs Incremental Diff

**Question**: When editing a user's roles, should the service diff the new vs old role set and add/remove only changed roles, or simply remove all current roles and re-add the new set?

**Decision**: Replace-all: `await _userManager.RemoveFromRolesAsync(user, currentRoles)` followed by `await _userManager.AddToRolesAsync(user, newRoles)`. Execute both in a logical unit.

**Rationale**: The role set per user is small (typically 1–3 roles). Diffing adds complexity with no measurable performance benefit. Replace-all is idempotent, testable, and is the standard pattern for Identity role management in admin panels of this scale. The `UserManager` handles its own concurrency stamps.

**Alternatives considered**:
- Incremental diff (add new, remove removed) — overengineered for the scale; introduces ordering bugs if intermediate persist fails.
- Storing roles outside Identity (`UserRoles` custom table) — violates Constitution Principle I (use Identity's built-in tables).

---

## R-005 — ID Type in JSON Responses (`Guid` vs `string`)

**Question**: `ApplicationUser` inherits from `IdentityUser<Guid>`, so `Id` is a `Guid`. The DataTable's `editUrl` and `deleteUrl` append the row ID as a query/route parameter. How should the ID be serialized in `GetData` JSON?

**Decision**: Serialize `Id` as a **lowercase hyphenated GUID string** (default `System.Text.Json` behavior for `Guid`). In `UsersController`, route parameters typed as `string id` and passed to `UserService.GetByIdAsync(id)` where they are parsed via `Guid.Parse(id)`. Never return raw `Guid` binary; always string.

**Rationale**: DataTables.net constructs URLs by string concatenation. A GUID string is URL-safe and unambiguous. `System.Text.Json` serializes `Guid` as `"xxxxxxxx-xxxx-..."` by default — no custom converter needed.

**Alternatives considered**:
- Numeric surrogate key — rejected; `IdentityUser<Guid>` uses GUIDs by design per the established entity model.
- `[JsonConverter(typeof(GuidConverter))]` for short IDs — unnecessary complexity.

---

## R-006 — `CreateUserDto` Missing `IsActive` Field

**Question**: The existing `CreateUserDto` record does not include an `IsActive` field, but the spec FR-003 requires the Create form to capture an initial active state.

**Decision**: Add `bool IsActive = true` as an optional parameter with a default of `true` to `CreateUserDto`. This is a non-breaking additive change to the existing DTO record.

**Rationale**: The Create form provides an "Active" toggle per the spec. Defaulting to `true` preserves existing callers (e.g., the data seeder). The change is backward-compatible.

**Alternatives considered**:
- Not adding to DTO and always defaulting to active at creation — violates FR-003.
- New `CreateUserWithStatusDto` — unnecessary proliferation of DTOs.

---

## R-007 — Activate/Deactivate UX: AJAX vs Form POST

**Question**: FR-007 states deactivation must happen "without leaving the list view." Should the toggle be an AJAX `fetch` call from the DataTable row, or a full-page form POST?

**Decision**: AJAX `fetch` POST to `/Users/ToggleActive/{id}` with the CSRF token injected from the `__RequestVerificationToken` cookie/field. The response is `{ success: true, isActive: false }`. The DataTable row's status cell is updated in-place via the `DataTable.row().invalidate()` or direct DOM manipulation.

**Rationale**: The spec explicitly requires no page reload (FR-007, SC-006 < 1s round-trip). AJAX is the only mechanism that satisfies this. The generic `datatable.js` design (Phase 5) already establishes a pattern for AJAX interactions with CSRF injection (task 5.8). This follows the same pattern.

**Alternatives considered**:
- DataTable action column "Deactivate" link with `data-url` attribute → AJAX call — same thing, preferred to keep toggle in the status badge cell itself for better UX.
- Full form POST with `[ValidateAntiForgeryToken]` redirect — violates FR-007 (requires page reload).

---

## R-008 — `GetPagedAsync` DataTable Query Shape

**Question**: The `IUserService.GetPagedAsync` signature uses `pageIndex/pageSize` (page-based). DataTables.net uses `start/length` (offset-based). How to reconcile?

**Decision**: Accept `start` and `length` from the DataTable request and convert: `pageIndex = start / length`, `pageSize = length` before calling `GetPagedAsync`. The conversion happens in `UsersController.GetData`, not in the service layer. This keeps the service API page-based (clean), while the controller adapter handles protocol translation.

**Rationale**: The service interface should be agnostic of the HTTP transport protocol. `pageIndex/pageSize` is a cleaner abstraction for reuse (e.g., API endpoints, other callers). The DataTable offset→page conversion is a single arithmetic operation and belongs at the transport boundary.

**Alternatives considered**:
- Change `IUserService.GetPagedAsync` to accept `start/length` — pollutes the service interface with HTTP protocol concerns.
- Change DataTables to use page-based parameters — would require modifying the shared `datatable.js`, breaking Phase 5 contract.

---

## Summary of Decisions

| ID | Decision |
|---|---|
| R-001 | Flat `DataTableRequest` DTO bound `[FromQuery]`; returns `DataTableResponse<T>` JSON |
| R-002 | Check `IsActive` in `AccountController.Login` post-sign-in; Phase 6 only implements `SetActiveAsync` |
| R-003 | Initials rendered client-side in DataTable column renderer from `fullName` |
| R-004 | Replace-all role assignment: remove all, then add new set |
| R-005 | Serialize GUID IDs as lowercase hyphenated strings (default `System.Text.Json`) |
| R-006 | Add `bool IsActive = true` optional param to `CreateUserDto` |
| R-007 | AJAX fetch POST to `/Users/ToggleActive/{id}`; in-place status badge update |
| R-008 | DataTable `start/length` → `pageIndex/pageSize` conversion in controller, not service |
