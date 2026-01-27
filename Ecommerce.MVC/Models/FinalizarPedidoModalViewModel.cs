using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ecommerce.MVC.Models.Produtos;

namespace Ecommerce.MVC.Models
{
    public class FinalizarPedidoModalViewModel
    {
        public Guid ClienteId { get; set; }
        public string ClienteNome { get; set; }
        public string ClienteTelefone { get; set; }

        public List<EnderecoResumoVm> Enderecos { get; set; } = new();
        public Guid? EnderecoSelecionadoId { get; set; } // opcional (principal)

        public CarrinhoResumoVm Carrinho { get; set; } = new();
    }
}