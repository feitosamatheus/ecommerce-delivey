using Ecommerce.MVC.Models;
using Ecommerce.MVC.Models.Pedidos;

namespace Ecommerce.MVC.Interfaces;

public interface IPedidoService
{
    Task<ConfirmarPedidoResponse> ConfirmarAsync(HttpContext http, Guid clienteId, ConfirmarPedidoRequest req, CancellationToken ct);
    Task<PedidoModalFinalizarViewModel> ObterDadosFinalizacaoPedidoAsync(HttpContext http, Guid clienteId, CancellationToken ct);
    Task<IReadOnlyList<PedidosEmAndamentoViewModel>> ListarEmAndamentoAsync(Guid clienteId, CancellationToken ct);
    Task<int> ObterQuantidadeEmAndamentoAsync(Guid clienteId, CancellationToken ct);
}
