using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ecommerce.MVC.Models.Pedidos;

 public class PedidoCarrinhoResumoViewModel
{
    public Guid CarrinhoId { get; set; }
    public int ItensCount { get; set; }

    public decimal Subtotal { get; set; }
    public decimal TaxaEntrega { get; set; }
    public decimal ValorSinal { get; set; }      
    public decimal ValorRestanteRetirada { get; set; }
    public decimal Total { get; set; }

    public List<PedidoCarrinhoItemResumoViewModel> Itens { get; set; } = new();
}