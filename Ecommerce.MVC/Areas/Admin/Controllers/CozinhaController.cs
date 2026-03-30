using Ecommerce.MVC.Config;
using Ecommerce.MVC.Entities;
using Ecommerce.MVC.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.MVC.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CozinhaController : Controller
    {
        private readonly DatabaseContext _db;

        public CozinhaController(DatabaseContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Painel da Cozinha";
            ViewData["Description"] = "Acompanhe os pedidos em produção e prontos para entrega/retirada.";

            var pedidos = await _db.Pedidos
                .AsNoTracking()
                .Include(p => p.Cliente)
                .Include(p => p.Itens)
                    .ThenInclude(i => i.Acompanhamentos)
                .Where(p =>
                    p.Status == EPedidoStatus.Confirmado ||
                    p.Status == EPedidoStatus.EmPreparo ||
                    p.Status == EPedidoStatus.Pronto ||
                    p.Status == EPedidoStatus.Concluido)
                .OrderBy(p => p.HorarioRetirada)
                .ThenByDescending(p => p.CriadoEmUtc)
                .ToListAsync();

            return View(pedidos);
        }

        public async Task<IActionResult> EmPreparo()
        {
            ViewData["Title"] = "Em Preparo";
            ViewData["Description"] = "Pedidos que estão atualmente em produção na cozinha.";

            var pedidos = await _db.Pedidos
                .AsNoTracking()
                .Include(p => p.Cliente)
                .Include(p => p.Itens)
                    .ThenInclude(i => i.Acompanhamentos)
                .Where(p => p.Status == EPedidoStatus.EmPreparo)
                .OrderBy(p => p.HorarioRetirada)
                .ThenByDescending(p => p.CriadoEmUtc)
                .ToListAsync();

            return View(pedidos);
        }

        [HttpPost]
        public async Task<IActionResult> IniciarPreparo(Guid id)
        {
            var pedido = await _db.Pedidos.FirstOrDefaultAsync(p => p.Id == id);
            if (pedido == null)
                return NotFound();

            if (pedido.Status == EPedidoStatus.Confirmado)
            {
                pedido.Status = EPedidoStatus.EmPreparo;
                pedido.EmpreparoEmUtc = DateTime.UtcNow;

                // Se estiver reabrindo fluxo, garante que as etapas seguintes fiquem limpas
                pedido.ConcluidoEmUtc = null;
                pedido.EntregueEmUtc = null;

                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> MarcarPronto(Guid id)
        {
            var pedido = await _db.Pedidos.FirstOrDefaultAsync(p => p.Id == id);
            if (pedido == null)
                return NotFound();

            if (pedido.Status == EPedidoStatus.EmPreparo)
            {
                pedido.Status = EPedidoStatus.Pronto;

                // Marca o momento em que a etapa de cozinha foi concluída
                pedido.ConcluidoEmUtc = DateTime.UtcNow;

                // Ainda não foi entregue
                pedido.EntregueEmUtc = null;

                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Concluir(Guid id)
        {
            var pedido = await _db.Pedidos.FirstOrDefaultAsync(p => p.Id == id);
            if (pedido == null)
                return NotFound();

            if (pedido.Status == EPedidoStatus.Pronto)
            {
                pedido.Status = EPedidoStatus.Concluido;
                pedido.EntregueEmUtc = DateTime.UtcNow;

                // Garante que exista uma data de conclusão da cozinha
                pedido.ConcluidoEmUtc ??= DateTime.UtcNow;

                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> VoltarParaConfirmado(Guid id)
        {
            var pedido = await _db.Pedidos.FirstOrDefaultAsync(p => p.Id == id);
            if (pedido == null)
                return NotFound();

            if (pedido.Status == EPedidoStatus.EmPreparo)
            {
                pedido.Status = EPedidoStatus.Confirmado;

                // Remove os marcos posteriores
                pedido.EmpreparoEmUtc = null;
                pedido.ConcluidoEmUtc = null;
                pedido.EntregueEmUtc = null;

                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> VoltarParaEmPreparo(Guid id)
        {
            var pedido = await _db.Pedidos.FirstOrDefaultAsync(p => p.Id == id);
            if (pedido == null)
                return NotFound();

            if (pedido.Status == EPedidoStatus.Pronto)
            {
                pedido.Status = EPedidoStatus.EmPreparo;

                // Continua em preparo, mas deixa de estar concluído/pronto
                pedido.ConcluidoEmUtc = null;
                pedido.EntregueEmUtc = null;

                // Se por algum motivo não existir a data de início de preparo, recria
                pedido.EmpreparoEmUtc ??= DateTime.UtcNow;

                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}