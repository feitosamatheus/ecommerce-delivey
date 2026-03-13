using Ecommerce.MVC.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Ecommerce.MVC.Components;

public class HeaderViewComponent: ViewComponent
{
    private readonly IPedidoService _pedidoService;

    public HeaderViewComponent(IPedidoService pedidoService)
    {
        _pedidoService = pedidoService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        string nomeCliente = null;
        bool autenticado = false;
        int totalPedidosAndamento = 0;
        var perfil = "";
        if (HttpContext.User.Identity?.IsAuthenticated == true)
        {
            autenticado = true;
            nomeCliente = HttpContext.User.FindFirstValue(ClaimTypes.Name);
            perfil = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

            var claimId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(claimId, out var clienteId))
                totalPedidosAndamento = await _pedidoService.ObterQuantidadeEmAndamentoAsync(clienteId, HttpContext.RequestAborted);
        }

        ViewBag.Autenticado = autenticado;
        ViewBag.NomeCliente = nomeCliente;
        ViewBag.TotalPedidos = totalPedidosAndamento;
        ViewBag.Perfil = perfil;

        return View();
    }
}