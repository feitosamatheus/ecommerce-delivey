using System;
using System.Linq;
using System.Threading.Tasks;
using Ecommerce.MVC.Config;
using Ecommerce.MVC.Entities;
using Ecommerce.MVC.Models.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.MVC.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class ProdutosController : Controller
{
    private readonly DatabaseContext _db;

    public ProdutosController(DatabaseContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(string busca, Guid? categoriaId, int pagina = 1, int tamanhoPagina = 5)
    {
        var query = _db.Produtos
            .AsNoTracking()
            .Include(p => p.Categoria)
            .AsQueryable();

        // Aplicar Filtros
        if (!string.IsNullOrWhiteSpace(busca))
        {
            query = query.Where(p => p.Nome.Contains(busca) || p.Descricao.Contains(busca));
        }

        if (categoriaId.HasValue)
        {
            query = query.Where(p => p.CategoriaId == categoriaId.Value);
        }

        // Ordenação Padrão (Pode ser expandida depois)
        query = query.OrderBy(p => p.Nome);

        // Paginação
        var totalItens = await query.CountAsync();
        var produtos = await query
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync();

        // Preparar ViewModel
        var model = new ProdutosIndexViewModel
        {
            Produtos = produtos,
            Categorias = await _db.Categorias
                .AsNoTracking()
                .OrderBy(c => c.Nome)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Nome,
                    Selected = c.Id == categoriaId
                })
                .ToListAsync(),
            PaginaAtual = pagina,
            TotalPaginas = (int)Math.Ceiling(totalItens / (double)tamanhoPagina),
            TotalItens = totalItens,
            TamanhoPagina = tamanhoPagina,
            Busca = busca,
            CategoriaId = categoriaId
        };

        // Lógica AJAX: Se for uma requisição do script, retorna apenas a tabela
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return PartialView("_ProdutosLista", model);
        }

        return View(model);
    }

    // O restante do seu código (Create, Edit, Delete) permanece igual, 
    // apenas certifique-se de que os métodos auxiliares continuem funcionando.

    public async Task<IActionResult> Create()
    {
        await PopularCategoriasAsync();
        return View(new AdminProdutoFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminProdutoFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopularCategoriasAsync();
            return View(model);
        }

        var produto = new Produto
        {
            Nome = model.Nome.Trim(),
            Descricao = string.IsNullOrWhiteSpace(model.Descricao) ? null : model.Descricao.Trim(),
            Preco = model.Preco,
            ImagemUrl = string.IsNullOrWhiteSpace(model.ImagemUrl) ? null : model.ImagemUrl.Trim(),
            TempoPreparoMinutos = model.TempoPreparoMinutos,
            CategoriaId = model.CategoriaId
        };

        _db.Produtos.Add(produto);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var produto = await _db.Produtos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (produto == null) return NotFound();

        var vm = new AdminProdutoFormViewModel
        {
            Id = produto.Id,
            Nome = produto.Nome,
            Descricao = produto.Descricao,
            Preco = produto.Preco,
            ImagemUrl = produto.ImagemUrl,
            TempoPreparoMinutos = produto.TempoPreparoMinutos,
            CategoriaId = produto.CategoriaId
        };

        await PopularCategoriasAsync();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, AdminProdutoFormViewModel model)
    {
        if (id != model.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            await PopularCategoriasAsync();
            return View(model);
        }

        var produto = await _db.Produtos.FirstOrDefaultAsync(p => p.Id == id);
        if (produto == null) return NotFound();

        produto.Nome = model.Nome.Trim();
        produto.Descricao = string.IsNullOrWhiteSpace(model.Descricao) ? null : model.Descricao.Trim();
        produto.Preco = model.Preco;
        produto.ImagemUrl = string.IsNullOrWhiteSpace(model.ImagemUrl) ? null : model.ImagemUrl.Trim();
        produto.TempoPreparoMinutos = model.TempoPreparoMinutos;
        produto.CategoriaId = model.CategoriaId;

        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var produto = await _db.Produtos.FirstOrDefaultAsync(p => p.Id == id);

        if (produto == null) return NotFound();

        // Deleção Lógica
        produto.Ativo = false;

        _db.Produtos.Update(produto);
        await _db.SaveChangesAsync();

        // Se estiver usando AJAX, você pode retornar um Json
        // return Json(new { success = true }); 

        return RedirectToAction(nameof(Index));
    }

    private async Task PopularCategoriasAsync()
    {
        var categorias = await _db.Categorias
            .AsNoTracking()
            .OrderBy(c => c.Nome)
            .ToListAsync();

        ViewBag.Categorias = categorias;
    }
}