using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using QanoonCoalition.Web.Models;

namespace QanoonCoalition.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        var host = Request.Host.Host?.ToLower() ?? "";
        if (host == "form.gcc.iq")
            return RedirectToAction("Movements", "Public");

        if (User.Identity?.IsAuthenticated == true)
        {
            if (User.IsInRole("Admin"))
                return RedirectToAction("Dashboard", "Admin");
            if (User.IsInRole("MovementManager"))
                return RedirectToAction("Dashboard", "Manager");
            // Viewer أو أي دور آخر → تسجيل خروج وإعادة لصفحة الدخول
            return RedirectToAction("Logout", "Account");
        }
        return RedirectToAction("Login", "Account");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
