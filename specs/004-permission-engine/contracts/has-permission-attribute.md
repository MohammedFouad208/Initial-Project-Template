# Contract: `HasPermissionAttribute` Filter

**Feature Branch**: `004-permission-engine`  
**Date**: 2026-04-06  
**Scope**: Custom authorization filter applied to controller actions in the Web layer.

---

## Declaration

```csharp
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class HasPermissionAttribute : Attribute, IAsyncAuthorizationFilter
```

**Namespace**: `AdminTemplate.Web.Filters`  
**Location**: `AdminTemplate.Web/Filters/HasPermissionAttribute.cs`

---

## Constructor

```csharp
public HasPermissionAttribute(string objectName, string functionName)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `objectName` | `string` | The permission object name (e.g., `"Employee"`, `"Role"`) — must match an entry in `permissions.json` |
| `functionName` | `string` | The permission function name (e.g., `"Browse"`, `"Create"`) — must match a function defined for the given object in `permissions.json` |

---

## Usage

```csharp
// On a single action:
[HasPermission("Employee", "Create")]
public IActionResult Create() { ... }

// On an entire controller:
[HasPermission("User", "Browse")]
public class UsersController : Controller { ... }

// Multiple requirements (AND logic — all must pass):
[HasPermission("Employee", "Update")]
[HasPermission("Employee", "Browse")]
public IActionResult Edit(Guid id) { ... }
```

---

## Authorization Logic

Executed in `OnAuthorizationAsync(AuthorizationFilterContext context)`:

```
1. IF user is NOT authenticated
   → context.Result = ChallengeResult()   [triggers login redirect]
   → return

2. Extract userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier))
   IF parse fails
   → context.Result = ForbidResult()      [403 — identity corrupted]
   → return

3. Resolve IPermissionService from context.HttpContext.RequestServices

4. hasPermission = await permissionService.UserHasPermissionAsync(userId, objectName, functionName)

5. IF NOT hasPermission
   → context.Result = ViewResult { ViewName = "~/Views/Shared/Error403.cshtml", StatusCode = 403 }
   → return

6. ELSE — allow request to proceed (no result set)
```

---

## Response Behaviour

| Scenario | HTTP Status | User Experience |
|----------|-------------|-----------------|
| Not logged in | 302 (redirect) | Redirected to Login page |
| Logged in, no permission | 403 Forbidden | Styled `Error403.cshtml` view rendered with admin layout |
| Logged in, has permission | — | Request proceeds normally |

---

## `Error403.cshtml` View

**Location**: `AdminTemplate.Web/Views/Shared/Error403.cshtml`  
**Layout**: `_Layout.cshtml` (admin layout with sidebar + topnav)  
**Content**: "Access Denied" heading, descriptive message, "Go to Dashboard" link.  
**HTTP Status**: Set by the `ViewResult.StatusCode = 403` from the filter.
