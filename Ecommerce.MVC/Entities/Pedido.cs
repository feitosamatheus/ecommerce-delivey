using Ecommerce.MVC.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ecommerce.MVC.Entities;

public class Pedido
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Codigo { get; set; }

    public Guid ClienteId { get; set; }
    public Cliente Cliente { get; set; }

    public string MetodoEntrega { get; set; } 
    public string Pagamento { get; set; }     
    public string Observacao { get; set; }
    public DateTime HorarioRetirada { get; set; }

    public decimal Subtotal { get; set; }
    public decimal TaxaEntrega { get; set; }
    public decimal Total { get; set; }

    public EPedidoStatus Status { get; set; }


    public DateTime CriadoEmUtc { get; set; } = DateTime.UtcNow;

    public List<PedidoItem> Itens { get; set; } = new();
}