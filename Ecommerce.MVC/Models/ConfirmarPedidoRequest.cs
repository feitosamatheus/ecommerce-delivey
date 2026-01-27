using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ecommerce.MVC.Models
{
    public class ConfirmarPedidoRequest
    {
        public string MetodoEntrega { get; set; } // "retirar" | "delivery"
        public Guid? EnderecoId { get; set; }     // obrigatório se delivery
        public string Pagamento { get; set; }     // "pix" | "cartao"
        public string Observacao { get; set; }
    }
}