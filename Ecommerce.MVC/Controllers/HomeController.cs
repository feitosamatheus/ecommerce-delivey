using Ecommerce.MVC.Config;
using Ecommerce.MVC.Interfaces;
using Ecommerce.MVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;

namespace Ecommerce.MVC.Controllers;

public class HomeController : Controller
{
    private readonly IProdutoService _produtoService;
    private readonly ICategoriaService _categoriaService;
    private readonly DatabaseContext _context;

    public HomeController(IProdutoService produtoService, ICategoriaService categoriaService, DatabaseContext context)
    {
        _produtoService = produtoService;
        _categoriaService = categoriaService;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var categorias = await _categoriaService.BuscarCategoriasAsync();

        if (User.Identity.IsAuthenticated)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (Guid.TryParse(userIdClaim, out Guid clienteId))
            {
                var cliente = await _context.Clientes
                    .FirstOrDefaultAsync(c => c.Id == clienteId);

                if (cliente != null)
                {
                    bool precisaTrocar = cliente.PrimeiroAcessoRedefinir;

                    ViewBag.ExibirModalTrocaSenha = precisaTrocar;

                    // if (cliente.PrimeiroAcessoRedefinir)
                    // {
                    //     cliente.PrimeiroAcessoRedefinir = false;

                    //     _context.Clientes.Update(cliente);
                    //     await _context.SaveChangesAsync();
                    // }
                }
            }
        }

        return View(new HomeViewModel { Categorias = categorias });
    }

    public IActionResult EmConstrucao()
    {
        return View();
    }

    [HttpGet]
    public IActionResult RenderizarFooter()
    {
        return ViewComponent("Footer");
    }
}
