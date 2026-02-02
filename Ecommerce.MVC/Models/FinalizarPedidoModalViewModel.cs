using Ecommerce.MVC.Models.Pedidos;
using Ecommerce.MVC.Models.Produtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ecommerce.MVC.Models
{
    public class FinalizarPedidoModalViewModel
    {
        public Guid ClienteId { get; set; }
        public string ClienteNome { get; set; }
        public string ClienteTelefone { get; set; }

        public CarrinhoResumoVm Carrinho { get; set; } = new();

        public List<RetiradaHorarioVm> HorariosRetirada { get; set; } = new();
    }
}