# Quickstart: Role-Permission Management (Phase 8)

**Branch**: `008-role-permission-management`  
**Produced by**: `/speckit.plan` — Phase 1

---

## Prerequisites

Phases 1–7 must be complete and the application must be running:
- Database migrated and seeded (SuperAdmin user + roles + any existing permissions)
- `permissions.json` present at `AdminTemplate.Web/Config/permissions.json`

---

## Verify the feature end-to-end in 5 steps

### 1. Log in as SuperAdmin

Navigate to `/Account/Login` and sign in with the seeded SuperAdmin credentials.

### 2. Open the Roles list

Navigate to `/Roles`. Confirm the "Manage Permissions" button (key icon) appears in the Actions column for each role.

### 3. Open the Permission Matrix

Click the "Manage Permissions" button for any role (e.g., "Admin"). You are taken to `/RolePermissions/{roleId}`.

**Expect:**
- Page header: "Manage Permissions: Admin" (role name)
- Matrix table with one row per permission object from `permissions.json`
- Column headers = all unique function names across all objects
- Currently assigned permissions are pre-checked

### 4. Modify and Save

- Uncheck one checked permission
- Check one unchecked permission
- Click Save

**Expect:**
- Redirect back to the same matrix page
- Green success alert at the top
- Matrix reloads with the new checkbox state reflecting exactly what was saved

### 5. Verify bulk controls

- Click "Check All" for a row → all functions in that row check
- Click "Uncheck All" for the same row → all uncheck
- Click a column header checkbox → all cells in that column toggle

---

## Adding a new permission object (no code change)

1. Open `AdminTemplate.Web/Config/permissions.json`
2. Add a new object to the `PermissionObjects` array:
   ```json
   { "Name": "Invoice", "DisplayName": "Invoices", "Functions": ["Browse", "Create", "Export"] }
   ```
3. Restart the application (the `JsonPermissionProvider` reads at startup)
4. Open any role's permission matrix — the new "Invoices" row appears automatically

---

## Troubleshooting

| Symptom | Likely Cause | Fix |
|---|---|---|
| 403 on `/RolePermissions/{id}` | Logged-in user's role lacks `Role → AssignPermissions` | Assign that permission to the role in another role's matrix, or log in as SuperAdmin |
| Matrix shows no rows | `permissions.json` missing or malformed | Check `AdminTemplate.Web/Config/permissions.json` exists and is valid JSON |
| Save shows no change | Browser cached old page | Hard refresh (`Ctrl+Shift+R`) after save |
| 404 on matrix load | Invalid `roleId` in URL | Navigate from the Roles list to get a valid ID |
