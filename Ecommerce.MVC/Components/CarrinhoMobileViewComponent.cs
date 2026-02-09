using Ecommerce.MVC.Entities;
using Ecommerce.MVC.Interfaces;
using Ecommerce.MVC.Models.Carrinhos;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.MVC.Components;

public class CarrinhoMobileViewComponent : ViewComponent
{
    private readonly ICarrinhoService _carrinhoService;

    public CarrinhoMobileViewComponent(ICarrinhoService carrinhoService)
    {
        _carrinhoService = carrinhoService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var carrinho = await _carrinhoService.ObterOuCriarCarrinhoAsync(HttpContext);

        var vm = new CarrinhoViewModel
        {
            CarrinhoId = carrinho.Id,
            Entrega = 0
        };

        foreach (var item in carrinho.Itens)
        {
            var acompanhamentos = item.Acompanhamentos ?? new List<CarrinhoItemAcompanhamento>();

            var precoAcompanhamentos = acompanhamentos.Sum(a => a.PrecoSnapshot);

            vm.Itens.Add(new CarrinhoItemViewModel
            {
                ItemId = item.Id,
                Nome = item.ProdutoNomeSnapshot,
                Quantidade = item.Quantidade,

                PrecoUnitario = item.PrecoBaseSnapshot + precoAcompanhamentos,
                Total = (item.PrecoBaseSnapshot + precoAcompanhamentos) * item.Quantidade,

                Acompanhamentos = acompanhamentos.Select(a => new CarrinhoItemAcompanhamentoViewModel
                {
                    Nome = a.NomeSnapshot,
                    Preco = a.PrecoSnapshot
                }).ToList()
            });
        }


        return View(vm);
    }
}
