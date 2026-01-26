using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ecommerce.MVC.Config;
using Ecommerce.MVC.Entities;
using Ecommerce.MVC.Interfaces;
using Ecommerce.MVC.Models.Categorias;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.MVC.Services;

public class CategoriaService : ICategoriaService
{
    private readonly DatabaseContext _context;

    public CategoriaService(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Categoria>> BuscarCategoriasAsync()
    {
        return await _context.Categorias.AsNoTracking().Where(c => c.Produtos.Any())
            .Include(c => c.Produtos)
            .OrderBy(c => c.Nome)
            .ToListAsync();
    }
}