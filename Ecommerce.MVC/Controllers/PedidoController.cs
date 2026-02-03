using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Ecommerce.MVC.Config;
using Ecommerce.MVC.Entities;
using Ecommerce.MVC.Helpers;
using Ecommerce.MVC.Interfaces;
using Ecommerce.MVC.Models;
using Ecommerce.MVC.Models.Pedidos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ecommerce.MVC.Controllers;

public class PedidoController : Controller
{
    private readonly IPedidoService _pedidoService;
    private readonly IProdutoService _produtoService;
    private readonly DatabaseContext _db;

    public PedidoController(IPedidoService pedidoService, DatabaseContext db, IProdutoService produtoService)
    {
        _pedidoService = pedidoService;
        _db = db;
        _produtoService = produtoService;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> BuscarModalFinalizarPedido(CancellationToken ct)
    {
        var clienteId = Guid.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value
        );

        var vm = await _pedidoService.ObterDadosFinalizacaoPedidoAsync(HttpContext, clienteId, ct);

        return PartialView("_ModalFinalizarPedido", vm);
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> BuscarModalPedidosEmAndamento(CancellationToken ct)
    {
        var clienteId = Guid.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        var pedidos = await _pedidoService.ListarEmAndamentoAsync(clienteId, ct);

        return PartialView("_ModalPedidosEmAndamento", pedidos);
    }

    [HttpGet]
    public async Task<IActionResult> AcompanhamentoCliente(CancellationToken ct)
    {
        var clienteIdRaw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(clienteIdRaw)) return Unauthorized();

        var clienteId = Guid.Parse(clienteIdRaw);

        var pedidos = await _db.Pedidos
            .AsNoTracking()
            .Where(p => p.ClienteId == clienteId)
            .OrderByDescending(p => p.CriadoEmUtc)
            .Include(p => p.Itens)
                .ThenInclude(i => i.Acompanhamentos)
            .ToListAsync(ct);

        var vm = new AcompanhamentoPedidosViewModel
        {
            Pedidos = pedidos.Select(p => new AcompanhamentoPedidoItemVm
            {
                PedidoId = p.Id,
                CriadoEmUtc = p.CriadoEmUtc,
                MetodoEntrega = p.MetodoEntrega,
                Pagamento = p.Pagamento,
                Total = p.Total,
                TaxaEntrega = p.TaxaEntrega,
                Subtotal = p.Subtotal,
                Itens = p.Itens.Select(i => new AcompanhamentoPedidoProdutoVm
                {
                    ProdutoNome = i.ProdutoNomeSnapshot,
                    Quantidade = i.Quantidade,
                    TotalLinha = i.TotalLinha,
                    Acompanhamentos = i.Acompanhamentos?.Select(a => a.NomeSnapshot).ToList() ?? new List<string>()
                }).ToList()
            }).ToList()
        };

        return PartialView("_AcompanhamentoCliente", vm);
    }

    [HttpPost]
    public async Task<IActionResult> Confirmar([FromBody] ConfirmarPedidoRequest req, CancellationToken ct)
    {
        if (req == null) return BadRequest("Requisição inválida.");

        var clienteIdRaw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(clienteIdRaw)) return Unauthorized();

        if (!Guid.TryParse(clienteIdRaw, out var clienteId)) return Unauthorized();

        try
        {
            var result = await _pedidoService.ConfirmarAsync(HttpContext, clienteId, req, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception)
        {
            return StatusCode(500, "Erro interno ao confirmar o pedido.");
        }
    }
}