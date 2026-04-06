# Data Model: Admin Layout & Dashboard

**Feature Branch**: `003-admin-layout-dashboard`  
**Date**: 2026-04-02

---

## New Entities

Phase 3 introduces **no new database entities**. The admin shell is a purely presentational layer; all data it queries comes from existing Phase 1 entities.

---

## View Model

### `DashboardViewModel`

**Location**: `AdminTemplate.Web/ViewModels/DashboardViewModel.cs`  
**Purpose**: Carries computed stat counts from Application services to the Dashboard view.

| Property | Type | Description |
|----------|------|-------------|
| `TotalUsers` | `int` | Count of all registered `ApplicationUser` records |
| `TotalRoles` | `int` | Count of all `ApplicationRole` records |
| `ActiveUsers` | `int` | Count of `ApplicationUser` records where `IsActive == true` and account is not locked out |

**Notes**:
- "Active Sessions" from the spec is renamed to **"Active Users"** per research decision R-003.
- All three counts are populated by `DashboardController.Index` before the View is returned; they are never hardcoded.

---

## Existing Entities Consumed (Read-Only)

| Entity | Source Project | Fields Read |
|--------|----------------|-------------|
| `ApplicationUser` | `AdminTemplate.Domain` | `Id`, `IsActive`, `LockoutEnd` |
| `ApplicationRole` | `AdminTemplate.Domain` | `Id` |

No migrations are required for this phase.

---

## Sidebar Navigation Items (Static in Phase 3)

These are not persisted — they are hardcoded HTML in `_Sidebar.cshtml`. Permission-based dynamic hiding is deferred to Phase 9.

| Label | Icon (FA class) | Controller | Action |
|-------|----------------|------------|--------|
| Dashboard | `fa-gauge-high` | Dashboard | Index |
| Users | `fa-users` | Users | Index |
| Roles | `fa-shield-halved` | Roles | Index |
| Role Permissions | `fa-key` | RolePermissions | Index |
