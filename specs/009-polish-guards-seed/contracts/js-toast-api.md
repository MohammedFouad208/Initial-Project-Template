# Contract: AppToast JavaScript API

**Feature**: `009-polish-guards-seed`  
**Type**: Client-side JS interface  
**Defined in**: `wwwroot/js/site.js`  
**Consumed by**: All admin Razor views (via `_Layout.cshtml`)

---

## Interface

```javascript
AppToast.show(message, type)
```

### Parameters

| Parameter | Type | Required | Values |
|---|---|---|---|
| `message` | string | Yes | Human-readable notification text |
| `type` | string | No (default: `"info"`) | `"success"` \| `"danger"` \| `"warning"` \| `"info"` |

### Behavior Contract

1. Creates a Bootstrap 5 Toast element dynamically.
2. Appends the element to `#toast-container` (injected once in `_Layout.cshtml`).
3. Applies `border-inline-start: 4px solid var(--{type})` to the toast element for design-token color.
4. Shows the toast using `bootstrap.Toast.getOrCreateInstance(el, { delay: 4000 }).show()`.
5. Removes the element from the DOM after it hides (`hidden.bs.toast` event).
6. Works in both LTR and RTL layouts (logical property `border-inline-start`).

### Toast Container (injected in `_Layout.cshtml`)

```html
<div id="toast-container" class="toast-container position-fixed bottom-0 end-0 p-3"
     style="z-index: 1100;" aria-live="polite" aria-atomic="true"></div>
```

> **Note**: `z-index: 1100` places toasts above Bootstrap modals (`z-index: 1055`) and the sticky topnav (`z-index: 1020`).

### TempData Integration Pattern

Server actions that redirect after success/failure pass a message via TempData. `_Layout.cshtml` includes an inline script after `site.js` that reads TempData-injected values and calls `AppToast.show()`:

```cshtml
@if (TempData["ToastMessage"] != null)
{
    <script>
        document.addEventListener("DOMContentLoaded", function() {
            AppToast.show(@Json.Serialize(TempData["ToastMessage"]!.ToString()),
                          @Json.Serialize(TempData["ToastType"]?.ToString() ?? "info"));
        });
    </script>
}
```

**TempData keys**:

| Key | Type | Description |
|---|---|---|
| `ToastMessage` | string | Message text to display |
| `ToastType` | string | `"success"` or `"danger"` (defaults to `"info"` if missing) |

---

## Example Usage (in a controller redirect)

```csharp
TempData["ToastMessage"] = "Permissions saved successfully.";
TempData["ToastType"] = "success";
return RedirectToAction(nameof(Index), new { roleId });
```

---

## Example Usage (from JavaScript on the same page)

```javascript
// After an AJAX operation completes:
AppToast.show("User activated successfully.", "success");
AppToast.show("Failed to delete role.", "danger");
```
