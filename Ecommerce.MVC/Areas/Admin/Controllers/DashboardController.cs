using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.MVC.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "administrador")]
public class DashboardController : Controller
{
    public IActionResult EmConstrucao()
    {
        return View();
    }
}