using Ecommerce.MVC.Config;
using Ecommerce.MVC.Entities;
using Ecommerce.MVC.Helpers;
using Ecommerce.MVC.Interfaces;
using Ecommerce.MVC.Migrations;
using Ecommerce.MVC.Models;
using Ecommerce.MVC.Models.Pedidos;
using Ecommerce.MVC.Models.Produtos;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ecommerce.MVC.Services;

public class ProdutoService : IProdutoService
{
    private readonly DatabaseContext _db;

    public ProdutoService(DatabaseContext context)
    {
        _db = context;
    }

    public async Task<IEnumerable<Produto>> BuscarProdutoAsync()
    {
        return await _db.Produtos.AsNoTracking().Include(p => p.Categoria).OrderBy(p => p.Nome).ToListAsync();
    }

    public async Task<Produto> ObterPorIdAsync(Guid produtoId)
    {
        return await _db.Produtos.AsNoTracking()
        .Include(p => p.Categoria)
        .Include(p => p.AcompanhamentoCategorias)
            .ThenInclude(pc => pc.Categoria)
                .ThenInclude(c => c.Acompanhamentos)
        .FirstOrDefaultAsync(p => p.Id == produtoId);
    }

}