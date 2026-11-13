using Microsoft.AspNetCore.Mvc;

namespace LeaveManager.Web.Controllers;

public sealed class HomeController : Controller
{
    public IActionResult Index() => View();

    [Route("/Home/Error")]
    public IActionResult Error()
    {
        Response.StatusCode = StatusCodes.Status500InternalServerError;
        return View();
    }

    [Route("/Home/NotFound")]
    public IActionResult NotFoundPage()
    {
        Response.StatusCode = StatusCodes.Status404NotFound;
        return View("NotFound");
    }
}
