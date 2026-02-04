using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ecommerce.MVC.Entities;

public class PedidoItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PedidoId { get; set; }
    public Pedido Pedido { get; set; }

    public Guid ProdutoId { get; set; }
    public string ProdutoNomeSnapshot { get; set; }

    public decimal PrecoBaseSnapshot { get; set; }
    public int Quantidade { get; set; }

    public decimal PrecoAcompanhamentosSnapshot { get; set; }
    public decimal TotalLinha { get; set; }

    public List<PedidoItemAcompanhamento> Acompanhamentos { get; set; } = new();
}