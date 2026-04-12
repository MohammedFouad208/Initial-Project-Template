# Contracts: Role-Permission Management (Phase 8)

**Branch**: `008-role-permission-management`  
**Produced by**: `/speckit.plan` — Phase 1

These are the MVC route contracts exposed by `RolePermissionsController`.

---

## GET /RolePermissions/{roleId}

**Action**: `RolePermissionsController.Index(string roleId)`  
**Auth**: `[Authorize]` + `[HasPermission("Role", "AssignPermissions")]`

**Route Parameter**:
| Parameter | Type | Description |
|---|---|---|
| `roleId` | `string` (GUID) | The ID of the role to manage permissions for |

**Success Response**: Renders `Views/RolePermissions/Index.cshtml` with a fully populated `RolePermissionsViewModel`.

**Error Responses**:
| Condition | Response |
|---|---|
| `roleId` is not a valid GUID or role does not exist | `404 Not Found` |
| Authenticated user lacks `Role → AssignPermissions` | `403 Forbidden` (custom `Error403.cshtml`) |
| User is not authenticated | Redirect to Login |

---

## POST /RolePermissions/Save

**Action**: `RolePermissionsController.Save(string roleId, List<string> selectedPermissions)`  
**Auth**: `[Authorize]` + `[HasPermission("Role", "AssignPermissions")]` + `[ValidateAntiForgeryToken]`

**Form Fields**:
| Field | Type | Description |
|---|---|---|
| `roleId` | `string` (hidden) | The role being updated |
| `selectedPermissions` | `string[]` (checkboxes) | Each value: `"ObjectName\|FunctionName"` |
| `__RequestVerificationToken` | `string` (hidden) | CSRF token |

**Behavior**: Replaces all `RolePermission` records for the given role with the submitted set. Empty `selectedPermissions` is valid and results in all permissions being removed.

**Success Response**: Redirect to `GET /RolePermissions/{roleId}` with `TempData["Success"]` set to a confirmation message.

**Error Responses**:
| Condition | Response |
|---|---|
| `roleId` is not a valid GUID or role does not exist | `404 Not Found` |
| Service throws | Set `TempData["Error"]`, redirect back to matrix |
| Authenticated user lacks `Role → AssignPermissions` | `403 Forbidden` |
| Missing or invalid CSRF token | `400 Bad Request` |
