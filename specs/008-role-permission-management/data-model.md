# Data Model: Role-Permission Management (Phase 8)

**Branch**: `008-role-permission-management`  
**Produced by**: `/speckit.plan` — Phase 1

---

## Existing Entities (No Changes)

### RolePermission *(AdminTemplate.Domain/Entities/RolePermission.cs)*

Represents a single granted function within an object for a specific role. Already exists and is fully implemented.

| Field | Type | Constraints |
|---|---|---|
| `Id` | `Guid` | PK (from BaseEntity) |
| `RoleId` | `Guid` | FK → AspNetRoles, required |
| `ObjectName` | `string` | Max 100 chars, required |
| `FunctionName` | `string` | Max 100 chars, required |
| `CreatedAt` | `DateTime` | From BaseEntity |

**State transitions**: Records are fully replaced on every Save operation. There is no incremental update.

---

## New ViewModel (Web Layer Only)

### RolePermissionsViewModel *(AdminTemplate.Web/ViewModels/RolePermissions/RolePermissionsViewModel.cs)*

Read-only input to the GET `Index` view. Carries everything the Razor view needs to render the matrix.

| Property | Type | Source | Purpose |
|---|---|---|---|
| `RoleId` | `string` | Route parameter | Hidden form field; passed back on POST |
| `RoleName` | `string` | `IRoleService.GetByIdAsync` | Page header and breadcrumb |
| `AllObjects` | `IReadOnlyList<PermissionObjectDto>` | `IPermissionProvider.GetAll()` | Drives matrix rows and column headers |
| `AssignedKeys` | `HashSet<string>` | `IPermissionService.GetRolePermissionsAsync` | Pre-computed set of `"ObjectName\|FunctionName"` for O(1) checkbox pre-check in Razor |

**Validation rules**: None — this is a read-only view model. The POST action model-binds `List<string>` directly.

---

## POST Binding Shape

The Save POST action receives:

| Binding | Source | Description |
|---|---|---|
| `string roleId` | Hidden form field | Identifies the role being updated |
| `List<string> selectedPermissions` | Checkbox collection | Each value is `"ObjectName\|FunctionName"` for every checked cell |

Each `selectedPermissions` entry is split on `|` to produce a `PermissionDto(ObjectName, FunctionName)` for passing to `IPermissionService.SaveRolePermissionsAsync`.

---

## Rendering Logic (Checkbox State)

For each cell at `[objectName][functionName]`:

```
isChecked = viewModel.AssignedKeys.Contains($"{objectName}|{functionName}")
```

Razor renders: `<input type="checkbox" name="selectedPermissions" value="@objectName|@functionName" @(isChecked ? "checked" : "") />`

---

## Entity Relationships (Unchanged)

```
AspNetRoles (1) ──── (N) RolePermissions
                         ├── ObjectName  (from permissions.json, not a FK)
                         └── FunctionName (from permissions.json, not a FK)

PermissionObjectDto (transient, from JSON)
├── Name
├── DisplayName
└── Functions: string[]
```

`ObjectName` and `FunctionName` are not foreign-keyed to a lookup table — they are free strings validated at the UI level against `permissions.json`. Orphaned records (object removed from JSON) remain in storage but are not displayed.
