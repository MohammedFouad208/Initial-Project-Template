using AdminTemplate.Application.Interfaces.CodeGen;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminTemplate.Web.Areas.SystemAdmin.Controllers;

[Area("SystemAdmin")]
[Authorize(Policy = "SuperAdminOnly")]
public class ThemeController : Controller
{
    private readonly IThemeService _themeService;
    private readonly IWebHostEnvironment _env;

    public ThemeController(IThemeService themeService, IWebHostEnvironment env)
    {
        _themeService = themeService;
        _env          = env;
    }

    private string ProjectRoot => Path.GetFullPath(Path.Combine(_env.ContentRootPath, ".."));

    [HttpGet]
    public IActionResult Index()
    {
        var theme = _themeService.LoadTheme(ProjectRoot);
        return View(theme);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Save(ThemeConfig config)
    {
        try
        {
            _themeService.SaveTheme(config, ProjectRoot);
            TempData["Success"] = "Theme saved and CSS generated successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Failed to save theme: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Preview()
    {
        var theme = _themeService.LoadTheme(ProjectRoot);
        var css = _themeService.GenerateCss(theme);
        return Content(css, "text/css");
    }
}
