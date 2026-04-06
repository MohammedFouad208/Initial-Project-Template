# Contracts: Admin Layout & Dashboard

**Feature Branch**: `003-admin-layout-dashboard`  
**Date**: 2026-04-02

---

## MVC Interface Contracts

Phase 3 exposes internal MVC routes consumed by the browser. There are no public APIs or service-to-service contracts in this phase.

---

## Controller Routes

### `DashboardController`

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `GET` | `/Dashboard/Index` (default route) | `[Authorize]` | Admin home page with stat cards |

### `AccountController` (addition to Phase 2)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `POST` | `/Account/Logout` | `[Authorize]` + `[ValidateAntiForgeryToken]` | Terminates session, redirects to `/Account/Login` |

---

## Layout Partial Rendering Contract

`_Layout.cshtml` renders the following partials. Each partial receives the ViewContext implicitly (no explicit model passed).

| Partial | Data Accessed | Method |
|---------|--------------|--------|
| `_Sidebar.cshtml` | `ViewContext.RouteData` (for active nav state) | `@Html.Partial("_Sidebar")` |
| `_Topnav.cshtml` | `User.Identity.Name` / `User.FindFirstValue(ClaimTypes.Name)` | `@Html.Partial("_Topnav")` |

---

## CSS Custom Property Contract

The following CSS tokens are defined in `site.css` and MUST be referenced by name throughout all stylesheets. No hard-coded colour values are permitted in any file added or modified in this phase.

| Token | Value | Usage |
|-------|-------|-------|
| `--primary-color` | `#004D82` | Sidebar background, active link highlight, primary buttons |
| `--primary-dark` | `#003560` | Sidebar hover states, topnav border |
| `--primary-light` | `#e8f1f8` | Active nav item background (light variant) |
| `--sidebar-width` | `260px` | Sidebar fixed width on desktop |
| `--topnav-height` | `60px` | Top navigation bar height |
