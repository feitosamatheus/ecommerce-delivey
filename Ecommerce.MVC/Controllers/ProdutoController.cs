using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Ecommerce.MVC.Interfaces;
using Ecommerce.MVC.Models.Produtos;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.MVC.Controllers;
public class ProdutoController : Controller
{
    private readonly IProdutoService _produtoService;

    public ProdutoController(IProdutoService produtoService)
    {
        _produtoService = produtoService;
    }

    [HttpGet]
    public async Task<IActionResult> BuscarModalAdicionarProduto(Guid id)
    {
        var produto = await _produtoService.ObterPorIdAsync(id);
        if (produto == null) return NotFound();

        return PartialView("_ModalAddProduto", produto);
    }
}