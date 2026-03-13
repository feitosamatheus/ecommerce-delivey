using System;
using System.Linq;
using System.Threading.Tasks;
using Ecommerce.MVC.Config;
using Ecommerce.MVC.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.MVC.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Administrador")]
public class PedidosController : Controller
{
    private readonly DatabaseContext _db;

    public PedidosController(DatabaseContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var pedidos = await _db.Pedidos
            .AsNoTracking()
            .Include(p => p.Cliente)
            .OrderByDescending(p => p.CriadoEmUtc)
            .Take(100)
            .ToListAsync();

        return View(pedidos);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var pedido = await _db.Pedidos
            .AsNoTracking()
            .Include(p => p.Cliente)
            .Include(p => p.Itens)
                .ThenInclude(i => i.Acompanhamentos)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pedido == null) return NotFound();

        return View(pedido);
    }
}

