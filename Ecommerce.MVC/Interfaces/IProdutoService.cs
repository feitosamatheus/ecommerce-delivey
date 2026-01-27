using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ecommerce.MVC.Entities;
using Ecommerce.MVC.Models;

namespace Ecommerce.MVC.Interfaces;

public interface IProdutoService
{
    Task<IEnumerable<Produto>> BuscarProdutoAsync();
    Task<FinalizarPedidoModalViewModel> MontarModalAsync(HttpContext http, Guid clienteId, CancellationToken ct);
    Task<Produto> ObterPorIdAsync(Guid produtoId);
}