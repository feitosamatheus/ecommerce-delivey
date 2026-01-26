using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ecommerce.MVC.Models.Produtos
{
    public class CarrinhoAddAcompanhamentoViewModel
    {
        public Guid AcompanhamentoId { get; set; }
        public Guid CategoriaId { get; set; }
    }
}