using Ecommerce.MVC.Config;
using Ecommerce.MVC.Entities;
using Ecommerce.MVC.Enums;
using Ecommerce.MVC.Helpers;
using Ecommerce.MVC.Interfaces;
using Ecommerce.MVC.Models;
using Ecommerce.MVC.Models.Pedidos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

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


    #region Pedidos em andamento modal
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> BuscarModalPedidosEmAndamento(CancellationToken ct)
    {
        var clienteId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        var totalPedidosAndamento = await _pedidoService.ObterQuantidadeEmAndamentoAsync(clienteId, HttpContext.RequestAborted);

        ViewBag.TotalPedidos = totalPedidosAndamento;

        var pedidos = await _pedidoService.ListarEmAndamentoAsync(clienteId, ct);

        return PartialView("_ModalPedidosEmAndamento", pedidos);
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> EmAndamento(string? status, string? codigo, int pagina = 1, CancellationToken ct = default)
    {
        // Chama a lógica comum de busca
        var vm = await ObterViewModelPedidosAndamento(status, codigo, pagina, ct);

        // Retorna a View completa (com Layout, Filtros, etc.)
        return View(vm);
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetListaPedidos(string? status, string? codigo, int pagina = 1, CancellationToken ct = default)
    {
        // Chama a mesma lógica de busca
        var vm = await ObterViewModelPedidosAndamento(status, codigo, pagina, ct);

        // Retorna APENAS o HTML da lista (Partial)
        return PartialView("_ListaPedidos", vm);
    }

    // MÉTODO PRIVADO (O Coração da Busca)
    private async Task<PedidosEmAndamentoPaginaViewModel> ObterViewModelPedidosAndamento(string? status, string? codigo, int pagina, CancellationToken ct)
    {
        var clienteId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        int itensPorPagina = 6;

        // 1. Query base (Removendo Rascunho)
        var query = _db.Pedidos
            .AsNoTracking()
            .Where(p => p.ClienteId == clienteId && p.Status != EPedidoStatus.Rascunho);

        // 2. Aplicação dos Filtros
        bool isFiltrado = false;

        if (!string.IsNullOrWhiteSpace(codigo))
        {
            isFiltrado = true;
            var codLimpo = codigo.Replace("#", "").Trim();
            query = query.Where(p => p.Codigo.Contains(codLimpo));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            isFiltrado = true;
            if (Enum.TryParse<EPedidoStatus>(status, out var statusEnum))
            {
                query = query.Where(p => p.Status == statusEnum);
            }
        }
        else if (string.IsNullOrWhiteSpace(codigo))
        {
            // Se não houver filtro de código nem de status, 
            // mostramos o padrão: O que não foi finalizado/cancelado
            query = query.Where(p => p.Status != EPedidoStatus.Concluido && p.Status != EPedidoStatus.Cancelado);
        }

        // 3. Paginação e Projeção
        var totalItens = await query.CountAsync(ct);
        var pedidos = await query
            .OrderByDescending(p => p.CriadoEmUtc)
            .Skip((pagina - 1) * itensPorPagina)
            .Take(itensPorPagina)
            .Select(p => new PedidosEmAndamentoResumoViewModel
            {
                Codigo = p.Codigo,
                CriadoEmUtc = p.CriadoEmUtc,
                Status = p.Status,
                StatusTexto = MapStatusTexto(p.Status), // Certifique-se que este método existe no controller
                Total = p.Total
            })
            .ToListAsync(ct);

        return new PedidosEmAndamentoPaginaViewModel
        {
            TotalPedidos = totalItens,
            Pedidos = pedidos,
            PaginaAtual = pagina,
            TotalPaginas = (int)Math.Ceiling(totalItens / (double)itensPorPagina),
            StatusFiltro = status,
            CodigoFiltro = codigo,
            IsFiltrado = isFiltrado
        };
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> DetalhesEmAndamento(string codigo, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(codigo))
            return BadRequest("Código do pedido não informado.");

        var clienteId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var pedidoEntity = await _db.Pedidos
            .AsNoTracking()
            .Where(p => p.ClienteId == clienteId
                        && p.Codigo == codigo
                        && p.Status != EPedidoStatus.Rascunho
                        && p.Status != EPedidoStatus.Concluido
                        && p.Status != EPedidoStatus.Cancelado)
            .Select(p => new
            {
                p.Codigo,
                p.CriadoEmUtc,
                p.Status,
                p.MetodoEntrega,
                p.HorarioRetirada,
                p.Observacao,
                p.Subtotal,
                p.Total,
                Pagamento = p.PedidoPagamento == null ? null : new
                {
                    p.PedidoPagamento.Gateway,
                    p.PedidoPagamento.TipoPagamento,
                    p.PedidoPagamento.Status,
                    p.PedidoPagamento.PixPayload,
                    p.PedidoPagamento.PixEncodedImage,
                    p.PedidoPagamento.PixExpirationDate,
                    p.PedidoPagamento.GatewayPaymentId,
                    p.PedidoPagamento.InvoiceUrl
                },
                Itens = p.Itens.Select(i => new
                {
                    i.ProdutoNomeSnapshot,
                    i.Quantidade,
                    i.PrecoBaseSnapshot,
                    i.TotalLinha,
                    Acompanhamentos = i.Acompanhamentos.Select(a => new
                    {
                        a.NomeSnapshot,
                        a.PrecoSnapshot
                    }).ToList()
                }).ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (pedidoEntity == null)
            return NotFound("Pedido não encontrado.");

        var pedido = new PedidosEmAndamentoViewModel
        {
            Codigo = pedidoEntity.Codigo,
            CriadoEmUtc = pedidoEntity.CriadoEmUtc,
            Status = pedidoEntity.Status,
            StatusTexto = MapStatusTexto(pedidoEntity.Status),
            Step = MapStatusToStep(pedidoEntity.Status),
            MetodoEntrega = pedidoEntity.MetodoEntrega,
            HorarioRetirada = pedidoEntity.HorarioRetirada,
            Observacao = pedidoEntity.Observacao,
            Subtotal = pedidoEntity.Subtotal,
            Total = pedidoEntity.Total,
            ValorSinal = Math.Round(pedidoEntity.Total * 0.50m, 2),
            ValorRestanteRetirada = pedidoEntity.Total - Math.Round(pedidoEntity.Total * 0.50m, 2),

            Pagamento = pedidoEntity.Pagamento == null ? null : new PedidoPagamentoViewModel
            {
                Gateway = pedidoEntity.Pagamento.Gateway,
                TipoPagamento = pedidoEntity.Pagamento.TipoPagamento,
                Status = pedidoEntity.Pagamento.Status,
                StatusPagamento = MapStatusPagamentoTexto(pedidoEntity.Pagamento.Status),
                PixCopiaCola = pedidoEntity.Pagamento.PixPayload,
                PixQrCodeUrl = pedidoEntity.Pagamento.PixEncodedImage,
                PixExpiraEm = pedidoEntity.Pagamento.PixExpirationDate.HasValue
                    ? pedidoEntity.Pagamento.PixExpirationDate.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
                    : null,
                PixIdentificador = pedidoEntity.Pagamento.GatewayPaymentId,
                PixBeneficiario = "Barriga Cheia Ltda.",
                InvoiceUrl = pedidoEntity.Pagamento.InvoiceUrl
            },

            Itens = pedidoEntity.Itens.Select(i => new PedidosEmAndamentoItemViewModel
            {
                ProdutoNome = i.ProdutoNomeSnapshot,
                Quantidade = i.Quantidade,
                PrecoBase = i.PrecoBaseSnapshot,
                TotalLinha = i.TotalLinha,
                Acompanhamentos = i.Acompanhamentos.Select(a => new PedidosEmAndamentoItemAcompanhamentoViewModel
                {
                    Nome = a.NomeSnapshot,
                    Preco = a.PrecoSnapshot
                }).ToList()
            }).ToList()
        };

        return PartialView("_DetalhesPedidoEmAndamento", pedido);
    }

    private static string MapStatusTexto(EPedidoStatus status)
    {
        return status switch
        {
            EPedidoStatus.AguardandoPagamento => "Aguardando pagamento",
            EPedidoStatus.Confirmado => "Confirmado",
            EPedidoStatus.EmPreparo => "Em preparo",
            EPedidoStatus.Pronto => "Pronto",
            EPedidoStatus.Concluido => "Concluído",
            EPedidoStatus.Cancelado => "Cancelado",
            EPedidoStatus.Rascunho => "Rascunho",
            _ => status.ToString()
        };
    }

    private static int MapStatusToStep(EPedidoStatus status)
    {
        return status switch
        {
            EPedidoStatus.AguardandoPagamento => 1,
            EPedidoStatus.Confirmado => 2,
            EPedidoStatus.EmPreparo => 3,
            EPedidoStatus.Pronto => 4,
            EPedidoStatus.Concluido => 5,
            _ => 1
        };
    }

    private static string MapStatusPagamentoTexto(EStatusPagamento status)
    {
        return status switch
        {
            EStatusPagamento.Pending => "Aguardando pagamento",
            EStatusPagamento.Received => "Recebido",
            EStatusPagamento.Confirmed => "Confirmado",
            EStatusPagamento.Overdue => "Vencido",
            EStatusPagamento.Refunded => "Estornado",
            EStatusPagamento.ReceivedInCash => "Recebido em dinheiro",
            EStatusPagamento.RefundRequested => "Estorno solicitado",
            EStatusPagamento.RefundInProgress => "Estorno em processamento",
            EStatusPagamento.ChargebackRequested => "Chargeback solicitado",
            EStatusPagamento.ChargebackDispute => "Em disputa de chargeback",
            EStatusPagamento.AwaitingChargebackReversal => "Aguardando reversão de chargeback",
            EStatusPagamento.DunningRequested => "Cobrança solicitada",
            EStatusPagamento.DunningReceived => "Cobrança recebida",
            EStatusPagamento.AwaitingRiskAnalysis => "Aguardando análise de risco",
            _ => "Status não identificado"
        };
    }

    public class PedidosEmAndamentoResumoViewModel
    {
        public string Codigo { get; set; } = default!;
        public DateTime CriadoEmUtc { get; set; }
        public EPedidoStatus Status { get; set; }
        public string StatusTexto { get; set; } = default!;
        public decimal Total { get; set; }
    }

    public class PedidosEmAndamentoPaginaViewModel
    {
        public int TotalPedidos { get; set; }
        public IReadOnlyList<PedidosEmAndamentoResumoViewModel> Pedidos { get; set; } = new List<PedidosEmAndamentoResumoViewModel>();

        // Propriedades para Paginação e Filtro
        public int PaginaAtual { get; set; }
        public int TotalPaginas { get; set; }
        public string? StatusFiltro { get; set; }
        public string? CodigoFiltro { get; set; }
        public bool IsFiltrado { get; set; }
    }
    #endregion


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