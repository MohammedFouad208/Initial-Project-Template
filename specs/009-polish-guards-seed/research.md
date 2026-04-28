# Research: Polish, Guards & Seed Data

**Feature**: `009-polish-guards-seed`  
**Generated**: 2026-04-12 (Phase 0)

---

## Decision 1: Bootstrap 5 Toast API & Positioning

**Decision**: Use Bootstrap 5's native `Toast` JS API with a fixed container positioned at `bottom-end` of the viewport. Inject a `<div id="toast-container" class="toast-container position-fixed bottom-0 end-0 p-3">` in `_Layout.cshtml` above the closing `</body>`. Expose a global `AppToast.show(message, type)` function in `site.js` that dynamically creates a toast element, appends it to the container, and calls `bootstrap.Toast.getOrCreateInstance(el).show()`.

**Rationale**: Bootstrap 5 Toast component is already loaded via CDN in `_Layout.cshtml`. Using the native API avoids adding a third-party notification library. The container pattern (inject once, fill dynamically) matches Bootstrap's own documented pattern. `bottom-end` is the spec-required position.

**Design token integration**: Toast `border-inline-start` uses `var(--success)` / `var(--danger)` / `var(--warning)` / `var(--info)` based on `type` parameter. Logical property `border-inline-start` ensures RTL compatibility.

**Alternatives considered**: SweetAlert2 (overkill, extra dependency), custom CSS animations (unnecessary when Bootstrap Toast exists), server-side TempData alerts (less fluid UX than toast).

---

## Decision 2: jQuery Validate + Bootstrap 5 Real-Time Validation

**Decision**: Use `jquery-validation-unobtrusive` (already present in `wwwroot/lib/`) combined with a custom Bootstrap 5 highlight/unhighlight adapter in `_ValidationScriptsPartial.cshtml`. Override `$.validator.setDefaults` with `highlight` (add `.is-invalid`) and `unhighlight` (remove `.is-invalid`, add `.is-valid`) callbacks. Load this partial in all Create and Edit form views via `@section Scripts { <partial name="_ValidationScriptsPartial" /> }`.

**Current state**: `_ValidationScriptsPartial.cshtml` already references `jquery.validate` and `jquery.validate.unobtrusive`. It does NOT configure the Bootstrap 5 integration (no highlight/unhighlight override). The `novalidate` on forms suppresses browser validation in favor of jQuery Validate, but `.is-valid`/`.is-invalid` classes are never applied because the adapter is missing.

**Implementation**: Add the following to `_ValidationScriptsPartial.cshtml` after the existing script references:

```javascript
$.validator.setDefaults({
  highlight: function (element) {
    $(element).addClass("is-invalid").removeClass("is-valid");
  },
  unhighlight: function (element) {
    $(element).removeClass("is-invalid").addClass("is-valid");
  },
  errorElement: "span",
  errorClass: "text-danger small"
});
```

**Rationale**: This is the standard ASP.NET Core + Bootstrap 5 integration pattern. It reuses existing libraries with zero new dependencies. The `errorElement`/`errorClass` settings ensure error messages render in the same style as existing `asp-validation-for` spans.

**Alternatives considered**: HTML5 native `required` constraint validation (cannot use due to `novalidate`; less consistent cross-browser), Parsley.js (extra dependency, no benefit here).

---

## Decision 3: ASP.NET Core 404 Handling

**Decision**: Use `app.UseStatusCodePagesWithRedirects("/Error/404")` in `Program.cs` combined with a new `ErrorController` action `Error404()` that returns `Error404.cshtml`. This is preferred over `UseExceptionHandler` for 404s because the app already has custom exception handling in place and `UseStatusCodePagesWithRedirects` only fires for empty response bodies with a 4xx status code.

**Rationale**: `UseStatusCodePagesWithReExecute` would be more SEO-correct (preserves original 404 status code), but for an admin template (not a public site), `WithRedirects` is simpler, sufficient, and avoids route conflict with the existing default route. The 404 page is only shown to authenticated admin users. If SEO matters for a future public-facing app derived from this template, the README notes upgrading to `WithReExecute`.

**Alternative considered**: `UseExceptionHandler("/Error")` — this handles 500s, not 4xx status codes. `UseStatusCodePagesWithReExecute` — correct SEO behavior but adds routing complexity; noted as upgrade path in README.

---

## Decision 4: Demo Seed Roles — Permission Sets

**Decision**: The three demo roles get the following permission sets, read idempotently from `IPermissionProvider` so they stay in sync if `permissions.json` is extended:

| Role | Objects | Functions |
|---|---|---|
| SuperAdmin | All | All (Browse, Create, Update, Delete, Export, AssignPermissions) |
| Admin | All | Browse, Create, Update |
| Viewer | All | Browse only |

**Rationale**: Matches the spec requirement exactly (task 9.4) and the constitution's "no hardcoded permissions" principle — Admin and Viewer permission sets are derived dynamically from the permission objects in `IPermissionProvider`, filtered to the relevant function names.

**Seed users added** (to complement roles):
- `admin@admintemplate.local` / `Admin@1234!` → assigned Admin role
- `viewer@admintemplate.local` / `Viewer@1234!` → assigned Viewer role

Credentials documented in README and in `appsettings.json` under `Seed:` section.

**Idempotency**: Seeder checks role existence by name before creating; checks permission record existence before inserting — identical to existing SuperAdmin seeding pattern.

---

## Decision 5: SMTP Configuration Placeholder

**Decision**: Add a `Smtp` section to `appsettings.json` with clearly marked placeholder values. The section matches the `SmtpSettings` shape already expected by `IEmailSender` infrastructure (populated from configuration via `IOptions<SmtpSettings>`):

```json
"Smtp": {
  "Host": "smtp.example.com",
  "Port": 587,
  "UseSsl": true,
  "UserName": "your-smtp-username",
  "Password": "your-smtp-password",
  "FromAddress": "noreply@admintemplate.local",
  "FromDisplayName": "Admin Template"
}
```

**Rationale**: Gives implementers a complete, uncommented template. Placeholder values are unambiguous strings ("your-smtp-username") rather than empty strings, which would cause silent failures.

**Alternatives considered**: User secrets for SMTP credentials (appropriate for production but the seed config intentionally uses plaintext for template onboarding clarity; README notes moving credentials to user secrets or environment variables before production).

---

## Decision 6: Forbidden Pattern Fixes (Design System Audit)

**Decision**: The following violations found during audit will be corrected as part of the design system polishing task (9.9):

| File | Violation | Fix |
|---|---|---|
| `_Layout.cshtml` | `@Html.Partial("_Sidebar")` — forbidden, use `<partial>` | Replace with `<partial name="_Sidebar" />` |
| `_Layout.cshtml` | `@Html.Partial("_Topnav")` — forbidden | Replace with `<partial name="_Topnav" />` |
| `Error403.cshtml` | `fa fa-lock` (FA4 class) instead of `fa-solid fa-lock`; icon not in soft-color circle | Redesign with `<span class="error-icon-circle danger-circle"><i class="fa-solid fa-lock"></i></span>` styled with `var(--danger-light)` bg and `var(--danger)` icon color in `site.css` |
| `Users/Create.cshtml` | `style="color:var(--primary)"` on breadcrumb links | Replace with CSS class `.breadcrumb-link` defined in `site.css` |
| `Roles/Create.cshtml` | Same inline style violation | Same fix |

**Rationale**: These are direct violations of constitution Forbidden Patterns, caught by the spec requirement for a design system audit.

---

## Decision 7: `RolePermissions` Sidebar Gating

**Decision**: The `RolePermissions` nav link in `_Sidebar.cshtml` currently has no permission check. Add a `canBrowseRolePermissions` check using `Role.AssignPermissions` function (the permission specifically defined in `permissions.json` for this action).

**Rationale**: Spec task 9.1 states "permission-driven `d-none` on nav links user cannot access." A user who lacks `Role.AssignPermissions` should not see the Role Permissions nav link. This follows the same pattern as the existing `canBrowseUsers` and `canBrowseRoles` checks.
