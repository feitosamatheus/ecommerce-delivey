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
using System.Text.Json;
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

    // public async Task<IActionResult> Index(string? busca, Guid? categoriaId, int pagina = 1, int tamanhoPagina = 5)
    // {
    //     var query = _db.Produtos
    //         .AsNoTracking()
    //         .Include(p => p.Categoria)
    //         .AsQueryable();

    //     if (!string.IsNullOrWhiteSpace(busca))
    //     {
    //         busca = busca.Trim();

    //         query = query.Where(p =>
    //             EF.Functions.ILike(p.Nome, $"%{busca}%") ||
    //             (p.Descricao != null && EF.Functions.ILike(p.Descricao, $"%{busca}%")));
    //     }

    //     if (categoriaId.HasValue)
    //     {
    //         query = query.Where(p => p.CategoriaId == categoriaId.Value);
    //     }

    //     query = query.OrderBy(p => p.Nome);

    //     var totalItens = await query.CountAsync();
    //     var produtos = await query
    //         .Skip((pagina - 1) * tamanhoPagina)
    //         .Take(tamanhoPagina)
    //         .ToListAsync();

    //     var model = new ProdutosIndexViewModel
    //     {
    //         Produtos = produtos,
    //         Categorias = await _db.Categorias
    //             .AsNoTracking()
    //             .OrderBy(c => c.Nome)
    //             .Select(c => new SelectListItem
    //             {
    //                 Value = c.Id.ToString(),
    //                 Text = c.Nome,
    //                 Selected = c.Id == categoriaId
    //             })
    //             .ToListAsync(),
    //         PaginaAtual = pagina,
    //         TotalPaginas = (int)Math.Ceiling(totalItens / (double)tamanhoPagina),
    //         TotalItens = totalItens,
    //         TamanhoPagina = tamanhoPagina,
    //         Busca = busca,
    //         CategoriaId = categoriaId
    //     };

    //     if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
    //     {
    //         return PartialView("_ProdutosLista", model);
    //     }

    //     return View(model);
    // }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? busca,
        Guid? categoriaId,
        string? sortColumn = "DataCriacao",
        string? sortDirection = "desc",
        int pagina = 1,
        int tamanhoPagina = 5)
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

        sortColumn ??= "DataCriacao";
        sortDirection = (sortDirection ?? "desc").ToLower();

        query = (sortColumn, sortDirection) switch
        {
            ("Nome", "desc") => query.OrderByDescending(p => p.Nome),

            ("Preco", "asc") => query.OrderBy(p => p.Preco),
            ("Preco", "desc") => query.OrderByDescending(p => p.Preco),

            ("Categoria", "asc") => query.OrderBy(p => p.Categoria != null ? p.Categoria.Nome : ""),
            ("Categoria", "desc") => query.OrderByDescending(p => p.Categoria != null ? p.Categoria.Nome : ""),

            ("Status", "asc") => query.OrderBy(p => p.Ativo),
            ("Status", "desc") => query.OrderByDescending(p => p.Ativo),

            ("DataCriacao", "asc") => query.OrderBy(p => p.DataCriacaoUtc),
            ("DataCriacao", "desc") => query.OrderByDescending(p => p.DataCriacaoUtc),

            (_, "desc") => query.OrderByDescending(p => p.Nome),
            _ => query.OrderBy(p => p.Nome)
        };

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
            CategoriaId = categoriaId,
            SortColumn = sortColumn,
            SortDirection = sortDirection
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
            .FirstOrDefaultAsync(x => x.Id == categoriaId);

        if (categoria == null)
            return NotFound(new { mensagem = "Categoria não encontrada." });

        var ptBr = new CultureInfo("pt-BR");

        var acompanhamentos = await _db.Acompanhamentos
            .AsNoTracking()
            .Where(x => x.AcompanhamentoCategoriaId == categoriaId && x.Ativo)
            .OrderBy(x => x.Ordem)
            .Select(x => new
            {
                id = x.Id,
                acompanhamentoId = x.Id,
                nome = x.Nome,
                descricao = x.Descricao,
                preco = x.Preco.ToString("N2", ptBr),
                ativo = x.Ativo,
                ordem = x.Ordem,
                selecionado = false
            })
            .ToListAsync();

        return Json(new
        {
            categoria = new
            {
                id = categoria.Id,
                nome = categoria.Nome,
                descricao = categoria.Descricao
            },
            acompanhamentos
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AdminProdutoFormViewModel vm, IFormFile? imagemForm)
    {
        Console.WriteLine("===== EDIT POST =====");
        Console.WriteLine($"ProdutoId: {vm.Id}");
        Console.WriteLine($"PersonalizacaoJson vazio? {string.IsNullOrWhiteSpace(vm.PersonalizacaoJson)}");
        Console.WriteLine($"Qtd grupos bindados antes do fallback: {vm.CategoriasAcompanhamentoSelecionadas?.Count ?? 0}");

        decimal precoConvertido = 0m;

        if (string.IsNullOrWhiteSpace(vm.Preco) ||
            !decimal.TryParse(
                vm.Preco.Replace(".", "").Replace(",", "."),
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out precoConvertido))
        {
            ModelState.AddModelError(nameof(vm.Preco), "Informe um preço válido.");
        }

        vm.CategoriasAcompanhamentoSelecionadas ??= new List<CategoriaAcompanhamentoSelecaoViewModel>();

        // fallback: monta a lista a partir do JSON
        if ((!vm.CategoriasAcompanhamentoSelecionadas.Any() ||
            vm.CategoriasAcompanhamentoSelecionadas.All(x => x.AcompanhamentoCategoriaId == Guid.Empty))
            && !string.IsNullOrWhiteSpace(vm.PersonalizacaoJson))
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var personalizacao = JsonSerializer.Deserialize<PersonalizacaoJsonPostViewModel>(
                    vm.PersonalizacaoJson,
                    options);

                if (personalizacao?.ProdutoAcompanhamentoCategorias != null)
                {
                    vm.CategoriasAcompanhamentoSelecionadas = personalizacao.ProdutoAcompanhamentoCategorias
                        .Select(grupo => new CategoriaAcompanhamentoSelecaoViewModel
                        {
                            ProdutoId = vm.Id,
                            AcompanhamentoCategoriaId = TryParseGuid(grupo.AcompanhamentoCategoriaId),
                            NomeCategoria = grupo.NomeCategoria ?? string.Empty,
                            Obrigatorio = grupo.Obrigatorio,
                            MinSelecionados = grupo.MinSelecionados,
                            MaxSelecionados = grupo.MaxSelecionados,
                            Ordem = grupo.Ordem,
                            Acompanhamentos = (grupo.ProdutoAcompanhamentos ?? new List<ProdutoAcompanhamentoJsonPostViewModel>())
                                .Select(item => new AcompanhamentoItemViewModel
                                {
                                    Id = TryParseGuid(item.Id),
                                    AcompanhamentoId = TryParseGuid(item.AcompanhamentoId),
                                    Nome = item.Nome ?? string.Empty,
                                    Descricao = item.Descricao,
                                    Preco = item.Preco ?? "0,00",
                                    Ativo = item.Ativo,
                                    Ordem = item.Ordem,
                                    Selecionado = item.Selecionado
                                })
                                .ToList()
                        })
                        .ToList();
                }

                Console.WriteLine($"Qtd grupos após desserializar JSON: {vm.CategoriasAcompanhamentoSelecionadas.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro ao desserializar PersonalizacaoJson:");
                Console.WriteLine(ex.ToString());
                ModelState.AddModelError("", "Não foi possível processar a personalização enviada.");
            }
        }

        foreach (var grupo in vm.CategoriasAcompanhamentoSelecionadas)
        {
            grupo.Acompanhamentos ??= new List<AcompanhamentoItemViewModel>();
        }

        Console.WriteLine($"Qtd grupos finais para salvar: {vm.CategoriasAcompanhamentoSelecionadas.Count}");

        for (int i = 0; i < vm.CategoriasAcompanhamentoSelecionadas.Count; i++)
        {
            var grupo = vm.CategoriasAcompanhamentoSelecionadas[i];
            Console.WriteLine($"Grupo {i}: CategoriaId={grupo.AcompanhamentoCategoriaId}, Nome={grupo.NomeCategoria}, Obrigatorio={grupo.Obrigatorio}, Min={grupo.MinSelecionados}, Max={grupo.MaxSelecionados}, Ordem={grupo.Ordem}");
            Console.WriteLine($"Grupo {i}: Qtd itens={grupo.Acompanhamentos?.Count ?? 0}");

            if (grupo.Acompanhamentos != null)
            {
                for (int j = 0; j < grupo.Acompanhamentos.Count; j++)
                {
                    var item = grupo.Acompanhamentos[j];
                    Console.WriteLine($"  Item {j}: AcompanhamentoId={item.AcompanhamentoId}, Nome={item.Nome}, Selecionado={item.Selecionado}, Ativo={item.Ativo}, Ordem={item.Ordem}, Preco={item.Preco}");
                }
            }
        }

        var produto = await _db.Produtos
            .FirstOrDefaultAsync(p => p.Id == vm.Id);

        if (produto == null)
            return NotFound();

        string imagemUrl = produto.ImagemUrl ?? "/img/placeholder-produto.png";

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

        // if (!ModelState.IsValid)
        // {
        //     foreach (var item in ModelState)
        //     {
        //         var key = item.Key;
        //         var errors = item.Value.Errors;

        //         foreach (var error in errors)
        //         {
        //             Console.WriteLine($"KEY: {key}");
        //             Console.WriteLine($"ERROR: {error.ErrorMessage}");
        //             Console.WriteLine($"EXCEPTION: {error.Exception}");
        //         }
        //     }
        //     await PopularListasAsync(vm);
        //     return View(vm);
        // }

        produto.Nome = vm.Nome?.Trim() ?? string.Empty;
        produto.Descricao = string.IsNullOrWhiteSpace(vm.Descricao) ? null : vm.Descricao.Trim();
        produto.Preco = precoConvertido;
        produto.TempoPreparoMinutos = vm.TempoPreparoMinutos;
        produto.CategoriaId = vm.CategoriaId;
        produto.Ativo = vm.Ativo;
        produto.ImagemUrl = imagemUrl;

        // remove todos os vínculos atuais
        var acompanhamentosExistentes = await _db.ProdutoAcompanhamentos
            .Where(x => x.ProdutoId == produto.Id)
            .ToListAsync();

        if (acompanhamentosExistentes.Any())
        {
            _db.ProdutoAcompanhamentos.RemoveRange(acompanhamentosExistentes);
        }

        var gruposExistentes = await _db.Set<ProdutoAcompanhamentoCategoria>()
            .Where(x => x.ProdutoId == produto.Id)
            .ToListAsync();

        if (gruposExistentes.Any())
        {
            _db.Set<ProdutoAcompanhamentoCategoria>().RemoveRange(gruposExistentes);
        }

        await _db.SaveChangesAsync();

        // recria com base na lista final
        var gruposValidos = vm.CategoriasAcompanhamentoSelecionadas
            .Where(x => x.AcompanhamentoCategoriaId != Guid.Empty)
            .ToList();

        Console.WriteLine($"Qtd grupos válidos para persistir: {gruposValidos.Count}");

        foreach (var grupoVm in gruposValidos)
        {
            var novaCategoriaProduto = new ProdutoAcompanhamentoCategoria
            {
                ProdutoId = produto.Id,
                AcompanhamentoCategoriaId = grupoVm.AcompanhamentoCategoriaId,
                Obrigatorio = grupoVm.Obrigatorio,
                MinSelecionados = grupoVm.MinSelecionados,
                MaxSelecionados = grupoVm.MaxSelecionados,
                Ordem = grupoVm.Ordem
            };

            var itensSelecionados = grupoVm.Acompanhamentos
                .Where(x => x.Selecionado && x.AcompanhamentoId != Guid.Empty)
                .ToList();

            Console.WriteLine($"Persistindo grupo {grupoVm.NomeCategoria} - itens selecionados: {itensSelecionados.Count}");

            foreach (var itemVm in itensSelecionados)
            {
                novaCategoriaProduto.ProdutoAcompanhamentos.Add(new ProdutoAcompanhamento
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

            _db.Set<ProdutoAcompanhamentoCategoria>().Add(novaCategoriaProduto);
        }

        await _db.SaveChangesAsync();

        // TempData["Success"] = "Produto atualizado com sucesso.";
        // return RedirectToAction(nameof(Index));

        await PopularListasAsync(vm);
        vm.ImagemUrl = produto.ImagemUrl;

        TempData["Success"] = "Produto atualizado com sucesso.";
        return View(vm);
    }

    private static Guid TryParseGuid(string? valor)
    {
        return Guid.TryParse(valor, out var guid) ? guid : Guid.Empty;
    }

    public class PersonalizacaoJsonPostViewModel
{
    public string? ProdutoId { get; set; }
    public List<ProdutoAcompanhamentoCategoriaJsonPostViewModel> ProdutoAcompanhamentoCategorias { get; set; } = new();
}

public class ProdutoAcompanhamentoCategoriaJsonPostViewModel
{
    public string? Id { get; set; }
    public string? ProdutoId { get; set; }
    public string? AcompanhamentoCategoriaId { get; set; }
    public string? NomeCategoria { get; set; }
    public string? DescricaoCategoria { get; set; }
    public bool Obrigatorio { get; set; }
    public int MinSelecionados { get; set; }
    public int MaxSelecionados { get; set; }
    public int Ordem { get; set; }
    public List<ProdutoAcompanhamentoJsonPostViewModel> ProdutoAcompanhamentos { get; set; } = new();
}

public class ProdutoAcompanhamentoJsonPostViewModel
{
    public string? Id { get; set; }
    public string? ProdutoId { get; set; }
    public string? AcompanhamentoCategoriaId { get; set; }
    public string? AcompanhamentoId { get; set; }
    public string? Nome { get; set; }
    public string? Descricao { get; set; }
    public string? Preco { get; set; }
    public bool Ativo { get; set; }
    public int Ordem { get; set; }
    public bool Selecionado { get; set; }
}

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var produto = await _db.Produtos
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
            Ativo = produto.Ativo
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
    
    [HttpGet]
    public async Task<IActionResult> ObterPersonalizacao(Guid id)
    {
        var produto = await _db.Produtos
            .AsNoTracking()
            .Include(p => p.AcompanhamentoCategorias.OrderBy(pc => pc.Ordem))
                .ThenInclude(pc => pc.Categoria)
            .Include(p => p.AcompanhamentoCategorias)
                .ThenInclude(pc => pc.ProdutoAcompanhamentos.OrderBy(pa => pa.Ordem))
                    .ThenInclude(pa => pa.Acompanhamento)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (produto == null)
            return NotFound(new { mensagem = "Produto não encontrado." });

        var response = new ProdutoPersonalizacaoResponse
        {
            ProdutoId = produto.Id,
            NomeProduto = produto.Nome,
            ProdutoAcompanhamentoCategorias = produto.AcompanhamentoCategorias
                .OrderBy(pc => pc.Ordem)
                .Select(pc => new ProdutoAcompanhamentoCategoriaJsonViewModel
                {
                    ProdutoId = pc.ProdutoId,
                    AcompanhamentoCategoriaId = pc.AcompanhamentoCategoriaId,

                    NomeCategoria = pc.Categoria != null ? pc.Categoria.Nome : string.Empty,
                    DescricaoCategoria = pc.Categoria != null ? pc.Categoria.Descricao : null,

                    Obrigatorio = pc.Obrigatorio,
                    MinSelecionados = pc.MinSelecionados,
                    MaxSelecionados = pc.MaxSelecionados,
                    Ordem = pc.Ordem,

                    ProdutoAcompanhamentos = pc.ProdutoAcompanhamentos
                        .OrderBy(pa => pa.Ordem)
                        .Select(pa => new ProdutoAcompanhamentoJsonViewModel
                        {
                            Id = pa.Id,
                            ProdutoId = pa.ProdutoId,
                            AcompanhamentoCategoriaId = pa.AcompanhamentoCategoriaId,
                            AcompanhamentoId = pa.AcompanhamentoId,

                            Nome = pa.Acompanhamento != null ? pa.Acompanhamento.Nome : string.Empty,
                            Descricao = pa.Acompanhamento != null ? pa.Acompanhamento.Descricao : null,

                            Preco = pa.Acompanhamento != null ? pa.Acompanhamento.Preco : 0m,
                            Ativo = pa.Ativo,
                            Ordem = pa.Ordem,
                            DataAdicionado = pa.DataAdicionado
                        })
                        .ToList()
                })
                .ToList()
        };

        return Json(response);
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