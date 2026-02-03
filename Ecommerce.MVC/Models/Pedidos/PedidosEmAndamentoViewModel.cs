using Ecommerce.MVC.Entities;
using Ecommerce.MVC.Enums;

namespace Ecommerce.MVC.Models.Pedidos;

public class PedidosEmAndamentoViewModel
{
    public string Codigo { get; set; } = default!;
    public DateTime CriadoEmUtc { get; set; }
    public EPedidoStatus Status { get; set; }
    public string StatusTexto { get; set; } = default!;
    public string MetodoEntrega { get; set; } = default!;
    public decimal Total { get; set; }
    public int Step { get; set; }
}
