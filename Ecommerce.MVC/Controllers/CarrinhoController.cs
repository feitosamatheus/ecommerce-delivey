using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ecommerce.MVC.Config;
using Ecommerce.MVC.Interfaces;
using Ecommerce.MVC.Models.Produtos;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.MVC.Controllers;

public class CarrinhoController : Controller
{
    private readonly ICarrinhoService _carrinho;
    private readonly DatabaseContext _context;


    public CarrinhoController(ICarrinhoService carrinho, DatabaseContext db)
    {
        _carrinho = carrinho;
        _context = db;
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

    [HttpPost]
    public async Task<IActionResult> AtualizarQuantidade(Guid itemId, int quantidade)
    {
        try
        {
            if (quantidade < 1)
                quantidade = 1;

            var item = await _context.CarrinhoItems.FindAsync(itemId);

            if (item == null)
            {
                return Json(new
                {
                    sucesso = false,
                    mensagem = "Item do carrinho não encontrado."
                });
            }

            item.Quantidade = quantidade;

            _context.CarrinhoItems.Update(item);
            await _context.SaveChangesAsync();

            return Json(new
            {
                sucesso = true,
                mensagem = "Quantidade atualizada com sucesso."
            });
        }
        catch (Exception ex)
        {
            return Json(new
            {
                sucesso = false,
                mensagem = $"Erro ao atualizar quantidade: {ex.Message}"
            });
        }
    }

    [HttpPost]
    public async Task<IActionResult> RemoverItem(Guid itemId)
    {
        try
        {
            var item = await _context.CarrinhoItems.FindAsync(itemId);

            if (item == null)
            {
                return Json(new
                {
                    sucesso = false,
                    mensagem = "Item não encontrado."
                });
            }

            _context.CarrinhoItems.Remove(item);
            await _context.SaveChangesAsync();

            return Json(new
            {
                sucesso = true
            });
        }
        catch (Exception ex)
        {
            return Json(new
            {
                sucesso = false,
                mensagem = ex.Message
            });
        }
    }
}