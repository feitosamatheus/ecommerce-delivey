using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ecommerce.MVC.Interfaces;
using Ecommerce.MVC.Models.Produtos;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.MVC.Controllers;

public class CarrinhoController : Controller
{
    private readonly ICarrinhoService _carrinho;

    public CarrinhoController(ICarrinhoService carrinho)
    {
        _carrinho = carrinho;
    }

    [HttpPost]
    public async Task<IActionResult> Adicionar([FromBody] AdicionarProdutoCarrinhoViewModel req, CancellationToken ct)
    {
        try
        {
            await _carrinho.AdicionarAsync(HttpContext, req, ct);

            return Ok(new
            {
                success = true,
                message = "Produto adicionado ao carrinho."
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
        }
    }
}