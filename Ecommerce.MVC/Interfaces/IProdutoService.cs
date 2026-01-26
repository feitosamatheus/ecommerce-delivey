using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ecommerce.MVC.Entities;

namespace Ecommerce.MVC.Interfaces;

public interface IProdutoService
{
    Task<IEnumerable<Produto>> BuscarProdutoAsync();
    Task<Produto> ObterPorIdAsync(Guid produtoId);
}