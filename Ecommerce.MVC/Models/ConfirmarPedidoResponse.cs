namespace Ecommerce.MVC.Models;

public class ConfirmarPedidoResponse
{
    public Guid PedidoId { get; set; }
    public bool Ok { get; set; }

    public decimal Valor { get; set; }
    public string Codigo { get; set; } = "";
    public string PixChave { get; set; } = "";
    public string PixCopiaCola { get; set; } = "";
    public string QrCodeBase64 { get; set; } = ""; // PNG base64 (sem data:image/png;base64,)
    public DateTime ExpiraEmUtc { get; set; }
}
