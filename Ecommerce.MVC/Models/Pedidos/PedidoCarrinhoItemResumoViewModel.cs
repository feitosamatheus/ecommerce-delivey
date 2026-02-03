using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ecommerce.MVC.Models.Pedidos;

public class PedidoCarrinhoItemResumoViewModel
{
    public Guid ProdutoId { get; set; }
    public string ProdutoNome { get; set; }
    public int Quantidade { get; set; }

    public decimal PrecoBase { get; set; }
    public decimal PrecoAcompanhamentos { get; set; }
    public decimal TotalLinha { get; set; }

    public List<PedidoAcompanhamentoDetalheViewModel> Acompanhamentos { get; set; } = new();
}