using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ecommerce.MVC.Models.Produtos
{
    public class AdicionarProdutoCarrinhoViewModel
    {
        public Guid ProdutoId { get; set; }
        public int Quantidade { get; set; } = 1;
        public string? Observacao { get; set; }
        public List<CarrinhoAddAcompanhamentoViewModel> Acompanhamentos { get; set; } = new();
    }
}