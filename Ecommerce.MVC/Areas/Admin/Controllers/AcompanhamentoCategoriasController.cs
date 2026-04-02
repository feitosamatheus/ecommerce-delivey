using Ecommerce.MVC.Config;
using Ecommerce.MVC.Entities;
using Ecommerce.MVC.Models.Admin.AcompanhamentoCategorias;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.MVC.Areas.Admin.Controllers;

[Area("Admin")]
public class AcompanhamentoCategoriasController : Controller
{
    private readonly DatabaseContext _db;

    public AcompanhamentoCategoriasController(DatabaseContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? busca)
    {
        ViewData["page"] = "Acompanhamento";

        var query = _db.AcompanhamentoCategorias
            .AsNoTracking()
            .Include(x => x.Acompanhamentos)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(busca))
        {
            busca = busca.Trim();

            query = query.Where(x =>
                x.Nome.Contains(busca) ||
                (x.Descricao != null && x.Descricao.Contains(busca)));
        }

        var model = new AcompanhamentoCategoriasIndexViewModel
        {
            Busca = busca,
            Itens = await query
                .OrderBy(x => x.Nome)
                .Select(x => new AcompanhamentoCategoriaListItemViewModel
                {
                    Id = x.Id,
                    Nome = x.Nome,
                    Descricao = x.Descricao,
                    QuantidadeAcompanhamentos = x.Acompanhamentos.Count
                })
                .ToListAsync()
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewData["page"] = "Acompanhamento";
        ViewData["Title"] = "Nova Categoria de Acompanhamento";
        ViewData["Description"] = "Cadastre e organize grupos de acompanhamentos.";
        return View(new AcompanhamentoCategoriaFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AcompanhamentoCategoriaFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Nova Categoria de Acompanhamento";
            ViewData["Description"] = "Cadastre e organize grupos de acompanhamentos.";
            return View(model);
        }

        var entity = new AcompanhamentoCategoria
        {
            Id = Guid.NewGuid(),
            Nome = model.Nome.Trim(),
            Descricao = string.IsNullOrWhiteSpace(model.Descricao) ? null : model.Descricao.Trim()
        };

        _db.AcompanhamentoCategorias.Add(entity);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Categoria de acompanhamento cadastrada com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        ViewData["page"] = "Acompanhamento";
        
        var entity = await _db.AcompanhamentoCategorias.FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return NotFound();

        ViewData["Title"] = "Editar Categoria de Acompanhamento";
        ViewData["Description"] = "Atualize os dados da categoria.";
        return View(new AcompanhamentoCategoriaFormViewModel
        {
            Id = entity.Id,
            Nome = entity.Nome,
            Descricao = entity.Descricao
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AcompanhamentoCategoriaFormViewModel model)
    {
        if (!model.Id.HasValue)
            return NotFound();

        var entity = await _db.AcompanhamentoCategorias.FirstOrDefaultAsync(x => x.Id == model.Id.Value);

        if (entity == null)
            return NotFound();

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Editar Categoria de Acompanhamento";
            ViewData["Description"] = "Atualize os dados da categoria.";
            return View(model);
        }

        entity.Nome = model.Nome.Trim();
        entity.Descricao = string.IsNullOrWhiteSpace(model.Descricao) ? null : model.Descricao.Trim();

        await _db.SaveChangesAsync();

        TempData["Success"] = "Categoria de acompanhamento atualizada com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _db.AcompanhamentoCategorias
            .Include(x => x.Acompanhamentos)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return NotFound();

        if (entity.Acompanhamentos.Any())
        {
            TempData["Error"] = "Não é possível excluir a categoria porque existem acompanhamentos vinculados.";
            return RedirectToAction(nameof(Index));
        }

        _db.AcompanhamentoCategorias.Remove(entity);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Categoria de acompanhamento removida com sucesso.";
        return RedirectToAction(nameof(Index));
    }
}