using Ecommerce.MVC.Enums;

namespace Ecommerce.MVC.Models.Pedidos;

public class PedidoPagamentoViewModel
{
    public string Gateway { get; set; } = default!;
    public string TipoPagamento { get; set; } = default!;
    public string StatusPagamento { get; set; } = default!;
    public EStatusPagamento Status { get; set; } = default!;

    public string? PixCopiaCola { get; set; }
    public string? PixQrCodeUrl { get; set; }
    public string? PixExpiraEm { get; set; }
    public string? PixIdentificador { get; set; }
    public string? PixBeneficiario { get; set; }
    public string? InvoiceUrl { get; set; }
}
