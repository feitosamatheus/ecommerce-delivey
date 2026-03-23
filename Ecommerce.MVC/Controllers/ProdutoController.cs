using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Ecommerce.MVC.Config;
using Ecommerce.MVC.Interfaces;
using Ecommerce.MVC.Models.Produtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.MVC.Controllers;
public class ProdutoController : Controller
{
    private readonly DatabaseContext _db;

    public ProdutoController(DatabaseContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> BuscarModalAdicionarProduto(Guid id)
    {
        var produto = await _db.Produtos
            .AsNoTracking()
            .Include(p => p.AcompanhamentoCategorias)
                .ThenInclude(pc => pc.Categoria)
            .Include(p => p.AcompanhamentoCategorias)
                .ThenInclude(pc => pc.ProdutoAcompanhamentos)
                    .ThenInclude(pa => pa.Acompanhamento)
            .FirstOrDefaultAsync(p => p.Id == id && p.Ativo);

        if (produto == null)
            return NotFound();

        return PartialView("_ModalAddProduto", produto);
    }
}