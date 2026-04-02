using Ecommerce.MVC.Config;
using Ecommerce.MVC.Entities;
using Ecommerce.MVC.Enums;
using Ecommerce.MVC.Models.Admin.Categorias;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.MVC.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class CategoriasController : Controller
{
    private readonly DatabaseContext _db;

    public CategoriasController(DatabaseContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? busca)
    {
        ViewData["page"] = "Produto";

        var query = _db.Categorias
            .AsNoTracking()
            .Include(x => x.Produtos)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(busca))
        {
            busca = busca.Trim();

            query = query.Where(x => EF.Functions.ILike(x.Nome, $"%{busca}%"));
        }

        var model = new CategoriasIndexViewModel
        {
            Busca = busca,
            Categorias = await query
                .OrderBy(x => x.Nome)
                .ToListAsync()
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewData["page"] = "Produto";
        var model = new CategoriaFormViewModel();
        PopularTiposExibicao(model);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoriaFormViewModel model)
    {
        ValidarNomeDuplicado(model);

        if (!ModelState.IsValid)
        {
            PopularTiposExibicao(model);
            return View(model);
        }

        var categoria = new Categoria
        {
            Nome = model.Nome.Trim(),
            TipoExibicao = model.TipoExibicao
        };

        _db.Categorias.Add(categoria);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Categoria cadastrada com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        ViewData["page"] = "Produto";
        
        var categoria = await _db.Categorias
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (categoria == null)
            return NotFound();

        var model = new CategoriaFormViewModel
        {
            Id = categoria.Id,
            Nome = categoria.Nome,
            TipoExibicao = categoria.TipoExibicao
        };

        PopularTiposExibicao(model);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, CategoriaFormViewModel model)
    {
        if (id != model.Id)
            return BadRequest();

        var categoria = await _db.Categorias.FirstOrDefaultAsync(x => x.Id == id);

        if (categoria == null)
            return NotFound();

        ValidarNomeDuplicado(model, id);

        if (!ModelState.IsValid)
        {
            PopularTiposExibicao(model);
            return View(model);
        }

        categoria.Nome = model.Nome.Trim();
        categoria.TipoExibicao = model.TipoExibicao;

        await _db.SaveChangesAsync();

        TempData["Success"] = "Categoria atualizada com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var categoria = await _db.Categorias
            .Include(x => x.Produtos)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (categoria == null)
            return NotFound();

        if (categoria.Produtos.Any())
        {
            TempData["Error"] = "Não é possível excluir uma categoria vinculada a produtos.";
            return RedirectToAction(nameof(Index));
        }

        _db.Categorias.Remove(categoria);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Categoria excluída com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    private void PopularTiposExibicao(CategoriaFormViewModel model)
    {
        model.TiposExibicao = Enum.GetValues(typeof(ETipoExibicao))
            .Cast<ETipoExibicao>()
            .Select(x => new SelectListItem
            {
                Value = x.ToString(),
                Text = x.ToString(),
                Selected = x == model.TipoExibicao
            })
            .ToList();
    }

    private void ValidarNomeDuplicado(CategoriaFormViewModel model, Guid? idAtual = null)
    {
        var nome = model.Nome?.Trim();

        if (string.IsNullOrWhiteSpace(nome))
            return;

        var existe = _db.Categorias.Any(x =>
            x.Nome.ToLower() == nome.ToLower() &&
            (!idAtual.HasValue || x.Id != idAtual.Value));

        if (existe)
        {
            ModelState.AddModelError(nameof(model.Nome), "Já existe uma categoria com esse nome.");
        }
    }
}