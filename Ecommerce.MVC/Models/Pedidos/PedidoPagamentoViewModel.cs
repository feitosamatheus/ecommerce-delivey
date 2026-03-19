using Ecommerce.MVC.Enums;

namespace Ecommerce.MVC.Models.Pedidos;

public class PedidoPagamentoViewModel
{
    public string Gateway { get; set; } = default!;
    public string TipoPagamento { get; set; } = default!;
    public string StatusPagamento { get; set; } = default!;
    public EStatusPagamento Status { get; set; } = default!;

    public decimal Valor { get; set; }

    public ETipoCobrancaPedido TipoCobranca { get; set; }
    public string TipoCobrancaTexto { get; set; }

    public int Sequencia { get; set; }

    public string? PixCopiaCola { get; set; }
    public string? PixQrCodeUrl { get; set; }
    public string? PixExpiraEm { get; set; }

    public string? GatewayPaymentId { get; set; }
    public string? PixBeneficiario { get; set; }
    public string? InvoiceUrl { get; set; }

    public DateTime CriadoEmUtc { get; set; }
    public DateTime? PagoEmUtc { get; set; }
}
