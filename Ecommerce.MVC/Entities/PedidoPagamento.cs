using Ecommerce.MVC.Enums;

namespace Ecommerce.MVC.Entities;

public class PedidoPagamento
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PedidoId { get; set; }
    public Pedido Pedido { get; set; }

    public string Gateway { get; set; } = "ASAAS";
    public string TipoPagamento { get; set; } = "PIX";

    public string? GatewayCustomerId { get; set; }
    public string? GatewayPaymentId { get; set; }

    public int Sequencia { get; set; }

    public decimal Valor { get; set; }
    public EStatusPagamento Status { get; set; }
    public ETipoCobrancaPedido TipoCobranca { get; set; }

    public string? PixPayload { get; set; }
    public string? PixEncodedImage { get; set; }
    public DateTime? PixExpirationDate { get; set; }
    public string? InvoiceUrl { get; set; }

    public DateTime CriadoEmUtc { get; set; } = DateTime.UtcNow;
    public DateTime? PagoEmUtc { get; set; } = null;
}
