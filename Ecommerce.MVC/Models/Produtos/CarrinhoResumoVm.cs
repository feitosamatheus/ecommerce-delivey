using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ecommerce.MVC.Models.Produtos
{
     public class CarrinhoResumoVm
    {
        public Guid CarrinhoId { get; set; }
        public int ItensCount { get; set; }

        public decimal Subtotal { get; set; }
        public decimal TaxaEntrega { get; set; }
        public decimal Total { get; set; }

        public List<CarrinhoItemResumoVm> Itens { get; set; } = new();
    }
}