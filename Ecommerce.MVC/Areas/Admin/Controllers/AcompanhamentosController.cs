using Ecommerce.MVC.Config;
using Ecommerce.MVC.Entities;
using Ecommerce.MVC.Models.Admin.Acompanhamentos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Ecommerce.MVC.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "administrador,gerente")]

public class AcompanhamentosController : Controller
{
    private readonly DatabaseContext _db;

    public AcompanhamentosController(DatabaseContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? busca, Guid? categoriaId)
    {
        ViewData["page"] = "Acompanhamento";

        var query = _db.Acompanhamentos
            .AsNoTracking()
            .Include(x => x.Categoria)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(busca))
        {
            busca = busca.Trim();

            query = query.Where(x =>
                EF.Functions.ILike(x.Nome, $"%{busca}%") ||
                (x.Descricao != null && EF.Functions.ILike(x.Descricao, $"%{busca}%")));
        }

        if (categoriaId.HasValue)
        {
            query = query.Where(x => x.AcompanhamentoCategoriaId == categoriaId.Value);
        }

        var model = new AcompanhamentosIndexViewModel
        {
            Busca = busca,
            CategoriaId = categoriaId,
            Categorias = await _db.AcompanhamentoCategorias
                .AsNoTracking()
                .OrderBy(x => x.Nome)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.Nome
                })
                .ToListAsync(),
            Itens = await query
                .OrderBy(x => x.Categoria.Nome)
                .ThenBy(x => x.Ordem)
                .ThenBy(x => x.Nome)
                .Select(x => new AcompanhamentoListItemViewModel
                {
                    Id = x.Id,
                    Nome = x.Nome,
                    Descricao = x.Descricao,
                    Preco = x.Preco,
                    Ativo = x.Ativo,
                    Ordem = x.Ordem,
                    CategoriaNome = x.Categoria.Nome
                })
                .ToListAsync()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new AcompanhamentoFormViewModel();
        await PopularCategoriasAsync(model);

        ViewData["page"] = "Acompanhamento";
        ViewData["Title"] = "Novo Acompanhamento";
        ViewData["Description"] = "Cadastre os itens adicionais dos produtos.";
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AcompanhamentoFormViewModel model)
    {
        await PopularCategoriasAsync(model);

        decimal preco = 0;

        if (!string.IsNullOrEmpty(model.Preco))
        {
            // Substitui a vírgula por ponto e faz a conversão para decimal
            preco = decimal.Parse(model.Preco.Replace(',', '.'), CultureInfo.InvariantCulture);
        }

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Novo Acompanhamento";
            ViewData["Description"] = "Cadastre os itens adicionais dos produtos.";
            return View(model);
        }

        var entity = new Acompanhamento
        {
            Id = Guid.NewGuid(),
            Nome = model.Nome.Trim(),
            Descricao = string.IsNullOrWhiteSpace(model.Descricao) ? null : model.Descricao.Trim(),
            Preco = preco,
            Ativo = model.Ativo,
            Ordem = model.Ordem,
            AcompanhamentoCategoriaId = model.AcompanhamentoCategoriaId!.Value
        };

        _db.Acompanhamentos.Add(entity);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Acompanhamento cadastrado com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        ViewData["page"] = "Acompanhamento";
        
        var entity = await _db.Acompanhamentos.FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return NotFound();

        var precoFormatado = entity.Preco.ToString("N2", new CultureInfo("pt-BR")).Replace('.', ',');


        var model = new AcompanhamentoFormViewModel
        {
            Id = entity.Id,
            Nome = entity.Nome,
            Descricao = entity.Descricao,
            Preco = precoFormatado,
            Ativo = entity.Ativo,
            Ordem = entity.Ordem,
            AcompanhamentoCategoriaId = entity.AcompanhamentoCategoriaId
        };

        await PopularCategoriasAsync(model);

        ViewData["Title"] = "Editar Acompanhamento";
        ViewData["Description"] = "Atualize os dados do acompanhamento.";
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AcompanhamentoFormViewModel model)
    {
        var precoBruto = Request.Form["Preco"].ToString();
        
        if (!model.Id.HasValue)
            return NotFound();

        decimal preco = 0;

        if (!string.IsNullOrEmpty(model.Preco))
        {
            // Substitui a vírgula por ponto e faz a conversão para decimal
            preco = decimal.Parse(model.Preco.Replace(',', '.'), CultureInfo.InvariantCulture);
        }

        var entity = await _db.Acompanhamentos.FirstOrDefaultAsync(x => x.Id == model.Id.Value);

        if (entity == null)
            return NotFound();

        await PopularCategoriasAsync(model);

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Editar Acompanhamento";
            ViewData["Description"] = "Atualize os dados do acompanhamento.";
            return View(model);
        }

        entity.Nome = model.Nome.Trim();
        entity.Descricao = string.IsNullOrWhiteSpace(model.Descricao) ? null : model.Descricao.Trim();
        entity.Preco = preco;
        entity.Ativo = model.Ativo;
        entity.Ordem = model.Ordem;
        entity.AcompanhamentoCategoriaId = model.AcompanhamentoCategoriaId!.Value;

        await _db.SaveChangesAsync();

        TempData["Success"] = "Acompanhamento atualizado com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _db.Acompanhamentos.FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return NotFound();

        _db.Acompanhamentos.Remove(entity);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Acompanhamento removido com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopularCategoriasAsync(AcompanhamentoFormViewModel model)
    {
        model.Categorias = await _db.AcompanhamentoCategorias
            .AsNoTracking()
            .OrderBy(x => x.Nome)
            .Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = x.Nome
            })
            .ToListAsync();
    }
}