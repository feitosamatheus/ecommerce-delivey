using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;

namespace Ecommerce.MVC.Hubs;

public class PagamentoHub : Hub
{
    public async Task EntrarNoGrupoPedido(string pedidoId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"pedido-{pedidoId}");
    }

    public async Task SairDoGrupoPedido(string pedidoId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"pedido-{pedidoId}");
    }
}
