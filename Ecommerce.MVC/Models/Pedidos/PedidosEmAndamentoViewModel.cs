using Ecommerce.MVC.Entities;
using Ecommerce.MVC.Enums;

namespace Ecommerce.MVC.Models.Pedidos;

public class PedidosEmAndamentoViewModel
{
    public Guid Id { get; set; }
    public string Codigo { get; set; } = default!;
    public DateTime CriadoEmUtc { get; set; }
    public EPedidoStatus Status { get; set; }
    public string StatusTexto { get; set; } = default!;
    public string MetodoEntrega { get; set; } = default!;
    public decimal Total { get; set; }
    public int Step { get; set; }
    public DateTime HorarioRetirada { get; set; }
    public DateTime? EmpreparoEmUtc { get; set; }
    public DateTime? ProntoEmUtc { get; set; }
    public DateTime? ConcluidoEmUtc { get; set; }
    public string Observacao { get; set; }
    public string ClienteNome { get; set; }
    public string ClienteTelefone { get; set; }

    public decimal Subtotal { get; set; }
    public decimal ValorSinal { get; set; }
    public decimal ValorRestanteRetirada { get; set; }

    public List<PedidoPagamentoViewModel> Pagamentos { get; set; } = new();

    public List<PedidosEmAndamentoItemViewModel> Itens { get; set; } = new();
}
