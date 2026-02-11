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
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var clienteId = Guid.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        var totalPedidosAndamento =
            await _pedidoService.ObterQuantidadeEmAndamentoAsync(
                clienteId,
                HttpContext.RequestAborted
            );

        ViewBag.TotalPedidos = totalPedidosAndamento;

        var pedidos = await _pedidoService.ListarEmAndamentoAsync(clienteId, ct);

        return View(pedidos);
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

        var totalPedidosAndamento =
            await _pedidoService.ObterQuantidadeEmAndamentoAsync(
                clienteId,
                HttpContext.RequestAborted
            );

        ViewBag.TotalPedidos = totalPedidosAndamento;

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

    [HttpPost]
    public async Task<JsonResult> ConfirmarPagamentoSinal(string pedidoId)
    {
        try
        {
            if (!Guid.TryParse(pedidoId, out Guid guidPedido))
            {
                return Json(new { success = false, message = "Formato de identificador inválido." });
            }

            // 1. Busca o pedido no banco (ajuste 'Pedido' e 'Codigo' para seus nomes reais)
            var pedido = await _db.Pedidos.FirstOrDefaultAsync(p => p.Id == guidPedido);

            if (pedido == null)
            {
                return Json(new { success = false, message = "Pedido não encontrado no sistema." });
            }

            // 2. Verifica se já não está confirmado para evitar processamento duplicado
            if (pedido.Status == Enums.EPedidoStatus.Confirmado)
            {
                return Json(new
                {
                    success = true,
                    // Redireciona para a Home com um marcador de sucesso
                    redirectUrl = Url.Action("Index", "Home", new { confirmado = true }),
                    message = "Pagamento confirmado!"
                });
            }

            // 3. Atualiza o status para 2 (Confirmado)
            pedido.Status = Enums.EPedidoStatus.Confirmado;
            //pedido.DataPagamentoSinal = DateTime.Now; // Recomendado salvar a data do pagamento

            _db.Pedidos.Update(pedido);
            await _db.SaveChangesAsync();

            // 4. Retorna sucesso para o AJAX
            return Json(new
            {
                success = true,
                // Redireciona para a Home com um marcador de sucesso
                redirectUrl = Url.Action("Index", "Home", new { confirmado = true }),
                message = "Pagamento confirmado!"
            });
        }
        catch (Exception ex)
        {
            // Logar o erro ex (usando Serilog ou ILogger)
            return Json(new { success = false, message = "Erro ao processar atualização: " + ex.Message });
        }
    }
}