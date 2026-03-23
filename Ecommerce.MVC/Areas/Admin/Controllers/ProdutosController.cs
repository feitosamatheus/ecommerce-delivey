using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Ecommerce.MVC.Config;
using Ecommerce.MVC.Entities;
using Ecommerce.MVC.Models.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.MVC.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class ProdutosController : Controller
{
    private readonly DatabaseContext _db;
    private readonly IWebHostEnvironment _environment;

    public ProdutosController(DatabaseContext db, IWebHostEnvironment environment)
    {
        _db = db;
        _environment = environment;
    }

    public async Task<IActionResult> Index(string busca, Guid? categoriaId, int pagina = 1, int tamanhoPagina = 5)
    {
        var query = _db.Produtos
            .AsNoTracking()
            .Include(p => p.Categoria)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(busca))
        {
            query = query.Where(p =>
                p.Nome.Contains(busca) ||
                (p.Descricao != null && p.Descricao.Contains(busca)));
        }

        if (categoriaId.HasValue)
        {
            query = query.Where(p => p.CategoriaId == categoriaId.Value);
        }

        query = query.OrderBy(p => p.Nome);

        var totalItens = await query.CountAsync();
        var produtos = await query
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync();

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

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return PartialView("_ProdutosLista", model);
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new ProdutoCreateViewModel();

        await PopularCategoriasAsync(model);
        await PopularCategoriasAcompanhamentoAsync(model);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProdutoCreateViewModel model, IFormFile? imagemForm)
    {
        NormalizarCategoriasSelecionadas(model);
        ValidarCategoriasSelecionadas(model);
        ValidarRegrasCategorias(model);

        string imagemUrl = "/img/placeholder-produto.png";

        if (imagemForm != null && imagemForm.Length > 0)
        {
            var resultadoImagem = await TentarSalvarImagemAsync(imagemForm);

            if (!resultadoImagem.Sucesso)
            {
                ModelState.AddModelError("imagemForm", resultadoImagem.Erro!);
            }
            else if (!string.IsNullOrWhiteSpace(resultadoImagem.Caminho))
            {
                imagemUrl = resultadoImagem.Caminho;
            }
        }

        if (!ModelState.IsValid)
        {
            await PopularCategoriasAsync(model);
            await PopularCategoriasAcompanhamentoAsync(model);
            return View(model);
        }

        var produto = new Produto
        {
            Nome = model.Nome?.Trim(),
            Descricao = string.IsNullOrWhiteSpace(model.Descricao) ? null : model.Descricao.Trim(),
            Preco = model.Preco,
            TempoPreparoMinutos = model.TempoPreparoMinutos,
            CategoriaId = model.CategoriaId,
            Ativo = model.Ativo,
            ImagemUrl = imagemUrl
        };

        foreach (var item in model.CategoriasAcompanhamentoSelecionadas
                    .Where(x => x.AcompanhamentoCategoriaId.HasValue))
        {
            var categoriaProduto = new ProdutoAcompanhamentoCategoria
            {
                ProdutoId = produto.Id,
                AcompanhamentoCategoriaId = item.AcompanhamentoCategoriaId!.Value,
                Obrigatorio = item.Obrigatorio,
                MinSelecionados = item.MinSelecionados,
                MaxSelecionados = item.MaxSelecionados,
                Ordem = item.Ordem
            };

            var idsSelecionados = item.Acompanhamentos
                .Where(x => x.Selecionado)
                .Select(x => x.AcompanhamentoId)
                .Distinct()
                .ToList();

            if (idsSelecionados.Any())
            {
                var acompanhamentos = await _db.Acompanhamentos
                    .Where(x =>
                        idsSelecionados.Contains(x.Id) &&
                        x.AcompanhamentoCategoriaId == item.AcompanhamentoCategoriaId.Value &&
                        x.Ativo)
                    .ToListAsync();

                foreach (var acompanhamento in acompanhamentos)
                {
                    categoriaProduto.Acompanhamentos.Add(acompanhamento);
                }
            }

            produto.AcompanhamentoCategorias.Add(categoriaProduto);
        }

        _db.Produtos.Add(produto);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Produto cadastrado com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ObterAcompanhamentosPorCategoria(Guid categoriaId)
    {
        var categoria = await _db.AcompanhamentoCategorias
            .AsNoTracking()
            .Where(x => x.Id == categoriaId)
            .Select(x => new
            {
                id = x.Id,
                nome = x.Nome
            })
            .FirstOrDefaultAsync();

        if (categoria == null)
            return NotFound();

        var acompanhamentos = await _db.Acompanhamentos
            .AsNoTracking()
            .Where(x => x.AcompanhamentoCategoriaId == categoriaId && x.Ativo)
            .OrderBy(x => x.Nome)
            .Select(x => new
            {
                id = x.Id,
                nome = x.Nome
            })
            .ToListAsync();

        return Json(new
        {
            categoria,
            acompanhamentos
        });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var produto = await _db.Produtos
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (produto == null)
            return NotFound();

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

        await PopularCategoriasEditAsync();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, AdminProdutoFormViewModel model)
    {
        if (id != model.Id)
            return BadRequest();

        if (!ModelState.IsValid)
        {
            await PopularCategoriasEditAsync();
            return View(model);
        }

        var produto = await _db.Produtos.FirstOrDefaultAsync(p => p.Id == id);
        if (produto == null)
            return NotFound();

        produto.Nome = model.Nome.Trim();
        produto.Descricao = string.IsNullOrWhiteSpace(model.Descricao) ? null : model.Descricao.Trim();
        produto.Preco = model.Preco;
        produto.ImagemUrl = string.IsNullOrWhiteSpace(model.ImagemUrl) ? null : model.ImagemUrl.Trim();
        produto.TempoPreparoMinutos = model.TempoPreparoMinutos;
        produto.CategoriaId = model.CategoriaId;

        await _db.SaveChangesAsync();

        TempData["Success"] = "Produto atualizado com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var produto = await _db.Produtos.FirstOrDefaultAsync(p => p.Id == id);

        if (produto == null)
            return NotFound();

        produto.Ativo = false;

        _db.Produtos.Update(produto);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Produto desativado com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopularCategoriasAsync(ProdutoCreateViewModel model)
    {
        model.Categorias = await _db.Categorias
            .AsNoTracking()
            .OrderBy(x => x.Nome)
            .Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = x.Nome
            })
            .ToListAsync();
    }

    private async Task PopularCategoriasAcompanhamentoAsync(ProdutoCreateViewModel model)
    {
        var categorias = await _db.AcompanhamentoCategorias
            .AsNoTracking()
            .OrderBy(x => x.Nome)
            .Select(x => new
            {
                x.Id,
                x.Nome
            })
            .ToListAsync();

        model.CategoriasAcompanhamentoDisponiveis = categorias
            .Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = x.Nome
            })
            .ToList();

        if (model.CategoriasAcompanhamentoSelecionadas == null)
            model.CategoriasAcompanhamentoSelecionadas = new List<ProdutoCategoriaAcompanhamentoItemViewModel>();

        foreach (var item in model.CategoriasAcompanhamentoSelecionadas.Where(x => x.AcompanhamentoCategoriaId.HasValue))
        {
            var categoria = categorias.FirstOrDefault(x => x.Id == item.AcompanhamentoCategoriaId.Value);
            if (categoria == null)
                continue;

            item.NomeCategoria = categoria.Nome;
        }
    }
    private void NormalizarCategoriasSelecionadas(ProdutoCreateViewModel model)
    {
        if (model.CategoriasAcompanhamentoSelecionadas == null)
        {
            model.CategoriasAcompanhamentoSelecionadas = new List<ProdutoCategoriaAcompanhamentoItemViewModel>();
            return;
        }

        model.CategoriasAcompanhamentoSelecionadas = model.CategoriasAcompanhamentoSelecionadas
            .Where(x => x.AcompanhamentoCategoriaId.HasValue)
            .ToList();

        foreach (var categoria in model.CategoriasAcompanhamentoSelecionadas)
        {
            categoria.NomeCategoria = categoria.NomeCategoria?.Trim() ?? string.Empty;

            if (categoria.MinSelecionados < 0)
                categoria.MinSelecionados = 0;

            if (categoria.MaxSelecionados < 1)
                categoria.MaxSelecionados = 1;

            if (categoria.Ordem < 0)
                categoria.Ordem = 0;

            if (categoria.Acompanhamentos == null)
            {
                categoria.Acompanhamentos = new List<ProdutoAcompanhamentoSelecionadoItemViewModel>();
                continue;
            }

            categoria.Acompanhamentos = categoria.Acompanhamentos
                .GroupBy(x => x.AcompanhamentoId)
                .Select(g => g.First())
                .ToList();
        }
    }

    private void ValidarCategoriasSelecionadas(ProdutoCreateViewModel model)
    {
        if (model.CategoriasAcompanhamentoSelecionadas == null || !model.CategoriasAcompanhamentoSelecionadas.Any())
            return;

        var duplicados = model.CategoriasAcompanhamentoSelecionadas
            .Where(x => x.AcompanhamentoCategoriaId.HasValue)
            .GroupBy(x => x.AcompanhamentoCategoriaId!.Value)
            .Where(g => g.Count() > 1)
            .ToList();

        if (duplicados.Any())
        {
            ModelState.AddModelError(string.Empty, "A mesma categoria de acompanhamento não pode ser adicionada mais de uma vez.");
        }
    }

    private void ValidarRegrasCategorias(ProdutoCreateViewModel model)
    {
        if (model.CategoriasAcompanhamentoSelecionadas == null)
            return;

        foreach (var categoria in model.CategoriasAcompanhamentoSelecionadas)
        {
            var nome = string.IsNullOrWhiteSpace(categoria.NomeCategoria)
                ? "categoria selecionada"
                : categoria.NomeCategoria;

            var totalSelecionado = categoria.Acompanhamentos?.Count(x => x.Selecionado) ?? 0;

            if (categoria.MinSelecionados < 0)
            {
                ModelState.AddModelError(string.Empty, $"Na categoria '{nome}', o mínimo não pode ser menor que zero.");
            }

            if (categoria.MaxSelecionados < 1)
            {
                ModelState.AddModelError(string.Empty, $"Na categoria '{nome}', o máximo deve ser pelo menos 1.");
            }

            if (categoria.MinSelecionados > categoria.MaxSelecionados)
            {
                ModelState.AddModelError(string.Empty, $"Na categoria '{nome}', o mínimo não pode ser maior que o máximo.");
            }

            if (categoria.Obrigatorio && totalSelecionado < categoria.MinSelecionados)
            {
                ModelState.AddModelError(string.Empty, $"Na categoria '{nome}', selecione pelo menos {categoria.MinSelecionados} acompanhamento(s).");
            }

            if (totalSelecionado > categoria.MaxSelecionados)
            {
                ModelState.AddModelError(string.Empty, $"Na categoria '{nome}', selecione no máximo {categoria.MaxSelecionados} acompanhamento(s).");
            }

            if (categoria.Obrigatorio && totalSelecionado == 0)
            {
                ModelState.AddModelError(string.Empty, $"Na categoria '{nome}', selecione pelo menos um acompanhamento.");
            }
        }
    }

    private async Task<(bool Sucesso, string? Caminho, string? Erro)> TentarSalvarImagemAsync(IFormFile? imagemForm)
    {
        if (imagemForm == null || imagemForm.Length == 0)
            return (true, null, null);

        var extensoesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extensao = Path.GetExtension(imagemForm.FileName).ToLowerInvariant();

        if (!extensoesPermitidas.Contains(extensao))
            return (false, null, "Formato de imagem inválido. Use JPG, PNG ou WebP.");

        if (imagemForm.Length > 2 * 1024 * 1024)
            return (false, null, "A imagem deve ter no máximo 2MB.");

        var pastaImagens = Path.Combine(_environment.WebRootPath, "img", "produtos");

        if (!Directory.Exists(pastaImagens))
            Directory.CreateDirectory(pastaImagens);

        var nomeArquivo = $"{Guid.NewGuid()}{extensao}";
        var caminhoCompleto = Path.Combine(pastaImagens, nomeArquivo);

        await using (var stream = new FileStream(caminhoCompleto, FileMode.Create))
        {
            await imagemForm.CopyToAsync(stream);
        }

        return (true, $"/img/produtos/{nomeArquivo}", null);
    }

    private async Task PopularCategoriasEditAsync()
    {
        var categorias = await _db.Categorias
            .AsNoTracking()
            .OrderBy(c => c.Nome)
            .ToListAsync();

        ViewBag.Categorias = categorias;
    }
}