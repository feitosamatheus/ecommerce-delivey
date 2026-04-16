using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ecommerce.MVC.Config;
using Ecommerce.MVC.Models.Admin;
using Ecommerce.MVC.Enums;

namespace Ecommerce.MVC.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "administrador")]
public class DashboardController : Controller
{
    private readonly DatabaseContext _context;

    public DashboardController(DatabaseContext context)
    {
        _context = context;
    }

    public IActionResult EmConstrucao()
    {
        return View();
    }

    public async Task<IActionResult> ProdutosVendidos()
    {
        ViewData["Title"] = "Produtos Vendidos";
        ViewData["page"] = "Produtos";

        var produtosVendidos = await _context.PedidoItens
            .Include(pi => pi.Pedido)
            .Where(pi => pi.Pedido.Status == EPedidoStatus.Confirmado
                    || pi.Pedido.Status == EPedidoStatus.EmPreparo
                    || pi.Pedido.Status == EPedidoStatus.Pronto
                    || pi.Pedido.Status == EPedidoStatus.Concluido)
            .GroupBy(pi => pi.ProdutoNomeSnapshot)
            .Select(g => new ProdutosVendidosViewModel
            {
                NomeProduto = g.Key,
                QuantidadeTotal = g.Sum(x => x.Quantidade),
                PrecoUnitario = g.First().PrecoBaseSnapshot,
                TotalVendido = g.Sum(x => x.TotalLinha),
                NumeroPedidos = g.Select(x => x.PedidoId).Distinct().Count(),
                UltimaVenda = g.Max(x => x.Pedido.CriadoEmUtc)
            })
            .OrderByDescending(x => x.QuantidadeTotal)
            .ToListAsync();

        var modelo = new DashboardProdutosVendidosViewModel
        {
            Produtos = produtosVendidos,
            TotalProdutosVendidos = produtosVendidos.Sum(x => x.QuantidadeTotal),
            TotalReceita = produtosVendidos.Sum(x => x.TotalVendido),
            TotalPedidos = produtosVendidos.Sum(x => x.NumeroPedidos)
        };

        return View(modelo);
    }

    public async Task<IActionResult> DetalhesProduto(string nomeProduto)
    {
        if (string.IsNullOrEmpty(nomeProduto))
        {
            return NotFound();
        }

        ViewData["Title"] = $"Detalhes - {nomeProduto}";
        ViewData["page"] = $"Detalhes do Produto";

        // Buscar informações gerais do produto
        var produtoInfo = await _context.PedidoItens
            .Where(pi => pi.ProdutoNomeSnapshot == nomeProduto)
            .GroupBy(pi => pi.ProdutoNomeSnapshot)
            .Select(g => new
            {
                NomeProduto = g.Key,
                QuantidadeTotal = g.Sum(x => x.Quantidade),
                PrecoUnitario = g.First().PrecoBaseSnapshot,
                TotalVendido = g.Sum(x => x.TotalLinha),
                NumeroPedidos = g.Select(x => x.PedidoId).Distinct().Count(),
                UltimaVenda = g.Max(x => x.Pedido.CriadoEmUtc)
            })
            .FirstOrDefaultAsync();

        if (produtoInfo == null)
        {
            return NotFound();
        }

        // Buscar todos os pedidos que contêm este produto
        var pedidos = await _context.PedidoItens
            .Include(pi => pi.Pedido)
            .ThenInclude(p => p.Cliente)
            .Where(pi => pi.ProdutoNomeSnapshot == nomeProduto)
            .Select(pi => new PedidoProdutoViewModel
            {
                PedidoId = pi.Pedido.Id,
                CodigoPedido = pi.Pedido.Codigo,
                NomeCliente = pi.Pedido.Cliente.Nome,
                DataPedido = pi.Pedido.CriadoEmUtc,
                QuantidadeProduto = pi.Quantidade,
                PrecoUnitario = pi.PrecoBaseSnapshot,
                TotalProduto = pi.TotalLinha,
                StatusPedido = pi.Pedido.Status.ToString(),
                MetodoEntrega = pi.Pedido.MetodoEntrega
            })
            .OrderByDescending(p => p.DataPedido)
            .ToListAsync();

        var modelo = new DetalhesProdutoViewModel
        {
            NomeProduto = produtoInfo.NomeProduto,
            QuantidadeTotal = produtoInfo.QuantidadeTotal,
            PrecoUnitario = produtoInfo.PrecoUnitario,
            TotalVendido = produtoInfo.TotalVendido,
            NumeroPedidos = produtoInfo.NumeroPedidos,
            UltimaVenda = produtoInfo.UltimaVenda,
            Pedidos = pedidos
        };

        return View(modelo);
    }
}