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
    private readonly IProdutoService _produtoService;
    private readonly DatabaseContext _db;

    public PedidoController(IProdutoService produtoService, DatabaseContext db)
    {
        _produtoService = produtoService;
        _db = db;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> ModalProduto(CancellationToken ct)
    {
        var clienteId = Guid.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value
        );

        var vm = await _produtoService.MontarModalAsync(HttpContext, clienteId, ct);

        return PartialView("FinalizacaoPedido", vm);
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
            .Include(p => p.Endereco) // se você tiver snapshot
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

                EnderecoTexto = p.Endereco == null
                    ? null
                    : $"{p.Endereco.Logradouro}, {p.Endereco.Numero} - {p.Endereco.Bairro} - {p.Endereco.Cidade}/{p.Endereco.Estado} - {p.Endereco.Cep}",

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

    public async Task<IActionResult> Confirmar([FromBody] ConfirmarPedidoRequest req, CancellationToken ct)
    {
        if (req == null) return BadRequest("Requisição inválida.");

        var clienteIdRaw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(clienteIdRaw)) return Unauthorized();

        var clienteId = Guid.Parse(clienteIdRaw);

        // 1) Carregar carrinho por token
        var token = CartTokenHelper.GetOrCreateToken(HttpContext);

        var carrinho = await _db.Carrinhos
            .Include(c => c.Itens)
                .ThenInclude(i => i.Acompanhamentos)
            .FirstOrDefaultAsync(c => c.Token == token, ct);

        if (carrinho == null || carrinho.Itens.Count == 0)
            return BadRequest("Carrinho vazio.");

        decimal subtotal = 0m;

        foreach (var item in carrinho.Itens)
        {
            var somaAcomp = item.Acompanhamentos?.Sum(a => a.PrecoSnapshot) ?? 0m;
            subtotal += (item.PrecoBaseSnapshot + somaAcomp) * item.Quantidade;
        }

        var total = subtotal;

        var horarioUtc = DateTime.SpecifyKind(
            req.HorarioRetirada,
            DateTimeKind.Utc
        );

        // 5) Criar Pedido + Itens (snapshot)
        var pedido = new Pedido
        {
            ClienteId = clienteId,
            MetodoEntrega = "Retirada",
            Pagamento = "Pix",
            Observacao = string.IsNullOrWhiteSpace(req.Observacao) ? null : req.Observacao.Trim(),
            Subtotal = subtotal,
            TaxaEntrega = 0m,
            Total = total,
            CriadoEmUtc = DateTime.UtcNow,
            Status = Enums.EPedidoStatus.AguardandoPagamento,
            HorarioRetirada = horarioUtc
        };


        foreach (var ci in carrinho.Itens)
        {
            var somaAcomp = ci.Acompanhamentos?.Sum(a => a.PrecoSnapshot) ?? 0m;
            var totalLinha = (ci.PrecoBaseSnapshot + somaAcomp) * ci.Quantidade;

            var pedidoItem = new PedidoItem
            {
                ProdutoId = ci.ProdutoId,
                ProdutoNomeSnapshot = ci.ProdutoNomeSnapshot,
                PrecoBaseSnapshot = ci.PrecoBaseSnapshot,
                Quantidade = ci.Quantidade,
                PrecoAcompanhamentosSnapshot = somaAcomp,
                TotalLinha = totalLinha
            };

            if (ci.Acompanhamentos?.Any() == true)
            {
                foreach (var a in ci.Acompanhamentos)
                {
                    pedidoItem.Acompanhamentos.Add(new PedidoItemAcompanhamento
                    {
                        AcompanhamentoId = a.AcompanhamentoId,
                        CategoriaId = a.CategoriaId,
                        NomeSnapshot = a.NomeSnapshot,
                        PrecoSnapshot = a.PrecoSnapshot
                    });
                }
            }

            pedido.Itens.Add(pedidoItem);
        }

        // ---- A PARTIR DAQUI: salvar pedido + excluir carrinho (ATÔMICO) ----
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        _db.Pedidos.Add(pedido);

        // 6) Excluir carrinho inteiro (itens + acompanhamentos + carrinho)
        // Se você já tiver cascade delete configurado, pode trocar por: _db.Carrinhos.Remove(carrinho);
        var itensIds = carrinho.Itens.Select(i => i.Id).ToList();

        if (itensIds.Count > 0)
        {
            var acomps = await _db.Set<CarrinhoItemAcompanhamento>()
                .Where(a => itensIds.Contains(a.CarrinhoItemId))
                .ToListAsync(ct);

            _db.Set<CarrinhoItemAcompanhamento>().RemoveRange(acomps);
        }

        _db.Set<CarrinhoItem>().RemoveRange(carrinho.Itens);
        _db.Carrinhos.Remove(carrinho);

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        // 7) Limpar token/cookie do carrinho (recomendado)
        CartTokenHelper.ClearToken(HttpContext);

        return Ok(new { ok = true, pedidoId = pedido.Id });
    }


    private static List<DateTime> GerarHorariosRetirada(int tempoPreparoMinutos)
    {
        var resultado = new List<DateTime>();

        var agora = DateTime.Now;
        var horarioMinimo = agora.AddMinutes(tempoPreparoMinutos);

        var horarioInicial = AjustarParaHorarioFuncionamento(horarioMinimo);

        for (int dia = 0; dia < 3; dia++)
        {
            var data = horarioInicial.Date.AddDays(dia);

            var abertura = data.AddHours(8);
            var fechamento = data.AddHours(22);

            var inicio = dia == 0 ? horarioInicial : abertura;

            for (var h = inicio; h <= fechamento; h = h.AddMinutes(30))
                resultado.Add(h);
        }

        return resultado;
    }

    private static DateTime AjustarParaHorarioFuncionamento(DateTime dataHora)
    {
        var abertura = dataHora.Date.AddHours(8);
        var fechamento = dataHora.Date.AddHours(22);

        if (dataHora < abertura)
            return abertura;

        if (dataHora > fechamento)
            return dataHora.Date.AddDays(1).AddHours(8);

        return dataHora;
    }
}