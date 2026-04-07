using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Ecommerce.MVC.Areas.Admin.Services;
using Ecommerce.MVC.Config;
using Ecommerce.MVC.Entities;
using Ecommerce.MVC.Enums;
using Ecommerce.MVC.Models.Admin.Pedidos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.MVC.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "administrador,gerente")]
public class PedidosController : Controller
{
    private readonly DatabaseContext _db;
    private readonly IPedidoExportService _pedidoExportService;

    public PedidosController(DatabaseContext db, IPedidoExportService pedidoExportService)
    {
        _db = db;
        _pedidoExportService = pedidoExportService;
    }

    public async Task<IActionResult> Index(
    DateTime? dataInicio,
    DateTime? dataFim,
    string? status,
    string? tipoData,
    string? numeroPedido,
    string? cliente,
    int pagina = 1,
    int tamanhoPagina = 5,
    string? sortColumn = "CriadoEmUtc",
    string? sortDirection = "desc")
    {
        ViewData["page"] = "Pedido";

        dataInicio ??= DateTime.Today;
        dataFim ??= DateTime.Today;
        tipoData ??= "CriadoEmUtc";
        sortColumn ??= "CriadoEmUtc";
        sortDirection ??= "desc";

        if (pagina < 1)
            pagina = 1;

        if (tamanhoPagina <= 0)
            tamanhoPagina = 5;

        var query = _db.Pedidos
            .AsNoTracking()
            .Include(p => p.Cliente)
            .Include(p => p.Pagamentos)
            .AsQueryable();

        var inicioLocal = dataInicio.Value.Date;
        var fimLocal = dataFim.Value.Date.AddDays(1);

        var inicioUtc = DateTime.SpecifyKind(inicioLocal, DateTimeKind.Local).ToUniversalTime();
        var fimUtc = DateTime.SpecifyKind(fimLocal, DateTimeKind.Local).ToUniversalTime();

        if (tipoData == "HorarioRetirada")
            query = query.Where(p => p.HorarioRetirada >= inicioUtc && p.HorarioRetirada < fimUtc);
        else
            query = query.Where(p => p.CriadoEmUtc >= inicioUtc && p.CriadoEmUtc < fimUtc);

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<EPedidoStatus>(status, out var statusEnum))
        {
            query = query.Where(p => p.Status == statusEnum);
        }

        if (!string.IsNullOrWhiteSpace(numeroPedido))
        {
            var termo = numeroPedido.Trim();

            query = query.Where(p =>
                EF.Functions.ILike(p.Codigo, $"%{termo}%")
            );
        }

        if (!string.IsNullOrWhiteSpace(cliente))
        {
            cliente = cliente.Trim();

            var clienteNormalizado = cliente.ToLower();
            var cpfSomenteNumeros = new string(cliente.Where(char.IsDigit).ToArray());

            query = query.Where(p =>
                (p.Cliente != null &&
                p.Cliente.Nome != null &&
                p.Cliente.Nome.ToLower().Contains(clienteNormalizado))
                ||
                (!string.IsNullOrEmpty(cpfSomenteNumeros) &&
                p.Cliente != null &&
                p.Cliente.CPF != null &&
                p.Cliente.CPF.Replace(".", "").Replace("-", "").Replace("/", "").Contains(cpfSomenteNumeros))
            );
        }

        var pedidosFiltrados = await query.ToListAsync();

        var resumoTotalPedidos = pedidosFiltrados.Sum(p => p.Total);
        var resumoTotalPago = pedidosFiltrados.Sum(p => p.ValorPago);
        var resumoTotalEmAberto = pedidosFiltrados.Sum(p => p.ValorEmAberto);
        var resumoQtdAguardandoPagamento = pedidosFiltrados.Count(p => p.Status == EPedidoStatus.AguardandoPagamento);
        var resumoQtdQuitados = pedidosFiltrados.Count(p => p.PedidoQuitado);

        query = (sortColumn, sortDirection.ToLower()) switch
        {
            ("Codigo", "asc") => query.OrderBy(p => p.Codigo),
            ("Codigo", "desc") => query.OrderByDescending(p => p.Codigo),

            ("Cliente", "asc") => query.OrderBy(p => p.Cliente!.Nome),
            ("Cliente", "desc") => query.OrderByDescending(p => p.Cliente!.Nome),

            ("Total", "asc") => query.OrderBy(p => p.Total),
            ("Total", "desc") => query.OrderByDescending(p => p.Total),

            ("Status", "asc") => query.OrderBy(p => p.Status),
            ("Status", "desc") => query.OrderByDescending(p => p.Status),

            ("HorarioRetirada", "asc") => query.OrderBy(p => p.HorarioRetirada),
            ("HorarioRetirada", "desc") => query.OrderByDescending(p => p.HorarioRetirada),

            ("CriadoEmUtc", "asc") => query.OrderBy(p => p.CriadoEmUtc),
            _ => query.OrderByDescending(p => p.CriadoEmUtc)
        };

        var totalRegistros = await query.CountAsync();

        var pedidos = await query
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync();

        var vm = new PedidosIndexViewModel
        {
            Itens = pedidos,
            PaginaAtual = pagina,
            TamanhoPagina = tamanhoPagina,
            TotalRegistros = totalRegistros,
            DataInicio = dataInicio,
            DataFim = dataFim,
            Status = status,
            TipoData = tipoData,
            SortColumn = sortColumn,
            SortDirection = sortDirection,
            NumeroPedido = numeroPedido,
            Cliente = cliente,

            ResumoTotalPedidos = resumoTotalPedidos,
            ResumoTotalPago = resumoTotalPago,
            ResumoTotalEmAberto = resumoTotalEmAberto,
            ResumoQtdAguardandoPagamento = resumoQtdAguardandoPagamento,
            ResumoQtdQuitados = resumoQtdQuitados
        };

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return PartialView("_PedidosLista", vm);

        return View(vm);
    }

    public async Task<IActionResult> DetailsModal(Guid id)
    {
        var pedido = await _db.Pedidos
            .AsNoTracking()
            .Include(p => p.Cliente)
            .Include(p => p.Itens)
                .ThenInclude(i => i.Acompanhamentos)
            .Include(p => p.Pagamentos)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pedido == null)
            return NotFound();

        return PartialView("_DetailsModal", pedido);
    }

    //[HttpPost]
    //public async Task<IActionResult> AtualizarStatus(Guid id, EPedidoStatus status)
    //{
    //    var pedido = await _db.Pedidos.FirstOrDefaultAsync(p => p.Id == id);

    //    if (pedido == null)
    //        return NotFound(new { sucesso = false, mensagem = "Pedido não encontrado." });

    //    pedido.Status = status;

    //    await _db.SaveChangesAsync();

    //    return Json(new
    //    {
    //        sucesso = true,
    //        mensagem = "Status atualizado com sucesso.",
    //        status = pedido.Status.ToString(),
    //        id = pedido.Id
    //    });
    //}

    [HttpPost]
    public async Task<IActionResult> AtualizarStatus(Guid id, EPedidoStatus status)
    {
        var pedido = await _db.Pedidos.FirstOrDefaultAsync(p => p.Id == id);

        if (pedido == null)
            return NotFound(new { sucesso = false, mensagem = "Pedido não encontrado." });

        pedido.Status = status;

        switch (status)
        {
            case EPedidoStatus.Rascunho:
            case EPedidoStatus.AguardandoPagamento:
            case EPedidoStatus.Confirmado:
                // limpa apenas etapas futuras
                pedido.EmpreparoEmUtc = null;
                pedido.ProntoEmUtc = null;
                pedido.ConcluidoEmUtc = null;
                pedido.EntregueEmUtc = null;
                break;

            case EPedidoStatus.EmPreparo:
                // marca a etapa atual se ainda não existir
                pedido.EmpreparoEmUtc ??= DateTime.UtcNow;

                // limpa apenas etapas à frente
                pedido.ProntoEmUtc = null;
                pedido.ConcluidoEmUtc = null;
                pedido.EntregueEmUtc = null;
                break;

            case EPedidoStatus.Pronto:
                // preserva início do preparo; cria se não existir
                pedido.EmpreparoEmUtc ??= DateTime.UtcNow;

                // marca pronto se ainda não existir
                pedido.ProntoEmUtc ??= DateTime.UtcNow;

                // limpa apenas etapas à frente
                pedido.ConcluidoEmUtc = null;
                pedido.EntregueEmUtc = null;
                break;

            case EPedidoStatus.Concluido:
                // garante histórico anterior
                pedido.EmpreparoEmUtc ??= DateTime.UtcNow;
                pedido.ProntoEmUtc ??= DateTime.UtcNow;

                // marca conclusão/entrega
                pedido.ConcluidoEmUtc ??= DateTime.UtcNow;
                pedido.EntregueEmUtc ??= DateTime.UtcNow;
                break;

            case EPedidoStatus.Cancelado:
                // não altera histórico por padrão
                // se quiser limpar algo no cancelamento, faça aqui
                break;
        }

        await _db.SaveChangesAsync();

        return Json(new
        {
            sucesso = true,
            mensagem = "Status atualizado com sucesso.",
            status = pedido.Status.ToString(),
            id = pedido.Id,
            empreparoEmUtc = pedido.EmpreparoEmUtc,
            prontoEmUtc = pedido.ProntoEmUtc,
            concluidoEmUtc = pedido.ConcluidoEmUtc,
            entregueEmUtc = pedido.EntregueEmUtc
        });
    }

    //--------------------------------------------------------------------------------

    [HttpGet("ExportarPdf/{id:guid}")]
    public async Task<IActionResult> ExportarPdf(Guid id, CancellationToken ct)
    {
        var pedido = await ObterPedidoCompleto(id, ct);

        if (pedido == null)
            return NotFound();

        var arquivo = _pedidoExportService.GerarPdf(pedido);

        return File(
            arquivo,
            "application/pdf",
            $"pedido-{pedido.Codigo}.pdf");
    }

    [HttpGet("ExportarExcel/{id:guid}")]
    public async Task<IActionResult> ExportarExcel(Guid id, CancellationToken ct)
    {
        var pedido = await ObterPedidoCompleto(id, ct);

        if (pedido == null)
            return NotFound();

        var arquivo = _pedidoExportService.GerarExcel(pedido);

        return File(
            arquivo,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"pedido-{pedido.Codigo}.xlsx");
    }

    private async Task<Ecommerce.MVC.Entities.Pedido?> ObterPedidoCompleto(Guid id, CancellationToken ct)
    {
        return await _db.Pedidos
            .Include(p => p.Cliente)
            .Include(p => p.Pagamentos)
            .Include(p => p.Itens)
                .ThenInclude(i => i.Acompanhamentos)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    [HttpPost]
    public async Task<IActionResult> ExcluirPagamento(Guid id, Guid adminId, string senha)
    {
        if (adminId == Guid.Empty)
            return BadRequest(new { success = false, message = "Selecione um administrador." });

        if (string.IsNullOrWhiteSpace(senha))
            return BadRequest(new { success = false, message = "A senha do administrador é obrigatória." });

        var admin = await _db.Clientes
            .FirstOrDefaultAsync(u => u.Id == adminId && u.Role == "administrador" && u.Ativo);

        if (admin == null)
            return BadRequest(new { success = false, message = "Administrador não encontrado." });

        bool senhaValida = BCrypt.Net.BCrypt.Verify(senha, admin.SenhaHash);

        if (!senhaValida)
            return BadRequest(new { success = false, message = "Senha do administrador inválida." });

        var pagamento = await _db.PedidoPagamentos
            .Include(p => p.Pedido)
            .ThenInclude(p => p.Pagamentos)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pagamento == null)
            return NotFound(new { success = false, message = "Pagamento não encontrado." });

        if (pagamento.Excluido)
            return BadRequest(new { success = false, message = "Pagamento já foi excluído." });

        if (User?.Identity == null || !User.Identity.IsAuthenticated)
        {
            return Unauthorized(); // ou NotFound(), mas o correto semanticamente é Unauthorized
        }

        var usuarioLogado = User.Identity.Name;

        if (string.IsNullOrWhiteSpace(usuarioLogado))
        {
            return NotFound(new { success = false, message = "Usuário não identificado." });
        }

        var usuarioLogadoIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Guid? usuarioLogadoId = null;

        if (!string.IsNullOrWhiteSpace(usuarioLogadoIdClaim) && Guid.TryParse(usuarioLogadoIdClaim, out var idUsuario))
        {
            usuarioLogadoId = idUsuario;
        }

        pagamento.Excluido = true;
        pagamento.ValidadoPor = admin.Nome;
        pagamento.ValidadoPorId = admin.Id;
        pagamento.ExcluidoEmUtc = DateTime.UtcNow;
        pagamento.ExcluidoPor = string.IsNullOrWhiteSpace(usuarioLogado) ? admin.Nome : usuarioLogado;
        pagamento.ExcluidoPorId = usuarioLogadoId ?? admin.Id;

        var pedido = pagamento.Pedido;

        var totalPago = pedido.Pagamentos
            .Where(p => !p.Excluido && p.Id != pagamento.Id)
            .Sum(p => p.Valor);

        if (pedido.Status != EPedidoStatus.EmPreparo &&
            pedido.Status != EPedidoStatus.Pronto &&
            pedido.Status != EPedidoStatus.Concluido)
        {
            if (totalPago >= pedido.ValorEntrada)
                pedido.Status = EPedidoStatus.Confirmado;
            else
                pedido.Status = EPedidoStatus.AguardandoPagamento;
        }

        await _db.SaveChangesAsync();

        return Json(new
        {
            success = true,
            pedidoId = pagamento.PedidoId,
            message = "Pagamento excluído com sucesso."
        });
    }

    [HttpGet]
    public async Task<IActionResult> ListarAdministradores()
    {
        var admins = await _db.Clientes
            .Where(u => u.Role == "administrador" && u.Ativo)
            .OrderBy(u => u.Nome)
            .Select(u => new
            {
                id = u.Id,
                nome = u.Nome
            })
            .ToListAsync();

        return Json(admins);
    }

    public async Task<IActionResult> AdicionarPagamento(Guid id)
    {
        var pedido = await _db.Pedidos
            .AsNoTracking()
            .Include(p => p.Cliente)
            .Include(p => p.Itens)
                .ThenInclude(i => i.Acompanhamentos)
            .Include(p => p.Pagamentos)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pedido == null)
        {
            return NotFound();
        }

        // Criar um novo PedidoPagamento para preencher o formulário
        var pagamento = new PedidoPagamento
        {
            PedidoId = pedido.Id,
            Valor = pedido.ValorEmAberto, // Valor inicial (você pode ajustar isso conforme necessário)
            TipoPagamento = "PIX", // Default, pode ser alterado
            Gateway = "ASAAS" // Default, pode ser alterado
        };

        // Retornar a partial view com o formulário para adicionar pagamento
        return PartialView("_AdicionarPagamento", pagamento);
    }

    [HttpPost]
    public async Task<IActionResult> SalvarPagamento(PedidoPagamento pagamento)
    {
        var valorTexto = Request.Form["Valor"].ToString();

        if (!string.IsNullOrWhiteSpace(valorTexto))
        {
            valorTexto = valorTexto
                .Replace("R$", "")
                .Replace(" ", "")
                .Replace(".", "")
                .Replace(",", ".");

            if (decimal.TryParse(valorTexto, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var valorConvertido))
            {
                pagamento.Valor = valorConvertido;
            }
            else
            {
                ModelState.AddModelError("Valor", "Valor inválido.");
            }
        }

        ModelState.Remove("Valor");

        if (pagamento.Valor <= 0)
        {
            ModelState.AddModelError("Valor", "Informe um valor maior que zero.");
        }

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var pedido = await _db.Pedidos
            .Include(p => p.Pagamentos)
            .FirstOrDefaultAsync(p => p.Id == pagamento.PedidoId);

        if (pedido == null)
            return NotFound();

        var proximaSequencia = pedido.Pagamentos.Where(p => !p.Excluido).Any()
        ? pedido.Pagamentos.Where(p => !p.Excluido).Max(p => p.Sequencia) + 1
        : 1;

        pagamento.Sequencia = proximaSequencia;

        // status recebido
        pagamento.Status = EStatusPagamento.Received;
        pagamento.PagoEmUtc = DateTime.UtcNow;

        _db.PedidoPagamentos.Add(pagamento);

        var totalPago = pedido.Pagamentos.Sum(p => p.Valor) + pagamento.Valor;

        // NÃO altera status se já estiver em preparo ou pronto
        if (pedido.Status != EPedidoStatus.EmPreparo &&
            pedido.Status != EPedidoStatus.Pronto)
        {
            if (totalPago >= pedido.ValorEntrada)
            {
                pedido.Status = EPedidoStatus.Confirmado;
            }
            else
            {
                pedido.Status = EPedidoStatus.AguardandoPagamento;
            }
        }

        await _db.SaveChangesAsync();

        return Ok();
    }

}

