using Ecommerce.MVC.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ecommerce.MVC.Entities
{
    public class Pedido
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ClienteId { get; set; }
        public Cliente Cliente { get; set; }

        public string MetodoEntrega { get; set; } // "retirar" | "delivery"
        public string Pagamento { get; set; }     // "pix" | "cartao"
        public string Observacao { get; set; }
        public DateTime HorarioRetirada { get; set; }

        public decimal Subtotal { get; set; }
        public decimal TaxaEntrega { get; set; }
        public decimal Total { get; set; }

        public EPedidoStatus Status { get; set; }


        public DateTime CriadoEmUtc { get; set; } = DateTime.UtcNow;

        public List<PedidoItem> Itens { get; set; } = new();
    }
}