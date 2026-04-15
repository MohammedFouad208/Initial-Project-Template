using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminTemplate.Web.Controllers;

[AllowAnonymous]
public class ErrorController : Controller
{
    [HttpGet("/Error/NotFound")]
    public new IActionResult NotFound()
    {
        return View("NotFound");
    }
}
