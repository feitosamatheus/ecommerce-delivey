using Ecommerce.MVC.Interfaces;
using Ecommerce.MVC.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ecommerce.MVC.Components;

public class FooterViewComponent : ViewComponent
{
    private readonly IPedidoService _pedidoService;
    private readonly ICarrinhoService _carrinhoService;

    public FooterViewComponent(IPedidoService pedidoService, ICarrinhoService carrinhoService)
    {
        _pedidoService = pedidoService;
        _carrinhoService = carrinhoService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var model = new FooterViewModel
        {
            Autenticado = false,
            TotalPedidosEmAndamento = 0,
            QuantidadeCarrinho = await _carrinhoService.ObterQuantidadeItensAsync(HttpContext, HttpContext.RequestAborted)
        };

        if (HttpContext.User.Identity?.IsAuthenticated == true)
        {
            model.Autenticado = true;

            var claimId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(claimId, out var clienteId))
            {
                model.TotalPedidosEmAndamento =
                    await _pedidoService.ObterQuantidadeEmAndamentoAsync(
                        clienteId,
                        HttpContext.RequestAborted
                    );
            }
        }

        return View(model);
    }
}
