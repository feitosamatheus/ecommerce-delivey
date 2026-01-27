using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ecommerce.MVC.Models.Produtos
{
    public class CarrinhoItemResumoVm
    {
        public Guid ProdutoId { get; set; }
        public string ProdutoNome { get; set; }
        public int Quantidade { get; set; }

        public decimal PrecoBase { get; set; }
        public decimal PrecoAcompanhamentos { get; set; }
        public decimal TotalLinha { get; set; }

        public List<string> Acompanhamentos { get; set; } = new();
    }
}