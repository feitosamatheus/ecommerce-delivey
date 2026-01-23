using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.MVC.Components;

public class HeaderViewComponent: ViewComponent
{
    public HeaderViewComponent() { }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        string nomeCliente = null;
        bool autenticado = false;

        if (HttpContext.User.Identity?.IsAuthenticated == true)
        {
            autenticado = true;

            // padrão: ClaimTypes.Name
            nomeCliente = HttpContext.User.FindFirstValue(ClaimTypes.Name);
        }

        ViewBag.Autenticado = autenticado;
        ViewBag.NomeCliente = nomeCliente;
        
        return View();
    }
}