# Research: Permission Engine

**Feature Branch**: `004-permission-engine`  
**Date**: 2026-04-06  
**Purpose**: Resolve all NEEDS CLARIFICATION items and architectural unknowns identified in the Technical Context before design begins.

---

## R-001 — How `PermissionService` Resolves a User's Role IDs

**Question**: `IPermissionService.UserHasPermissionAsync(Guid userId, ...)` must determine which roles the user belongs to before it can check permissions. `IUserRepository` currently has no method for this. Should `PermissionService` inject `UserManager<ApplicationUser>` directly (a framework type), or should the Domain interface surface area be extended?

**Decision**: Extend `IUserRepository` with two new methods:
- `GetRoleNamesAsync(Guid userId): Task<IReadOnlyList<string>>` — returns the role names for a user (used for the SuperAdmin bypass).
- No role-ID based method is needed on `IUserRepository`.

Additionally, extend `IPermissionRepository` with a single-query method:
- `UserHasPermissionAsync(Guid userId, string objectName, string functionName): Task<bool>` — performs a direct SQL join of `AspNetUserRoles → RolePermissions` and returns the boolean result in one round trip.

`PermissionService.UserHasPermissionAsync` then becomes:
1. Call `IUserRepository.GetRoleNamesAsync(userId)` → if "SuperAdmin" is in the list → `return true`.
2. Otherwise call `IPermissionRepository.UserHasPermissionAsync(userId, objectName, functionName)`.

**Rationale**: The Application layer must depend only on Domain interfaces, not on framework types like `UserManager<T>`. Centralising the SQL join in `PermissionRepository` avoids N+1 queries (one query per role ID) and is the most efficient path. Two clean interface methods are a smaller surface extension than introducing a framework dependency into the Application layer.

**Alternatives considered**:
- **Inject `UserManager<ApplicationUser>` into `PermissionService`**: Works but violates the constitution's rule that Application depends only on Domain. Rejected.
- **N+1 loop**: For each role ID, call `IPermissionRepository.ExistsAsync(roleId, ...)`. Works functionally but generates one DB query per role — rejected on performance grounds.
- **Add `GetRoleIdsAsync` to `IUserRepository`**: Equivalent complexity to `GetRoleNamesAsync` but less useful (role IDs not needed anywhere else in this phase). Rejected in favour of the more widely useful name-based lookup.

---

## R-002 — SuperAdmin Bypass Mechanism

**Question**: The spec assumes SuperAdmin bypasses all permission checks. There is no explicit mechanism defined. Should this be a claim on the user token, a hard-coded role name check, or an `IsSuperAdmin` flag on `ApplicationUser`?

**Decision**: Hard-coded role name check in `PermissionService`. Before calling the repository, the service calls `IUserRepository.GetRoleNamesAsync(userId)` and returns `true` immediately if the list contains `"SuperAdmin"`.

The role name `"SuperAdmin"` is the canonical string used by `DataSeeder` (Phase 1). This value should be defined as a constant:

```csharp
// AdminTemplate.Application/Services/PermissionService.cs
private const string SuperAdminRoleName = "SuperAdmin";
```

**Rationale**: The constitution says roles are seeded from JSON configuration but role *names* (like "SuperAdmin") are part of the domain model and acceptable as a named constant in the service. A claim-based check would require changes to the token pipeline (Phase 2 work). An `IsSuperAdmin` flag would couple an Identity concern to the domain entity. The role name check is the simplest approach with the fewest dependencies.

**Alternatives considered**:
- **Claim-based check**: Would require adding a custom claim during sign-in — a Phase 2 concern that is already complete and would need to be re-opened. Rejected.
- **`IsSuperAdmin` flag on `ApplicationUser`**: Adds a domain field solely for a permission bypass — rejected; role membership already expresses this.
- **Policy-based check via ASP.NET Core Authorization**: Requires adding a policy and handler — introduces framework types into Application. Rejected.

---

## R-003 — `HasPermissionAttribute` Filter Implementation Approach

**Question**: Should `HasPermissionAttribute` be implemented as `IAsyncAuthorizationFilter` or as a resource filter, policy requirement, or something else? How should it handle (a) unauthenticated users and (b) authenticated users without the required permission?

**Decision**: Implement as `Attribute, IAsyncAuthorizationFilter`:
- **Unauthenticated** → `context.Result = new ChallengeResult()` — this triggers the configured authentication scheme's login redirect.
- **Authenticated but lacking permission** → `context.Result = new ViewResult { ViewName = "~/Views/Shared/Error403.cshtml", StatusCode = 403 }` — renders the styled 403 view directly.

Placement: `AdminTemplate.Web/Filters/HasPermissionAttribute.cs`.

**Rationale**: `IAsyncAuthorizationFilter` runs before action execution and before model binding — the correct stage for an authorization check. `ChallengeResult` correctly defers to the configured Identity cookie scheme for login redirect. Returning a `ViewResult` from the filter renders the admin layout without requiring middleware configuration (`UseStatusCodePagesWithReExecute`), keeping `Program.cs` changes minimal in this phase. Phase 9 will add broader error page routing.

**Alternatives considered**:
- **Custom `IAuthorizationRequirement` + `IAuthorizationHandler`**: The ASP.NET Core Authorization policy system is the most "correct" abstraction, but applying per-action parameters (`objectName`, `functionName`) requires dynamic policy registration via `IAuthorizationPolicyProvider` — significant boilerplate for no additional benefit at this scale. Rejected.
- **`UseStatusCodePagesWithReExecute` in `Program.cs`**: Works for any 403 response but requires setting up routing for the error page and modifying `Program.cs`. Deferred to Phase 9 which explicitly covers custom error pages.
- **Resource filter**: Runs after model binding — authorization should run before. Rejected on semantic grounds.

---

## R-004 — Permission View Helper Strategy

**Question**: How should Razor views check whether the current user has a specific permission (for conditional rendering of buttons)? Options include `@inject`, a base controller `ViewBag`, a TagHelper, or a ViewComponent.

**Decision**: Register `@inject AdminTemplate.Application.Interfaces.IPermissionService PermissionService` in `_ViewImports.cshtml` so the service is available globally to all views without per-view declarations. Complement with a static extension method `CurrentUserHasPermissionAsync(this IPermissionService, ClaimsPrincipal, string, string)` defined in `AdminTemplate.Application` — this hides the user-ID extraction boilerplate from view authors.

Views then use:
```csharp
@if (await PermissionService.CurrentUserHasPermissionAsync(User, "Employee", "Export"))
{
    <button>Export</button>
}
```

**Rationale**: `@inject` in `_ViewImports.cshtml` follows ASP.NET Core's documented pattern for service injection into views and avoids per-action ViewBag population. The extension method keeps the `ClaimsPrincipal`-to-Guid extraction in one place and out of view templates.

**Alternatives considered**:
- **Base controller `PermissionViewBag`**: Would require all controllers to inherit from a base class and call a helper method — adds coupling for every future controller. Rejected.
- **TagHelper `<permission-check object="Employee" function="Export">`**: More declarative but requires a TagHelper class, registration in `_ViewImports.cshtml`, and async attribute evaluation patterns — over-engineered for a boolean check. Deferred if needed later.
- **ViewComponent**: Correct for partial view fragments; overkill for a boolean check. Rejected.

---

## R-005 — No New Database Migration Required

**Question**: Does Phase 4 require any new migrations or schema changes?

**Decision**: **No new migration**. The `RolePermissions` table was created in Phase 1 with its full schema (including the unique composite index on `RoleId + ObjectName + FunctionName` and `ON DELETE CASCADE` FK to `AspNetRoles`). The `permissions.json` file is already present at `AdminTemplate.Web/Config/permissions.json`.

**Rationale**: Phase 4 adds only runtime logic (service implementation, filter, view helper) and new files (`HasPermissionAttribute.cs`, `Error403.cshtml`). The data model already supports all required operations.

**Alternatives considered**: None — the migration question is a factual check, not a choice.

---

## Resolution Summary

| Unknown | Resolution |
|---------|-----------|
| How PermissionService gets user roles | Extend `IUserRepository.GetRoleNamesAsync`; extend `IPermissionRepository.UserHasPermissionAsync` with direct JOIN query |
| SuperAdmin bypass mechanism | Role name constant check in `PermissionService` before DB query |
| HasPermissionAttribute approach | `IAsyncAuthorizationFilter`; `ChallengeResult` for unauth; `ViewResult(403)` for authorized-but-denied |
| 403 page rendering | `ViewResult` returned from filter — no middleware change needed in this phase |
| Permission view helper | `@inject` in `_ViewImports.cshtml` + extension method `CurrentUserHasPermissionAsync` |
| New migration needed | No — schema already provisioned in Phase 1 |
