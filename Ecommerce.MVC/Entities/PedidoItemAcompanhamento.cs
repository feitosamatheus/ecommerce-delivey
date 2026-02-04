using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ecommerce.MVC.Entities;

public class PedidoItemAcompanhamento
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PedidoItemId { get; set; }
    public PedidoItem PedidoItem { get; set; }

    public Guid AcompanhamentoId { get; set; }
    public Guid CategoriaId { get; set; }
    public string NomeSnapshot { get; set; }
    public decimal PrecoSnapshot { get; set; }
}