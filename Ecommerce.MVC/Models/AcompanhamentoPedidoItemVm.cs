using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ecommerce.MVC.Models
{
    public class AcompanhamentoPedidoItemVm
    {
        public Guid PedidoId { get; set; }
        public DateTime CriadoEmUtc { get; set; }

        public string MetodoEntrega { get; set; } = "";
        public string Pagamento { get; set; } = "";

        public decimal Subtotal { get; set; }
        public decimal TaxaEntrega { get; set; }
        public decimal Total { get; set; }

        public string? EnderecoTexto { get; set; }

        public List<AcompanhamentoPedidoProdutoVm> Itens { get; set; } = new();
    }
}