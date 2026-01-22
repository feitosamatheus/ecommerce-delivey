using Microsoft.AspNetCore.Mvc;

public class ClienteController : Controller
{
    [HttpGet]
    public IActionResult ContaModal()
    {
        return PartialView("Partials/_ClienteLogin", null);
    }
}