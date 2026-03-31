using System;
using System.Linq;
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
[Authorize(Roles = "administrador")]
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
    int pagina = 1,
    int tamanhoPagina = 5,
    string? sortColumn = "CriadoEmUtc",
    string? sortDirection = "desc")
    {
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

    public async Task<IActionResult> Details(Guid id)
    {
        var pedido = await _db.Pedidos
            .AsNoTracking()
            .Include(p => p.Cliente)
            .Include(p => p.Itens)
                .ThenInclude(i => i.Acompanhamentos)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pedido == null)
            return NotFound();

        return View(pedido);
    }

    [HttpPost]
    public async Task<IActionResult> AtualizarStatus(Guid id, EPedidoStatus status)
    {
        var pedido = await _db.Pedidos.FirstOrDefaultAsync(p => p.Id == id);

        if (pedido == null)
            return NotFound(new { sucesso = false, mensagem = "Pedido não encontrado." });

        pedido.Status = status;

        await _db.SaveChangesAsync();

        return Json(new
        {
            sucesso = true,
            mensagem = "Status atualizado com sucesso.",
            status = pedido.Status.ToString(),
            id = pedido.Id
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
}

