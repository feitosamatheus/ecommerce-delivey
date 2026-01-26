using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ecommerce.MVC.Config;
using Ecommerce.MVC.Entities;
using Ecommerce.MVC.Interfaces;
using Ecommerce.MVC.Models.Produtos;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.MVC.Services;

public class ProdutoService : IProdutoService
{
    private readonly DatabaseContext _context;

    public ProdutoService(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Produto>> BuscarProdutoAsync()
    {
        return await _context.Produtos.AsNoTracking().Include(p => p.Categoria).OrderBy(p => p.Nome).ToListAsync();
    }

    public async Task<Produto> ObterPorIdAsync(Guid produtoId)
    {
        return await _context.Produtos.AsNoTracking()
        .Include(p => p.Categoria)
        .Include(p => p.AcompanhamentoCategorias)
            .ThenInclude(pc => pc.Categoria)
                .ThenInclude(c => c.Acompanhamentos)
        .FirstOrDefaultAsync(p => p.Id == produtoId);
    }
}