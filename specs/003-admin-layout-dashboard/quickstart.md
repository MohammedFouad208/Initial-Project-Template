# Quickstart: Admin Layout & Dashboard

**Feature Branch**: `003-admin-layout-dashboard`  
**Date**: 2026-04-02  
**Prerequisites**: Phase 1 (Foundation) and Phase 2 (Auth Pages) complete and running.

---

## What This Phase Delivers

After implementing Phase 3 you will have:

- A full sidebar + top navigation bar rendered on every authenticated admin page.
- A `/Dashboard` home page showing Total Users, Total Roles, and Active Users stat cards with live data.
- A responsive layout: sidebar collapses to a hamburger-triggered offcanvas on mobile.
- Brand colour `#004D82` applied consistently via CSS custom properties.
- A functioning Logout action (POST) from the topnav avatar dropdown.

---

## Files to Create / Modify

| Action | Path |
|--------|------|
| REPLACE | `AdminTemplate.Web/Views/Shared/_Layout.cshtml` |
| CREATE | `AdminTemplate.Web/Views/Shared/_Sidebar.cshtml` |
| CREATE | `AdminTemplate.Web/Views/Shared/_Topnav.cshtml` |
| REPLACE | `AdminTemplate.Web/wwwroot/css/site.css` |
| CREATE | `AdminTemplate.Web/wwwroot/css/sidebar.css` |
| REPLACE | `AdminTemplate.Web/wwwroot/js/site.js` |
| CREATE | `AdminTemplate.Web/Controllers/DashboardController.cs` |
| CREATE | `AdminTemplate.Web/ViewModels/DashboardViewModel.cs` |
| CREATE | `AdminTemplate.Web/Views/Dashboard/Index.cshtml` |
| MODIFY | `AdminTemplate.Web/Controllers/AccountController.cs` — add `Logout` POST action |
| MODIFY | `AdminTemplate.Web/Program.cs` — update default route to `Dashboard/Index` |

---

## Implementation Steps (for `/speckit.implement`)

### Step 1 — CSS foundation

1. Replace `wwwroot/css/site.css` with `:root` CSS variable declarations (`--primary-color`, `--primary-dark`, `--primary-light`, `--sidebar-width`, `--topnav-height`) and base `body`/`html` resets.
2. Create `wwwroot/css/sidebar.css` with the sidebar layout rules (fixed position on desktop, offcanvas on mobile), hover/active states using `var(--primary-color)`, and smooth transition rules.

### Step 2 — Layout shell

1. Replace `Views/Shared/_Layout.cshtml`:
   - `<html>` / `<head>` linking Bootstrap 5.3 CDN, Font Awesome 6.x CDN, `site.css`, `sidebar.css`.
   - `<body>` structured as: sidebar `<div>` (renders `_Sidebar` partial) | main wrapper containing topnav `<div>` (renders `_Topnav` partial) + `<div class="page-content">` where `@RenderBody()` sits.
   - Footer removed (not in spec for admin layout).
   - Scripts section at bottom: Bootstrap bundle CDN, `site.js`, `@RenderSection("Scripts", required: false)`.

2. Create `Views/Shared/_Sidebar.cshtml`:
   - App logo / name at top.
   - `<nav>` with `<ul>` of links; each `<li>` reads `ViewContext.RouteData.Values["controller"]` to apply `active` class.
   - Links: Dashboard, Users, Roles, Role Permissions (using Font Awesome icons per data-model.md).

3. Create `Views/Shared/_Topnav.cshtml`:
   - Hamburger `<button>` with `data-bs-toggle="offcanvas"` targeting the sidebar on mobile.
   - App name / breadcrumb area.
   - Notification bell `<i class="fa fa-bell">` with a badge `<span>` (static `0` in this phase).
   - User avatar dropdown: displays `User.Identity.Name`; items: "Profile" (stub link) and "Logout" form POST button.

### Step 3 — Logout action

Add to `AccountController.cs`:
```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Logout()
{
    await _signInManager.SignOutAsync();
    return RedirectToAction("Login", "Account");
}
```

### Step 4 — Dashboard controller & ViewModel

1. Create `ViewModels/DashboardViewModel.cs` with `TotalUsers`, `TotalRoles`, `ActiveUsers` int properties.
2. Create `Controllers/DashboardController.cs`:
   - Constructor injects `IUserService` and `IRoleService`.
   - `[Authorize]` attribute on controller class.
   - `Index()` action: calls service count methods, maps to `DashboardViewModel`, returns `View(model)`.
3. Create `Views/Dashboard/Index.cshtml`: Bootstrap 5 row of three `col-md-4` stat cards, each showing label + count from the model.

### Step 5 — Default route

In `Program.cs`, change the default route pattern:
```csharp
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");
```

### Step 6 — site.js

Replace `wwwroot/js/site.js` with the sidebar toggle script for desktop collapse (toggling a `sidebar-collapsed` class on the body/wrapper when the hamburger is tapped on desktop, separate from the Bootstrap Offcanvas behaviour on mobile).

---

## Acceptance Verification

Run through these checks after implementation:

1. [ ] Navigate to `https://localhost:{port}/` — redirects to `/Account/Login` when not logged in.
2. [ ] Log in with the seeded SuperAdmin account — lands on Dashboard home.
3. [ ] Dashboard shows three stat cards with non-zero numeric values.
4. [ ] Sidebar is visible on desktop with Dashboard link highlighted as active.
5. [ ] Click "Users" in the sidebar → navigates to `/Users/Index`; "Users" link is now active.
6. [ ] Resize browser to < 768px — sidebar disappears.
7. [ ] Tap hamburger — Bootstrap Offcanvas sidebar slides in.
8. [ ] Click the avatar dropdown → "Logout" appears.
9. [ ] Click "Logout" → redirected to login page; navigating back to any admin URL redirects to login.
10. [ ] No inline `style="..."` attributes exist in `_Layout.cshtml`, `_Sidebar.cshtml`, or `_Topnav.cshtml`.
