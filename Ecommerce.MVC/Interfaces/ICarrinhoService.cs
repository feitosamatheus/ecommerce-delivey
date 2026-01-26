using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ecommerce.MVC.Entities;
using Ecommerce.MVC.Models.Produtos;

namespace Ecommerce.MVC.Interfaces
{
    public interface ICarrinhoService
    {
        Task<Carrinho> ObterOuCriarCarrinhoAsync(HttpContext http, CancellationToken ct = default);
        Task<Carrinho> AdicionarAsync(HttpContext http, AdicionarProdutoCarrinhoViewModel req, CancellationToken ct = default);
    }
}