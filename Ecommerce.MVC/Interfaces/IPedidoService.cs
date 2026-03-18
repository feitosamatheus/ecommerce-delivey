using Ecommerce.MVC.Models;
using Ecommerce.MVC.Models.Pedidos;
using static Ecommerce.MVC.Controllers.PedidoController;

namespace Ecommerce.MVC.Interfaces;

public interface IPedidoService
{
    Task<ConfirmarPedidoResponse> ConfirmarAsync(HttpContext http, Guid clienteId, ConfirmarPedidoRequest req, CancellationToken ct);
    Task<PedidoModalFinalizarViewModel> ObterDadosFinalizacaoPedidoAsync(HttpContext http, Guid clienteId, CancellationToken ct);
    Task<IReadOnlyList<PedidosEmAndamentoResumoViewModel>> ListarEmAndamentoAsync(Guid clienteId, CancellationToken ct);
    Task<int> ObterQuantidadeEmAndamentoAsync(Guid clienteId, CancellationToken ct);
}
