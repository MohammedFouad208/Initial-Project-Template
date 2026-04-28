using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminTemplate.Web.Areas.SystemAdmin.Controllers;

[Area("SystemAdmin")]
[Authorize(Policy = "SuperAdminOnly")]
public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
