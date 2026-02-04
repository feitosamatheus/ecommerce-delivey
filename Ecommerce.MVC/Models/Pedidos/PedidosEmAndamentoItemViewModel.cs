using Ecommerce.MVC.Entities;

namespace Ecommerce.MVC.Models.Pedidos;

public class PedidosEmAndamentoItemViewModel
{
    public string ProdutoNome { get; set; } = default!;
    public int Quantidade { get; set; }

    public decimal PrecoBase { get; set; }
    public decimal PrecoAcompanhamentos { get; set; }

    public decimal TotalLinha { get; set; }

    public List<PedidosEmAndamentoItemAcompanhamentoViewModel> Acompanhamentos { get; set; } = new();
}
