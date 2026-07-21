using AdminTemplate.Application.Interfaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AdminTemplate.Web.Areas.SystemAdmin.Controllers;

[Area("SystemAdmin")]
[Authorize(Policy = "SuperAdminOnly")]
public class ResourcesController : Controller
{
    private readonly IResourceService _resourceService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ResourcesController(IResourceService resourceService, IStringLocalizer<SharedResource> localizer)
    {
        _resourceService = resourceService;
        _localizer       = localizer;
    }

    [HttpGet]
    public IActionResult Index(string? search)
    {
        var entries = _resourceService.GetAll();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            entries = entries
                .Where(e => e.Key.Contains(term, StringComparison.OrdinalIgnoreCase)
                         || e.Value.Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        ViewBag.Search = search;
        return View(entries);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Upsert([FromForm] string key, [FromForm] string value)
    {
        key = key?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(key))
            return Json(new { success = false, message = _localizer["Resources_Error_KeyRequired"].Value });

        _resourceService.Upsert(key, value ?? string.Empty);
        return Json(new { success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete([FromForm] string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return Json(new { success = false, message = _localizer["Resources_Error_KeyRequired"].Value });

        _resourceService.Delete(key.Trim());
        return Json(new { success = true });
    }
}
