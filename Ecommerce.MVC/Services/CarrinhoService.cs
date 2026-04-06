using Ecommerce.MVC.Config;
using Ecommerce.MVC.Entities;
using Ecommerce.MVC.Helpers;
using Ecommerce.MVC.Interfaces;
using Ecommerce.MVC.Models.Produtos;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

public class CarrinhoService : ICarrinhoService
{
    private readonly DatabaseContext _db;

    public CarrinhoService(DatabaseContext db)
    {
        _db = db;
    }

    public async Task<Carrinho> ObterOuCriarCarrinhoAsync(HttpContext http, CancellationToken ct = default)
    {
        Guid? userId = null;

        if (http.User.Identity?.IsAuthenticated == true)
        {
            var idClaim = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(idClaim, out var parsed))
                userId = parsed;
        }

        if (userId.HasValue)
        {
            var carrinhoUsuario = await _db.Carrinhos
                .Include(c => c.Itens)
                .ThenInclude(i => i.Acompanhamentos)
                .FirstOrDefaultAsync(c => c.UserId == userId, ct);

            if (carrinhoUsuario != null)
                return carrinhoUsuario;

            var novoCarrinho = new Carrinho
            {
                UserId = userId,
                UpdatedAtUtc = DateTime.UtcNow
            };

            _db.Carrinhos.Add(novoCarrinho);
            await _db.SaveChangesAsync(ct);

            return novoCarrinho;
        }

        var token = CartTokenHelper.GetOrCreateToken(http);

        var carrinhoAnonimo = await _db.Carrinhos
            .Include(c => c.Itens)
            .ThenInclude(i => i.Acompanhamentos)
            .FirstOrDefaultAsync(c => c.Token == token && c.UserId == null, ct);

        if (carrinhoAnonimo != null)
            return carrinhoAnonimo;

        var novoAnonimo = new Carrinho
        {
            Token = token,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _db.Carrinhos.Add(novoAnonimo);
        await _db.SaveChangesAsync(ct);

        return novoAnonimo;
    }

    public async Task<Carrinho> AdicionarAsync(HttpContext http, AdicionarProdutoCarrinhoViewModel req, CancellationToken ct = default)
    {
        if (req.ProdutoId == Guid.Empty)
            throw new InvalidOperationException("Produto inválido.");

        if (req.Quantidade < 1)
            req.Quantidade = 1;

        if (req.Acompanhamentos == null)
        {
            req.Acompanhamentos = new List<CarrinhoAddAcompanhamentoViewModel>();
        }

        var carrinho = await ObterOuCriarCarrinhoAsync(http, ct);

        var produto = await _db.Set<Produto>()
            .AsNoTracking()
            .Include(p => p.AcompanhamentoCategorias)
                .ThenInclude(pc => pc.Categoria)
            .Include(p => p.AcompanhamentoCategorias)
                .ThenInclude(pc => pc.ProdutoAcompanhamentos)
                    .ThenInclude(px=> px.Acompanhamento)
            .FirstOrDefaultAsync(p => p.Id == req.ProdutoId && p.Ativo, ct);

        if (produto == null)
            throw new InvalidOperationException("Produto não encontrado.");

        var categoriasDoProduto = produto.AcompanhamentoCategorias
            .GroupBy(x => x.AcompanhamentoCategoriaId)
            .Select(g => g.First())
            .ToDictionary(x => x.AcompanhamentoCategoriaId);

        foreach (var sel in req.Acompanhamentos)
        {
            if (!categoriasDoProduto.TryGetValue(sel.CategoriaId, out var categoriaProduto))
                throw new InvalidOperationException("Categoria inválida para este produto.");

            var acompanhamentoValido = categoriaProduto.ProdutoAcompanhamentos
                .FirstOrDefault(a => a.Id == sel.AcompanhamentoId && a.Ativo);

            if (acompanhamentoValido == null)
                throw new InvalidOperationException("Acompanhamento inválido ou inativo para este produto.");
        }

        foreach (var categoriaProduto in categoriasDoProduto.Values)
        {
            var selecionadosNaCategoria = req.Acompanhamentos
                .Where(x => x.CategoriaId == categoriaProduto.AcompanhamentoCategoriaId)
                .ToList();

            var quantidadeSelecionada = selecionadosNaCategoria.Count;

            if (categoriaProduto.Obrigatorio && quantidadeSelecionada == 0)
            {
                throw new InvalidOperationException(
                    $"A categoria '{categoriaProduto.Categoria.Nome}' é obrigatória.");
            }

            if (quantidadeSelecionada < categoriaProduto.MinSelecionados)
            {
                throw new InvalidOperationException(
                    $"A categoria '{categoriaProduto.Categoria.Nome}' exige pelo menos {categoriaProduto.MinSelecionados} seleção(ões).");
            }

            if (categoriaProduto.MaxSelecionados > 0 && quantidadeSelecionada > categoriaProduto.MaxSelecionados)
            {
                throw new InvalidOperationException(
                    $"A categoria '{categoriaProduto.Categoria.Nome}' permite no máximo {categoriaProduto.MaxSelecionados} seleção(ões).");
            }
        }

        var item = new CarrinhoItem
        {
            CarrinhoId = carrinho.Id,
            ProdutoId = produto.Id,
            ProdutoNomeSnapshot = produto.Nome,
            PrecoBaseSnapshot = produto.Preco,
            Quantidade = req.Quantidade,
            Observacao = string.IsNullOrWhiteSpace(req.Observacao) ? null : req.Observacao.Trim()
        };

        foreach (var sel in req.Acompanhamentos)
        {
            var categoriaProduto = categoriasDoProduto[sel.CategoriaId];

            var acompanhamento = categoriaProduto.ProdutoAcompanhamentos
                .First(a => a.Id == sel.AcompanhamentoId && a.Ativo);

            item.Acompanhamentos.Add(new CarrinhoItemAcompanhamento
            {
                AcompanhamentoId = acompanhamento.Id,
                CategoriaId = categoriaProduto.AcompanhamentoCategoriaId,
                NomeSnapshot = acompanhamento.Acompanhamento.Nome,
                PrecoSnapshot = acompanhamento.Acompanhamento.Preco
            });
        }

        _db.CarrinhoItems.Add(item);

        carrinho.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return await _db.Carrinhos
            .Include(c => c.Itens)
            .ThenInclude(i => i.Acompanhamentos)
            .FirstAsync(c => c.Id == carrinho.Id, ct);
    }

    public async Task<int> ObterQuantidadeItensAsync(HttpContext http, CancellationToken ct = default)
    {
        var carrinho = await ObterOuCriarCarrinhoAsync(http, ct);

        if (carrinho == null || carrinho.Itens == null)
            return 0;

        // return carrinho.Itens.Sum(i => i.Quantidade);
        return carrinho.Itens.Count;
    }

    public async Task UnificarCarrinhoAsync(Guid clienteId, string token)
    {
        var carrinhoAnonimo = await _db.Carrinhos
            .Include(c => c.Itens)
            .ThenInclude(i => i.Acompanhamentos)
            .FirstOrDefaultAsync(c => c.Token == token && c.UserId == null);

        if (carrinhoAnonimo is null)
            return;

        var carrinhoCliente = await _db.Carrinhos
            .Include(c => c.Itens)
            .ThenInclude(i => i.Acompanhamentos)
            .FirstOrDefaultAsync(c => c.UserId == clienteId);

        if (carrinhoCliente is null)
        {
            carrinhoAnonimo.UserId = clienteId;
            carrinhoAnonimo.Token = null;
            await _db.SaveChangesAsync();
            return;
        }

        foreach (var itemAnonimo in carrinhoAnonimo.Itens.ToList())
        {
            var existente = carrinhoCliente.Itens
                .FirstOrDefault(i => i.ProdutoId == itemAnonimo.ProdutoId);

            if (existente is null)
            {
                carrinhoCliente.Itens.Add(itemAnonimo);
            }
            else
            {
                existente.Quantidade += itemAnonimo.Quantidade;

                foreach (var acompanhamento in itemAnonimo.Acompanhamentos)
                {
                    existente.Acompanhamentos.Add(acompanhamento);
                }
            }
        }

        _db.Carrinhos.Remove(carrinhoAnonimo);
        await _db.SaveChangesAsync();
    }
}