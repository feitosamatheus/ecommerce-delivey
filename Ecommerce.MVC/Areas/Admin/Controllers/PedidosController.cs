using System;
using System.Linq;
using System.Threading.Tasks;
using Ecommerce.MVC.Config;
using Ecommerce.MVC.Entities;
using Ecommerce.MVC.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.MVC.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "administrador")]
public class PedidosController : Controller
{
    private readonly DatabaseContext _db;

    public PedidosController(DatabaseContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(DateTime? dataInicio, DateTime? dataFim, string? status, string? tipoData)
    {
        dataInicio ??= DateTime.Today;
        dataFim ??= DateTime.Today;
        tipoData ??= "CriadoEmUtc";

        var query = _db.Pedidos
            .AsNoTracking()
            .Include(p => p.Cliente)
            .AsQueryable();

        var inicioLocal = dataInicio.Value.Date;
        var fimLocal = dataFim.Value.Date.AddDays(1);

        var inicioUtc = DateTime.SpecifyKind(inicioLocal, DateTimeKind.Local).ToUniversalTime();
        var fimUtc = DateTime.SpecifyKind(fimLocal, DateTimeKind.Local).ToUniversalTime();

        if (tipoData == "HorarioRetirada")
        {
            query = query.Where(p => p.HorarioRetirada >= inicioUtc && p.HorarioRetirada < fimUtc);
        }
        else
        {
            query = query.Where(p => p.CriadoEmUtc >= inicioUtc && p.CriadoEmUtc < fimUtc);
        }

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(p => p.Status.ToString() == status);

        query = tipoData == "HorarioRetirada"
            ? query.OrderByDescending(p => p.HorarioRetirada)
            : query.OrderByDescending(p => p.CriadoEmUtc);

        var pedidos = await query
            .Take(100)
            .ToListAsync();

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return PartialView("_PedidosLista", pedidos);

        return View(pedidos);
    }

    public async Task<IActionResult> DetailsModal(Guid id)
    {
        var pedido = await _db.Pedidos
            .AsNoTracking()
            .Include(p => p.Cliente)
            .Include(p => p.Itens)
                .ThenInclude(i => i.Acompanhamentos)
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
}

