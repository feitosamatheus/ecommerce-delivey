namespace Ecommerce.MVC.Models.Pedidos;

public class PedidoRetiradaHorarioViewModel
{
    public DateTime DataHora { get; set; }

    public string Texto => DataHora.ToString("dd/MM/yyyy 'às' HH:mm");
}
