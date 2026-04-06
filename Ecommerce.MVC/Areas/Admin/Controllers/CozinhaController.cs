using Ecommerce.MVC.Config;
using Ecommerce.MVC.Entities;
using Ecommerce.MVC.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.MVC.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "administrador,cozinhaN1,cozinhaN2,balcaoN1,balcaoN2,gerente")]
    public class CozinhaController : Controller
    {
        private readonly DatabaseContext _db;

        public CozinhaController(DatabaseContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["page"] = "Cozinha";
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

        public async Task<IActionResult> EmPreparo(string nomeCliente, string numeroPedido)
        {
            ViewData["page"] = "Cozinha";
            ViewData["Title"] = "Em Preparo";
            ViewData["Description"] = "Pedidos que estão atualmente em produção na cozinha.";

            // Buscar pedidos em preparo
            var pedidosQuery = _db.Pedidos
                .AsNoTracking()
                .Include(p => p.Cliente)
                .Include(p => p.Itens)
                    .ThenInclude(i => i.Acompanhamentos)
                .Where(p => p.Status == EPedidoStatus.EmPreparo);

            // Filtro adicional para Nome do Cliente
            if (!string.IsNullOrEmpty(nomeCliente))
            {
                pedidosQuery = pedidosQuery.Where(p => p.Cliente.Nome.Contains(nomeCliente));
            }

            // Filtro adicional para Número do Pedido
            if (!string.IsNullOrEmpty(numeroPedido))
            {
                pedidosQuery = pedidosQuery.Where(p => p.Codigo.Contains(numeroPedido));
            }

            var pedidos = await pedidosQuery.OrderBy(p => p.HorarioRetirada)
                                            .ThenByDescending(p => p.CriadoEmUtc)
                                            .ToListAsync();

            var model = new PedidosViewModel
            {
                PedidosEmPreparo = pedidos,
                NomeCliente = nomeCliente,
                NumeroPedido = numeroPedido
            };

            return View(model);
        }

        public async Task<IActionResult> Prontos(DateTime? dataInicio, DateTime? dataFim, string nomeCliente, string numeroPedido)
        {
            ViewData["page"] = "Cozinha";
            ViewData["Title"] = "Pedidos Prontos";
            ViewData["Description"] = "Pedidos prontos para entrega ou retirada.";

            dataInicio ??= DateTime.Now.Date;
            dataFim ??= DateTime.Now.Date;

            TimeZoneInfo timeZoneBrasil;

            try
            {
                timeZoneBrasil = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
            }
            catch
            {
                timeZoneBrasil = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
            }

            var inicioLocal = DateTime.SpecifyKind(dataInicio.Value.Date, DateTimeKind.Unspecified);
            var fimLocal = DateTime.SpecifyKind(dataFim.Value.Date.AddDays(1), DateTimeKind.Unspecified);

            var inicioUtc = TimeZoneInfo.ConvertTimeToUtc(inicioLocal, timeZoneBrasil);
            var fimUtc = TimeZoneInfo.ConvertTimeToUtc(fimLocal, timeZoneBrasil);

            var pedidosProntos = await _db.Pedidos
                .AsNoTracking()
                .Include(p => p.Cliente)
                .Include(p => p.Itens)
                    .ThenInclude(i => i.Acompanhamentos)
                .Where(p => p.Status == EPedidoStatus.Pronto)
                .Where(p => p.ProntoEmUtc >= inicioUtc && p.ProntoEmUtc < fimUtc)
                .Where(p => string.IsNullOrEmpty(nomeCliente) || p.Cliente.Nome.Contains(nomeCliente))
                .Where(p => string.IsNullOrEmpty(numeroPedido) || p.Codigo.Contains(numeroPedido))
                .OrderByDescending(p => p.ProntoEmUtc)
                .ToListAsync();

            var model = new PedidosViewModel
            {
                PedidosProntos = pedidosProntos,
                DataInicio = dataInicio,
                DataFim = dataFim,
                NomeCliente = nomeCliente,
                NumeroPedido = numeroPedido
            };

            return View(model);
        }

        public async Task<IActionResult> Concluidos(DateTime? dataInicio, DateTime? dataFim, string numeroPedido, string nomeCliente)
        {
            ViewData["page"] = "Cozinha";
            ViewData["Title"] = "Pedidos Concluídos";
            ViewData["Description"] = "Pedidos já finalizados e entregues.";

            // Valor padrão: hoje (UTC)
            dataInicio ??= DateTime.Now.Date;
            dataFim ??= DateTime.Now.Date;

            // Converter para intervalo UTC completo (00:00 até 23:59:59)
            var inicioUtc = DateTime.SpecifyKind(dataInicio.Value.Date, DateTimeKind.Utc);
            var fimUtc = DateTime.SpecifyKind(dataFim.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

            var pedidosConcluidos = _db.Pedidos
                .AsNoTracking()
                .Include(p => p.Cliente)
                .Include(p => p.Itens)
                    .ThenInclude(i => i.Acompanhamentos)
                .Where(p =>
                    p.Status == EPedidoStatus.Concluido &&
                    p.ConcluidoEmUtc.HasValue &&
                    p.ConcluidoEmUtc.Value >= inicioUtc &&
                    p.ConcluidoEmUtc.Value <= fimUtc
                );

            // Filtro adicional para Código do Pedido
            if (!string.IsNullOrEmpty(numeroPedido))
            {
                pedidosConcluidos = pedidosConcluidos.Where(p => p.Codigo.Contains(numeroPedido));
            }

            // Filtro adicional para Nome do Cliente
            if (!string.IsNullOrEmpty(nomeCliente))
            {
                pedidosConcluidos = pedidosConcluidos.Where(p => p.Cliente.Nome.Contains(nomeCliente));
            }

            // Ordena os pedidos por data de conclusão
            pedidosConcluidos = pedidosConcluidos.OrderByDescending(p => p.ConcluidoEmUtc);

            // Executa a consulta e retorna os pedidos
            var pedidosList = await pedidosConcluidos.ToListAsync();

            // Criando o modelo para passar para a view
            var model = new PedidosViewModel
            {
                PedidosConcluidos = pedidosList,
                DataInicio = dataInicio,
                DataFim = dataFim,
                NumeroPedido = numeroPedido,
                NomeCliente = nomeCliente
            };

            return View(model);
        }


        public class PedidosViewModel
        {
            public List<Ecommerce.MVC.Entities.Pedido> PedidosProntos { get; set; }
            public List<Ecommerce.MVC.Entities.Pedido> PedidosConcluidos { get; set; }
            public List<Ecommerce.MVC.Entities.Pedido> PedidosEmPreparo { get; set; }

            public DateTime? DataInicio { get; set; }
            public DateTime? DataFim { get; set; }

            // Adicionando as novas propriedades para o filtro
            public string NomeCliente { get; set; }
            public string NumeroPedido { get; set; }
        }

        public async Task<IActionResult> Detalhes(Guid id)
        {
            // Buscar o pedido pelos detalhes
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

            // Retornar o conteúdo parcial (parcial da view com detalhes do pedido)
            return PartialView("_DetalhesPedidoCozinha", pedido);
        }
        
        public async Task<IActionResult> ConcluirPedido(Guid id)
        {
            var pedido = await _db.Pedidos.FirstOrDefaultAsync(p => p.Id == id);
            if (pedido == null)
                return NotFound();

            pedido.Status = EPedidoStatus.Concluido; // Mudar status para Concluído
            pedido.ConcluidoEmUtc = DateTime.UtcNow; // Mudar status para Concluído
            await _db.SaveChangesAsync();

            return Json(new { success = true });
        }

        public async Task<IActionResult> CancelarPedido(Guid id)
        {
            var pedido = await _db.Pedidos.FirstOrDefaultAsync(p => p.Id == id);
            if (pedido == null)
                return NotFound();

            pedido.Status = EPedidoStatus.Cancelado; // Mudar status para Cancelado
            await _db.SaveChangesAsync();

            return Json(new { success = true });
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
                pedido.ProntoEmUtc = null;

                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> MarcarPronto(Guid id, string? returnUrl)
        {
            var pedido = await _db.Pedidos.FirstOrDefaultAsync(p => p.Id == id);
            if (pedido == null)
                return NotFound();

            if (pedido.Status == EPedidoStatus.EmPreparo)
            {
                pedido.Status = EPedidoStatus.Pronto;

                // Marca o momento em que a etapa de cozinha foi concluída
                pedido.ProntoEmUtc = DateTime.UtcNow;

                // Ainda não foi entregue
                pedido.EntregueEmUtc = null;
                pedido.ConcluidoEmUtc = null;

                await _db.SaveChangesAsync();
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

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
                pedido.ConcluidoEmUtc = DateTime.UtcNow;
                pedido.EntregueEmUtc = DateTime.UtcNow;

                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> VoltarParaConfirmado(Guid id, string? returnUrl)
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
                pedido.ProntoEmUtc = null;

                await _db.SaveChangesAsync();
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> VoltarParaEmPreparo(Guid id, string? returnUrl)
        {
            var pedido = await _db.Pedidos.FirstOrDefaultAsync(p => p.Id == id);
            if (pedido == null)
                return NotFound();

            if (pedido.Status == EPedidoStatus.Pronto)
            {
                pedido.Status = EPedidoStatus.EmPreparo;

                // Continua em preparo, mas deixa de estar concluído/pronto
                pedido.ConcluidoEmUtc = null;
                pedido.ProntoEmUtc = null;
                pedido.EntregueEmUtc = null;

                // Se por algum motivo não existir a data de início de preparo, recria
                pedido.EmpreparoEmUtc ??= DateTime.UtcNow;

                await _db.SaveChangesAsync();
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(Index));
        }
    }
}