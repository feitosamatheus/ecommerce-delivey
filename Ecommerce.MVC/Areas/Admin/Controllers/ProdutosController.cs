using Ecommerce.MVC.Config;
using Ecommerce.MVC.Entities;
using Ecommerce.MVC.Models.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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

    public async Task<IActionResult> Index(string? busca, Guid? categoriaId, int pagina = 1, int tamanhoPagina = 5)
    {
        var query = _db.Produtos
            .AsNoTracking()
            .Include(p => p.Categoria)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(busca))
        {
            busca = busca.Trim();

            query = query.Where(p =>
                EF.Functions.ILike(p.Nome, $"%{busca}%") ||
                (p.Descricao != null && EF.Functions.ILike(p.Descricao, $"%{busca}%")));
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

        if (!decimal.TryParse(
            model.Preco.Replace(".", "").Replace(",", "."),
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out var precoConvertido))
        {
            ModelState.AddModelError(nameof(model.Preco), "Informe um preço válido.");
        }

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
            Preco = precoConvertido,
            TempoPreparoMinutos = model.TempoPreparoMinutos,
            CategoriaId = model.CategoriaId,
            Ativo = model.Ativo,
            ImagemUrl = imagemUrl
        };

        foreach (var item in model.CategoriasAcompanhamentoSelecionadas
                     .Where(x => x.AcompanhamentoCategoriaId.HasValue))
        {
            var categoriaId = item.AcompanhamentoCategoriaId!.Value;

            var categoriaProduto = new ProdutoAcompanhamentoCategoria
            {
                ProdutoId = produto.Id,
                AcompanhamentoCategoriaId = categoriaId,
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
                        x.AcompanhamentoCategoriaId == categoriaId &&
                        x.Ativo)
                    .OrderBy(x => x.Ordem)
                    .ToListAsync();

                foreach (var acompanhamento in acompanhamentos)
                {
                    categoriaProduto.ProdutoAcompanhamentos.Add(new ProdutoAcompanhamento
                    {
                        Id = Guid.NewGuid(),
                        ProdutoId = produto.Id,
                        AcompanhamentoCategoriaId = categoriaId,
                        AcompanhamentoId = acompanhamento.Id,
                        DataAdicionado = DateTime.UtcNow,
                        Ativo = true,
                        Ordem = acompanhamento.Ordem
                    });
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
            .Include(p => p.AcompanhamentoCategorias)
                .ThenInclude(pc => pc.Categoria)
                    .ThenInclude(c => c.Acompanhamentos)
            .Include(p => p.AcompanhamentoCategorias)
                .ThenInclude(pc => pc.ProdutoAcompanhamentos)
                    .ThenInclude(pa => pa.Acompanhamento)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (produto == null)
            return NotFound();

        var ptBr = new System.Globalization.CultureInfo("pt-BR");

        var vm = new AdminProdutoFormViewModel
        {
            Id = produto.Id,
            Nome = produto.Nome,
            Descricao = produto.Descricao,
            Preco = produto.Preco.ToString("N2", ptBr),
            ImagemUrl = produto.ImagemUrl,
            TempoPreparoMinutos = produto.TempoPreparoMinutos,
            CategoriaId = produto.CategoriaId,
            Ativo = produto.Ativo,

            CategoriasAcompanhamentoSelecionadas = produto.AcompanhamentoCategorias
                .OrderBy(x => x.Ordem)
                .Select(x => new CategoriaAcompanhamentoSelecaoViewModel
                {
                    ProdutoId = x.ProdutoId,
                    AcompanhamentoCategoriaId = x.AcompanhamentoCategoriaId,
                    NomeCategoria = x.Categoria != null ? x.Categoria.Nome : string.Empty,
                    Obrigatorio = x.Obrigatorio,
                    MinSelecionados = x.MinSelecionados,
                    MaxSelecionados = x.MaxSelecionados,
                    Ordem = x.Ordem,

                    Acompanhamentos = x.Categoria != null
                        ? x.Categoria.Acompanhamentos
                            .OrderBy(a => a.Ordem)
                            .Select(a =>
                            {
                                var vinculo = x.ProdutoAcompanhamentos
                                    .FirstOrDefault(pa => pa.AcompanhamentoId == a.Id);

                                return new AcompanhamentoItemViewModel
                                {
                                    Id = vinculo != null ? vinculo.Id : Guid.Empty, // id do vínculo ProdutoAcompanhamento
                                    AcompanhamentoId = a.Id, // id do acompanhamento base
                                    Nome = a.Nome,
                                    Descricao = a.Descricao,
                                    Preco = a.Preco.ToString("N2", ptBr),
                                    Ativo = vinculo?.Ativo ?? true,
                                    Ordem = vinculo?.Ordem ?? a.Ordem,
                                    Selecionado = vinculo != null
                                };
                            })
                            .ToList()
                        : new List<AcompanhamentoItemViewModel>()
                })
                .ToList()
        };

        await PopularListasAsync(vm);

        return View(vm);
    }

    private async Task PopularListasAsync(AdminProdutoFormViewModel vm)
    {
        var categoriasMenu = await _db.Categorias
            .AsNoTracking()
            .OrderBy(c => c.Nome)
            .ToListAsync();

        vm.Categorias = categoriasMenu.Select(c => new SelectListItem
        {
            Value = c.Id.ToString(),
            Text = c.Nome
        });

        var categoriasAcomp = await _db.AcompanhamentoCategorias
            .AsNoTracking()
            .OrderBy(c => c.Nome)
            .ToListAsync();

        vm.CategoriasAcompanhamentoDisponiveis = categoriasAcomp.Select(c => new SelectListItem
        {
            Value = c.Id.ToString(),
            Text = c.Nome
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AdminProdutoFormViewModel vm, IFormFile? imagemForm)
    {
        decimal precoConvertido = 0m;

        if (string.IsNullOrWhiteSpace(vm.Preco))
        {
            ModelState.AddModelError(nameof(vm.Preco), "Informe o preço.");
        }
        else
        {
            var precoTexto = vm.Preco.Trim().Replace(".", "").Replace(",", ".");

            if (!decimal.TryParse(
                precoTexto,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out precoConvertido))
            {
                ModelState.AddModelError(nameof(vm.Preco), "Informe um preço válido.");
            }
            else if (precoConvertido <= 0)
            {
                ModelState.AddModelError(nameof(vm.Preco), "O preço deve ser maior que zero.");
            }
        }

        vm.CategoriasAcompanhamentoSelecionadas ??= new List<CategoriaAcompanhamentoSelecaoViewModel>();

        foreach (var grupo in vm.CategoriasAcompanhamentoSelecionadas)
        {
            grupo.Acompanhamentos ??= new List<AcompanhamentoItemViewModel>();
        }

        if (!ModelState.IsValid)
        {
            await PopularListasAsync(vm);
            return View(vm);
        }

        var produto = await _db.Produtos
            .Include(p => p.AcompanhamentoCategorias)
                .ThenInclude(pc => pc.ProdutoAcompanhamentos)
            .FirstOrDefaultAsync(p => p.Id == vm.Id);

        if (produto == null)
            return NotFound();

        produto.Nome = vm.Nome?.Trim() ?? string.Empty;
        produto.Descricao = string.IsNullOrWhiteSpace(vm.Descricao) ? null : vm.Descricao.Trim();
        produto.Preco = precoConvertido;
        produto.TempoPreparoMinutos = vm.TempoPreparoMinutos;
        produto.CategoriaId = vm.CategoriaId;
        produto.Ativo = vm.Ativo;

        // 1) Remove grupos que saíram da tela
        var categoriasVindasDaTela = vm.CategoriasAcompanhamentoSelecionadas
            .Where(x => x.AcompanhamentoCategoriaId != Guid.Empty)
            .Select(x => x.AcompanhamentoCategoriaId)
            .ToHashSet();

        var gruposParaRemover = produto.AcompanhamentoCategorias
            .Where(x => !categoriasVindasDaTela.Contains(x.AcompanhamentoCategoriaId))
            .ToList();

        foreach (var grupoRemover in gruposParaRemover)
        {
            if (grupoRemover.ProdutoAcompanhamentos.Any())
            {
                _db.ProdutoAcompanhamentos.RemoveRange(grupoRemover.ProdutoAcompanhamentos);
            }

            _db.Remove(grupoRemover);
        }

        // 2) Cria ou atualiza grupos
        foreach (var grupoVm in vm.CategoriasAcompanhamentoSelecionadas
                     .Where(x => x.AcompanhamentoCategoriaId != Guid.Empty))
        {
            var relacao = produto.AcompanhamentoCategorias
                .FirstOrDefault(x => x.AcompanhamentoCategoriaId == grupoVm.AcompanhamentoCategoriaId);

            if (relacao == null)
            {
                relacao = new ProdutoAcompanhamentoCategoria
                {
                    ProdutoId = produto.Id,
                    AcompanhamentoCategoriaId = grupoVm.AcompanhamentoCategoriaId,
                    Obrigatorio = grupoVm.Obrigatorio,
                    MinSelecionados = grupoVm.MinSelecionados,
                    MaxSelecionados = grupoVm.MaxSelecionados,
                    Ordem = grupoVm.Ordem,
                    ProdutoAcompanhamentos = new List<ProdutoAcompanhamento>()
                };

                produto.AcompanhamentoCategorias.Add(relacao);
            }
            else
            {
                relacao.Obrigatorio = grupoVm.Obrigatorio;
                relacao.MinSelecionados = grupoVm.MinSelecionados;
                relacao.MaxSelecionados = grupoVm.MaxSelecionados;
                relacao.Ordem = grupoVm.Ordem;

                // Estratégia segura: limpa os vínculos atuais e recria
                if (relacao.ProdutoAcompanhamentos.Any())
                {
                    _db.ProdutoAcompanhamentos.RemoveRange(relacao.ProdutoAcompanhamentos.ToList());
                    relacao.ProdutoAcompanhamentos.Clear();
                }
            }

            // recria somente os selecionados
            foreach (var itemVm in grupoVm.Acompanhamentos.Where(x => x.Selecionado && x.AcompanhamentoId != Guid.Empty))
            {
                relacao.ProdutoAcompanhamentos.Add(new ProdutoAcompanhamento
                {
                    Id = Guid.NewGuid(),
                    ProdutoId = produto.Id,
                    AcompanhamentoCategoriaId = grupoVm.AcompanhamentoCategoriaId,
                    AcompanhamentoId = itemVm.AcompanhamentoId,
                    DataAdicionado = DateTime.UtcNow,
                    Ativo = itemVm.Ativo,
                    Ordem = itemVm.Ordem
                });
            }
        }

        await _db.SaveChangesAsync();

        TempData["Success"] = "Produto atualizado com sucesso.";
        return RedirectToAction(nameof(Index));
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoverCategoriaAcompanhamento(Guid produtoId, Guid acompanhamentoCategoriaId)
    {
        var relacao = await _db.Set<ProdutoAcompanhamentoCategoria>()
            .Include(x => x.ProdutoAcompanhamentos)
            .FirstOrDefaultAsync(x =>
                x.ProdutoId == produtoId &&
                x.AcompanhamentoCategoriaId == acompanhamentoCategoriaId);

        if (relacao == null)
        {
            return Json(new
            {
                sucesso = false,
                mensagem = "Grupo de acompanhamento não encontrado."
            });
        }

        if (relacao.ProdutoAcompanhamentos != null && relacao.ProdutoAcompanhamentos.Any())
        {
            _db.RemoveRange(relacao.ProdutoAcompanhamentos);
        }

        _db.Remove(relacao);

        await _db.SaveChangesAsync();

        return Json(new { sucesso = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SalvarSelecaoAcompanhamento(
    Guid produtoId,
    Guid acompanhamentoCategoriaId,
    Guid acompanhamentoId,
    bool selecionado,
    bool obrigatorio,
    int minSelecionados,
    int maxSelecionados,
    int ordemGrupo,
    bool ativo,
    int ordemItem)
    {
        try
        {
            var produtoExiste = await _db.Produtos
                .AsNoTracking()
                .AnyAsync(p => p.Id == produtoId);

            if (!produtoExiste)
            {
                return Json(new { sucesso = false, mensagem = "Produto não encontrado." });
            }

            var grupo = await _db.Set<ProdutoAcompanhamentoCategoria>()
                .Include(x => x.ProdutoAcompanhamentos)
                .FirstOrDefaultAsync(x =>
                    x.ProdutoId == produtoId &&
                    x.AcompanhamentoCategoriaId == acompanhamentoCategoriaId);

            if (grupo == null)
            {
                grupo = new ProdutoAcompanhamentoCategoria
                {
                    ProdutoId = produtoId,
                    AcompanhamentoCategoriaId = acompanhamentoCategoriaId,
                    Obrigatorio = obrigatorio,
                    MinSelecionados = minSelecionados,
                    MaxSelecionados = maxSelecionados,
                    Ordem = ordemGrupo
                };

                _db.Set<ProdutoAcompanhamentoCategoria>().Add(grupo);
            }
            else
            {
                grupo.Obrigatorio = obrigatorio;
                grupo.MinSelecionados = minSelecionados;
                grupo.MaxSelecionados = maxSelecionados;
                grupo.Ordem = ordemGrupo;
            }

            var vinculo = await _db.Set<ProdutoAcompanhamento>()
                .FirstOrDefaultAsync(x =>
                    x.ProdutoId == produtoId &&
                    x.AcompanhamentoCategoriaId == acompanhamentoCategoriaId &&
                    x.AcompanhamentoId == acompanhamentoId);

            if (selecionado)
            {
                if (vinculo == null)
                {
                    vinculo = new ProdutoAcompanhamento
                    {
                        Id = Guid.NewGuid(),
                        ProdutoId = produtoId,
                        AcompanhamentoCategoriaId = acompanhamentoCategoriaId,
                        AcompanhamentoId = acompanhamentoId,
                        DataAdicionado = DateTime.UtcNow,
                        Ativo = ativo,
                        Ordem = ordemItem
                    };

                    _db.Set<ProdutoAcompanhamento>().Add(vinculo);
                }
                else
                {
                    vinculo.Ativo = ativo;
                    vinculo.Ordem = ordemItem;
                }
            }
            else
            {
                if (vinculo != null)
                {
                    _db.Set<ProdutoAcompanhamento>().Remove(vinculo);
                }
            }

            await _db.SaveChangesAsync();

            return Json(new { sucesso = true });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Json(new
            {
                sucesso = false,
                mensagem = "O registro foi alterado por outra operação. Atualize a página e tente novamente."
            });
        }
        catch (Exception)
        {
            return Json(new
            {
                sucesso = false,
                mensagem = "Não foi possível salvar a seleção do acompanhamento."
            });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoverAcompanhamento(Guid produtoId, Guid acompanhamentoCategoriaId, Guid acompanhamentoId)
    {
        var acompanhamento = await _db.Set<ProdutoAcompanhamento>()
            .FirstOrDefaultAsync(a =>
                a.Id == acompanhamentoId &&
                a.AcompanhamentoCategoriaId == acompanhamentoCategoriaId);

        if (acompanhamento == null)
        {
            return Json(new
            {
                sucesso = false,
                mensagem = "Acompanhamento não encontrado."
            });
        }

        _db.Remove(acompanhamento);
        await _db.SaveChangesAsync();

        return Json(new { sucesso = true });
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