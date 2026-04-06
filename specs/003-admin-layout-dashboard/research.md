# Research: Admin Layout & Dashboard

**Feature Branch**: `003-admin-layout-dashboard`  
**Date**: 2026-04-02  
**Purpose**: Resolve all unknowns identified in Technical Context before design begins.

---

## R-001 — Bootstrap 5 Sidebar Pattern

**Question**: What is the correct Bootstrap 5 pattern for a collapsible sidebar that works on both desktop and mobile without a separate CSS framework?

**Decision**: Use Bootstrap 5's **Offcanvas** component for the sidebar on mobile viewports (`offcanvas-start`), and a static positioned sidebar `<div>` on desktop using a CSS media query to switch between the two. This avoids a custom overlay implementation.

**Rationale**: Offcanvas is built into Bootstrap 5.3 (already in the project via CDN) and provides the animated slide-in/out, backdrop, and ARIA keyboard navigation for free. No additional JS library is needed.

**Alternatives considered**:
- **Custom CSS toggle**: Would work but requires writing aria management manually — rejected in favour of the built-in Offcanvas which handles accessibility.
- **Bootstrap Collapse**: Designed for vertical accordion content, not sidebar panels — rejected; wrong semantic.

---

## R-002 — Active Navigation State in Razor Layout

**Question**: How should the sidebar mark the currently active navigation link without duplicating controller/action strings?

**Decision**: Inject `IHttpContextAccessor` (or use the View context's `ViewContext.RouteData`) inside the `_Sidebar.cshtml` partial. Compare `ViewContext.RouteData.Values["controller"]` and `ViewContext.RouteData.Values["action"]` to the expected values and conditionally add the `active` CSS class.

**Rationale**: This is the idiomatic ASP.NET Core MVC approach — no additional packages or tag helpers required, and it works with the existing routing configuration.

**Alternatives considered**:
- **ViewData["ActivePage"] set in each controller**: Would work but requires every controller action to set the value — error-prone and repetitive; rejected.
- **Tag helper extension**: Clean but adds unnecessary abstraction for a small number of nav items; deferred to later phases if needed.

---

## R-003 — Dashboard Stat: Active Sessions

**Question**: ASP.NET Core Identity does not track active sessions by default. How should "Active Sessions" be approximated without building a full session-tracking system?

**Decision**: Use **total count of users where `IsActive = true` and `LockoutEnd` is null or in the past** as a proxy for "active accounts". Rename the dashboard card label to **"Active Users"** to be accurate rather than misleading.

**Rationale**: The spec assumption (documented in spec.md) explicitly states that a real-time session count is out of scope and an approximation is acceptable. Using `IsActive` flag (already on `ApplicationUser`) makes the stat meaningful and truthful without any infrastructure change.

**Alternatives considered**:
- **Distributed cache / session table**: Accurate but requires a new DB table and session-tracking middleware — out of scope for Phase 3.
- **SignalR online presence**: Real-time but adds a new dependency; deferred to future phases.

---

## R-004 — CDN vs Local Assets for Bootstrap and Font Awesome

**Question**: The `_AuthLayout.cshtml` uses CDN links. Should the admin layout also use CDN, or should assets be installed locally via LibMan?

**Decision**: **Use CDN links** in `_Layout.cshtml` for Bootstrap 5.3 and Font Awesome 6.x, matching the pattern already established in `_AuthLayout.cshtml`. Add a `<noscript>` fallback comment for documentation purposes.

**Rationale**: The project already uses CDN in the auth layout; consistency is more important than local-asset control in a template project. The constitution does not mandate local assets.

**Alternatives considered**:
- **LibMan**: Correct for production isolation but introduces a setup step (LibMan tool installation) that adds friction for first-time cloners; deferred.

---

## R-005 — CSS Architecture for `--primary-color` Token

**Question**: The `_AuthLayout.cshtml` defines `--primary-color` inside a `<style>` block (inline). How should the admin layout define this token to comply with the "no inline styles" constitution rule?

**Decision**: Define `:root { --primary-color: #004D82; }` in `wwwroot/css/site.css` (replacing the current placeholder content). All Bootstrap colour overrides (`.btn-primary`, `.nav-link.active`, etc.) reference `var(--primary-color)`. The existing inline `<style>` block in `_AuthLayout.cshtml` is **not changed** in this phase (it is pre-existing and out of scope).

**Rationale**: `site.css` is already linked from `_Layout.cshtml` and is the correct single-source-of-truth for global CSS variables per the constitution.

**Alternatives considered**:
- **SCSS with compilation step**: Would require adding a build step; not warranted for a template with minimal custom CSS.

---

## R-006 — Logout: GET vs POST

**Question**: Should the logout action be a GET or POST request?

**Decision**: **POST only**, decorated with `[ValidateAntiForgeryToken]`. The topnav logout link will submit a small hidden form via JavaScript `fetch` (or a `<form>` with `method="post"` triggered by a link click).

**Rationale**: Logging out via a GET request is a CSRF vulnerability — a malicious third-party page could embed `<img src="/Account/Logout">` and force logout. ASP.NET Core Identity's `SignOutAsync` should always be triggered from a POST. This aligns with the constitution security requirement.

**Alternatives considered**:
- **GET logout with anti-forgery query parameter**: Non-standard and brittle — rejected.

---

## Resolution Summary

| Unknown | Resolution |
|---------|-----------|
| Sidebar collapse pattern | Bootstrap 5 Offcanvas on mobile; static on desktop |
| Active nav state | `ViewContext.RouteData.Values` comparison in partial |
| "Active Sessions" stat | Renamed to "Active Users"; uses `IsActive` flag count |
| CDN vs local assets | CDN, matching existing `_AuthLayout.cshtml` pattern |
| CSS token placement | `:root` in `site.css`; no inline styles in admin layout |
| Logout HTTP method | POST + `[ValidateAntiForgeryToken]` |

All NEEDS CLARIFICATION items are resolved. Ready for Phase 1 design.
