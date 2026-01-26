using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Ecommerce.MVC.Models;
using Ecommerce.MVC.Interfaces;

namespace Ecommerce.MVC.Controllers;

public class HomeController : Controller
{
    private readonly IProdutoService _produtoService;
    private readonly ICategoriaService _categoriaService;

    public HomeController(IProdutoService produtoService, ICategoriaService categoriaService)
    {
        _produtoService = produtoService;
        _categoriaService = categoriaService;
    }

    public async Task<IActionResult> Index()
    {
        var categorias = await _categoriaService.BuscarCategoriasAsync();
        return View(new HomeViewModel { Categorias = categorias });
    }
}
