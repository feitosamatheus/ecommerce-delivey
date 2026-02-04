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
    public DateTime HorarioRetirada { get; set; }
    public string Observacao { get; set; }
    public string ClienteNome { get; set; }
    public string ClienteTelefone { get; set; }

    public string StatusPagamento { get; set; }
    public string PixCopiaCola { get; set; }
    public string PixQrCodeUrl { get; set; }
    public string PixExpiraEm { get; set; }
    public string PixIdentificador { get; set; }
    public string PixBeneficiario { get; set; }

    public decimal Subtotal { get; set; }
    public decimal ValorSinal { get; set; }
    public decimal ValorRestanteRetirada { get; set; }
    public List<PedidosEmAndamentoItemViewModel> Itens { get; set; } = new();
}
