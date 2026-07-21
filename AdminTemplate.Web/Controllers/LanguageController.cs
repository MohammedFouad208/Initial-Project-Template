using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace AdminTemplate.Web.Controllers;

public class LanguageController : Controller
{
    private static readonly HashSet<string> _supported = ["en", "ar"];

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Set(string culture, string returnUrl = "/")
    {
        if (!_supported.Contains(culture))
            culture = "en";

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true });

        if (!Url.IsLocalUrl(returnUrl))
            returnUrl = "/";

        return LocalRedirect(returnUrl);
    }
}
