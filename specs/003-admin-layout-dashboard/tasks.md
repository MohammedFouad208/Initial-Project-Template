# Tasks: Admin Layout & Dashboard

**Input**: Design documents from `/specs/003-admin-layout-dashboard/`  
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅, quickstart.md ✅

**Tests**: No test tasks — not requested in spec or user stories.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: User story label (US1–US4 as defined in spec.md)
- Exact file paths are included in all descriptions

---

## Phase 1: Setup

**Purpose**: No new project scaffolding required — solution, DI, and database are in place from Phase 1 & 2. Phase 3 setup is the CSS token foundation that all layout work depends on.

- [ ] T001 [P] Replace `AdminTemplate.Web/wwwroot/css/site.css` — clear placeholder content and define `:root` block with five CSS custom properties: `--primary-color: #004D82`, `--primary-dark: #003560`, `--primary-light: #e8f1f8`, `--sidebar-width: 260px`, `--topnav-height: 60px`; add base `html`/`body` resets removing the default bottom margin
- [ ] T002 [P] Create `AdminTemplate.Web/wwwroot/css/sidebar.css` — sidebar layout rules: `.sidebar` fixed `260px` width on desktop using `var(--sidebar-width)` and `var(--primary-color)` background; `.nav-link` default and `.nav-link.active` styles using `var(--primary-light)` and `var(--primary-dark)`; `.main-wrapper` left margin matching `var(--sidebar-width)`; hover transitions; sidebar independently scrollable with `overflow-y: auto`; `.topnav` height using `var(--topnav-height)`; no hardcoded colour values anywhere in this file

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: No additional foundational tasks — Phase 1 & 2 of the project already provide Identity, DI, DB, and AccountController infrastructure. CSS in Phase 1 (Setup above) is the only prerequisite that blocks user story implementation.

**⚠️ CRITICAL**: T001 and T002 must be complete before any user story layout work begins.

---

## Phase 3: User Story 1 — Navigate the Admin Shell (Priority: P1) 🎯 MVP

**Goal**: Replace the default MVC layout with a full admin shell — sidebar + top navigation bar — rendered consistently on every authenticated admin page.

**Independent Test**: Log in with seeded SuperAdmin; verify sidebar and topnav are visible on any admin page; click each sidebar link and confirm the link is highlighted as active.

### Implementation for User Story 1

- [ ] T003 [US1] Replace `AdminTemplate.Web/Views/Shared/_Layout.cshtml` — rebuild from scratch: `<head>` links Bootstrap 5.3 CDN, Font Awesome 6.x CDN, `~/css/site.css`, `~/css/sidebar.css`; `<body>` structured as a flex container with `.sidebar` div rendering `@Html.Partial("_Sidebar")` and `.main-wrapper` div containing `.topnav` rendering `@Html.Partial("_Topnav")` followed by `<div class="page-content">@RenderBody()</div>`; scripts at bottom: Bootstrap bundle CDN, `~/js/site.js`, `@await RenderSectionAsync("Scripts", required: false)`; remove footer and default nav bar
- [ ] T004 [P] [US1] Create `AdminTemplate.Web/Views/Shared/_Sidebar.cshtml` — app name at top (reads from `IConfiguration` or hardcoded brand string); `<nav>` with four `<a>` links: Dashboard (`fa-gauge-high`), Users (`fa-users`), Roles (`fa-shield-halved`), Role Permissions (`fa-key`); each link receives `active` CSS class when `ViewContext.RouteData.Values["controller"]?.ToString()` matches its target controller name; sidebar `id="adminSidebar"` for offcanvas targeting
- [ ] T005 [P] [US1] Create `AdminTemplate.Web/Views/Shared/_Topnav.cshtml` — hamburger `<button>` with `data-bs-toggle="offcanvas"` and `data-bs-target="#adminSidebar"` for mobile; app title text; Font Awesome bell icon `<i class="fas fa-bell">` with `<span class="badge">0</span>` (static placeholder); Bootstrap dropdown showing `User.Identity.Name` with items: "Profile" `<a>` stub link and "Logout" as an inline `<form method="post" action="/Account/Logout">` containing `@Html.AntiForgeryToken()` and a `<button type="submit">` styled as a link

**Checkpoint**: US1 complete — admin shell renders on all authenticated pages with working nav active states.

---

## Phase 4: User Story 2 — Collapse Sidebar on Mobile (Priority: P2)

**Goal**: On viewports ≤768px the sidebar is hidden by default; the hamburger button in the topnav opens it as a Bootstrap Offcanvas overlay. On desktop, the sidebar can optionally toggle collapsed via a desktop hamburger button.

**Independent Test**: Resize browser to <768px; verify sidebar is not visible; tap hamburger; confirm sidebar slides in as offcanvas; tap outside or tap hamburger again to dismiss.

### Implementation for User Story 2

- [ ] T006 [US2] Replace `AdminTemplate.Web/wwwroot/js/site.js` — add a `DOMContentLoaded` listener that attaches a click handler to a `.sidebar-desktop-toggle` button (if present) to toggle a `sidebar-collapsed` CSS class on `document.body`, shrinking `.main-wrapper` margin and hiding sidebar text labels; Bootstrap Offcanvas on mobile is already handled declaratively via `data-bs-toggle="offcanvas"` in `_Topnav.cshtml` (no extra JS needed for mobile); add a media query check so desktop toggle script only runs at `window.innerWidth >= 768`

**Checkpoint**: US2 complete — sidebar collapses on mobile via offcanvas; desktop sidebar toggles on demand.

---

## Phase 5: User Story 3 — View the Dashboard Home (Priority: P3)

**Goal**: Authenticated users land on a dashboard page displaying three live stat cards: Total Users, Total Roles, Active Users.

**Independent Test**: Log in; confirm redirect to `/Dashboard`; verify three stat cards render with non-zero counts matching the seeded data from Phase 1.

### Implementation for User Story 3

- [ ] T007 [P] [US3] Create `AdminTemplate.Web/ViewModels/DashboardViewModel.cs` — three `int` properties: `TotalUsers`, `TotalRoles`, `ActiveUsers`; namespace `AdminTemplate.Web.ViewModels`
- [ ] T008 [P] [US3] Add `GetTotalCountAsync()` method to `AdminTemplate.Application/Interfaces/IUserService.cs` and implement it in `AdminTemplate.Application/Services/UserService.cs` — returns total count of all `ApplicationUser` records; add `GetActiveCountAsync()` returning count where `IsActive == true` and `LockoutEnd` is null or in the past
- [ ] T009 [P] [US3] Add `GetTotalCountAsync()` method to `AdminTemplate.Application/Interfaces/IRoleService.cs` and implement it in `AdminTemplate.Application/Services/RoleService.cs` — returns total count of all `ApplicationRole` records
- [ ] T010 [P] [US3] Update `AdminTemplate.Web/Program.cs` — change the default route pattern from `{controller=Home}/{action=Index}/{id?}` to `{controller=Dashboard}/{action=Index}/{id?}`
- [ ] T011 [US3] Create `AdminTemplate.Web/Controllers/DashboardController.cs` — class-level `[Authorize]` attribute; constructor injects `IUserService` and `IRoleService`; `Index()` async action builds `DashboardViewModel` by calling `GetTotalCountAsync()` on both services and `GetActiveCountAsync()` on user service; returns `View(model)`
- [ ] T012 [US3] Create `AdminTemplate.Web/Views/Dashboard/Index.cshtml` — `@model AdminTemplate.Web.ViewModels.DashboardViewModel`; Bootstrap row with three `col-xl-4 col-md-6` stat cards; each card shows an icon, a label ("Total Users", "Total Roles", "Active Users"), and the bound count (`Model.TotalUsers`, `Model.TotalRoles`, `Model.ActiveUsers`); cards styled using `var(--primary-color)` for icon colour; no hardcoded colours

**Checkpoint**: US3 complete — dashboard home renders with live stat counts.

---

## Phase 6: User Story 4 — Log Out Safely (Priority: P4)

**Goal**: Clicking Logout in the topnav dropdown terminates the session and redirects to the login page via a CSRF-protected POST.

**Independent Test**: Log in; click Logout in the topnav avatar dropdown; confirm redirect to `/Account/Login`; confirm that navigating to `/Dashboard` redirects back to login.

### Implementation for User Story 4

- [ ] T013 [US4] Add `Logout` action to `AdminTemplate.Web/Controllers/AccountController.cs` — decorate with `[HttpPost]` and `[ValidateAntiForgeryToken]`; call `await _signInManager.SignOutAsync()`; return `RedirectToAction("Login", "Account")`; ensure `_signInManager` is already injected (it should be from Phase 2 — verify and add to constructor if missing)

**Checkpoint**: US4 complete — logout terminates session and redirects securely.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Consistency, security, and spec acceptance verification.

- [ ] T014 [P] Audit all Phase 3 files for hardcoded colour values — check `AdminTemplate.Web/Views/Shared/_Layout.cshtml`, `_Sidebar.cshtml`, `_Topnav.cshtml`, `AdminTemplate.Web/wwwroot/css/site.css`, `AdminTemplate.Web/wwwroot/css/sidebar.css`; replace any occurrences of `#004D82`, `#003560`, or other hardcoded colour hex values with their `var(--*)` equivalents
- [ ] T015 Run the 10-point acceptance verification checklist from `specs/003-admin-layout-dashboard/quickstart.md` — verify each checkpoint passes before marking Phase 3 complete

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately. T001 and T002 are parallel.
- **Foundational (Phase 2)**: N/A — merged into Setup above.
- **US1 (Phase 3)**: Depends on T001 + T002 completion. T003 must complete before T004/T005.
- **US2 (Phase 4)**: Depends on T004 + T005 completion (needs HTML structure in place for JS targeting).
- **US3 (Phase 5)**: Depends on T003 (layout shell must exist). T007, T008, T009, T010 are parallel. T011 depends on T007+T008+T009. T012 depends on T011.
- **US4 (Phase 6)**: Depends on T005 completion (Logout form is rendered from `_Topnav.cshtml`). Can proceed independently from US2/US3.
- **Polish (Phase 7)**: Depends on all story phases being complete.

### User Story Dependencies

- **US1 (P1)**: Depends on Setup only — no story dependencies.
- **US2 (P2)**: Depends on US1 layout HTML being in place (T003, T004, T005). Independently testable on its own viewport.
- **US3 (P3)**: Depends on US1 (the admin layout shell renders the Dashboard view). Service count methods (T008, T009) can be worked in parallel with US1/US2.
- **US4 (P4)**: Depends on `_Topnav.cshtml` (T005). Can be implemented in parallel with T006/US2 and US3 service tasks.

### Parallel Opportunities

```
Phase 1 (can start immediately, run together):
  Task: T001  Replace site.css
  Task: T002  Create sidebar.css

After T001 + T002:
  Task: T003  Replace _Layout.cshtml  ← must complete first

After T003:
  Task: T004  Create _Sidebar.cshtml  (parallel with T005)
  Task: T005  Create _Topnav.cshtml   (parallel with T004)

After T004 + T005 (while US2/US4 proceed, US3 service tasks can run):
  Task: T006  Replace site.js                         [US2]  (separate file)
  Task: T007  Create DashboardViewModel.cs             [US3]  (parallel with T008, T009, T010)
  Task: T008  Add count methods to IUserService        [US3]  (parallel with T007, T009, T010)
  Task: T009  Add count methods to IRoleService        [US3]  (parallel with T007, T008, T010)
  Task: T010  Update Program.cs default route          [US3]  (parallel with T007, T008, T009)
  Task: T013  Add Logout action to AccountController   [US4]  (parallel with US3)

After T007 + T008 + T009:
  Task: T011  Create DashboardController.cs  [US3]

After T011:
  Task: T012  Create Dashboard/Index.cshtml  [US3]

After all above:
  Task: T014  CSS colour audit      (parallel with T015 if auditing separately)
  Task: T015  Acceptance checklist
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 Setup: T001, T002
2. Complete Phase 3 (US1): T003 → T004 + T005 in parallel
3. **STOP and VALIDATE**: Verify admin shell renders with sidebar and topnav on desktop
4. Proceed to US2 (mobile collapse) to complete responsive behaviour

### Full Feature Delivery Order

P1 → P2 → P4 (quick, adds POST logout) → P3 (dashboard stat cards)

---

## Summary

| Metric | Value |
|--------|-------|
| Total tasks | 15 |
| Setup tasks | 2 (T001–T002) |
| US1 tasks | 3 (T003–T005) |
| US2 tasks | 1 (T006) |
| US3 tasks | 6 (T007–T012) |
| US4 tasks | 1 (T013) |
| Polish tasks | 2 (T014–T015) |
| Tasks marked [P] | 9 |
| New files | 8 |
| Modified files | 3 |
| Migrations required | None |

### Independent Test Criteria Per Story

| Story | How to Test Independently |
|-------|--------------------------|
| US1 — Admin Shell | Log in; verify sidebar + topnav render; click nav links; confirm active state |
| US2 — Mobile Collapse | Resize to <768px; verify sidebar hidden; tap hamburger; confirm offcanvas opens/closes |
| US3 — Dashboard Home | Log in; navigate to `/Dashboard`; verify 3 stat cards with live non-zero counts |
| US4 — Safe Logout | Log in; click Logout; confirm redirect to login; confirm admin URLs redirect back to login |

### Suggested MVP Scope

Implement US1 only (T001–T005): delivers the admin shell that all future phases depend on. US2, US3, US4 can follow in the same session.
