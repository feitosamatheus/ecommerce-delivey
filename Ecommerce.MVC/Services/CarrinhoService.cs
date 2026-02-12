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
        if (req.ProdutoId == Guid.Empty) throw new InvalidOperationException("Produto inválido.");
        if (req.Quantidade < 1) req.Quantidade = 1;

        var carrinho = await ObterOuCriarCarrinhoAsync(http, ct);

        // Carrega o produto com categorias e acompanhamentos necessários para validar/snapshot
        var produto = await _db.Set<Produto>()
            .Include(p => p.AcompanhamentoCategorias)
                .ThenInclude(pc => pc.Categoria)
                    .ThenInclude(cat => cat.Acompanhamentos)
            .FirstOrDefaultAsync(p => p.Id == req.ProdutoId, ct);

        if (produto == null) throw new InvalidOperationException("Produto não encontrado.");

        // Validação mínima: acompanhamentos precisam pertencer ao produto
        var categoriasDoProduto = produto.AcompanhamentoCategorias
            .Select(x => x.Categoria)
            .GroupBy(c => c.Id)
            .Select(g => g.First())
            .ToDictionary(c => c.Id);

        // (Opcional) valida min/max por categoria usando req.Acompanhamentos
        foreach (var cat in categoriasDoProduto.Values)
        {
            var selecionados = req.Acompanhamentos.Count(a => a.CategoriaId == cat.Id);
            if (cat.Obrigatorio && selecionados < cat.MinSelecionados)
                throw new InvalidOperationException($"Categoria '{cat.Nome}' exige pelo menos {cat.MinSelecionados} seleção(ões).");

            if (cat.MaxSelecionados > 0 && selecionados > cat.MaxSelecionados)
                throw new InvalidOperationException($"Categoria '{cat.Nome}' permite no máximo {cat.MaxSelecionados} seleção(ões).");
        }

        // Monta snapshot do item
        var item = new CarrinhoItem
        {
            CarrinhoId = carrinho.Id,
            ProdutoId = produto.Id,
            ProdutoNomeSnapshot = produto.Nome,
            PrecoBaseSnapshot = produto.Preco,
            Quantidade = req.Quantidade,
            Observacao = string.IsNullOrWhiteSpace(req.Observacao) ? null : req.Observacao.Trim()
        };

        // Snapshot dos acompanhamentos selecionados
        foreach (var sel in req.Acompanhamentos)
        {
            if (!categoriasDoProduto.TryGetValue(sel.CategoriaId, out var cat))
                throw new InvalidOperationException("Categoria inválida para este produto.");

            var acomp = cat.Acompanhamentos.FirstOrDefault(a => a.Id == sel.AcompanhamentoId && a.Ativo);
            if (acomp == null)
                throw new InvalidOperationException("Acompanhamento inválido ou inativo.");

            item.Acompanhamentos.Add(new CarrinhoItemAcompanhamento
            {
                AcompanhamentoId = acomp.Id,
                CategoriaId = cat.Id,
                NomeSnapshot = acomp.Nome,
                PrecoSnapshot = acomp.Preco
            });
        }

        _db.CarrinhoItems.Add(item);

        carrinho.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        // Retorna carrinho atualizado
        return await _db.Carrinhos
            .Include(c => c.Itens).ThenInclude(i => i.Acompanhamentos)
            .FirstAsync(c => c.Id == carrinho.Id, ct);
    }

    public async Task<int> ObterQuantidadeItensAsync(HttpContext http, CancellationToken ct = default)
    {
        var carrinho = await ObterOuCriarCarrinhoAsync(http, ct);

        if (carrinho == null || carrinho.Itens == null)
            return 0;

        return carrinho.Itens.Sum(i => i.Quantidade);
    }

    public async Task UnificarCarrinhoAsync(Guid clienteId, string token)
    {
        var carrinhoAnonimo = await _db.Carrinhos
            .Include(c => c.Itens)
            .FirstOrDefaultAsync(c => c.Token == token && c.UserId == null);

        if (carrinhoAnonimo is null)
            return;

        var carrinhoCliente = await _db.Carrinhos
            .Include(c => c.Itens)
            .FirstOrDefaultAsync(c => c.UserId == clienteId);

        if (carrinhoCliente is null)
        {
            carrinhoAnonimo.UserId = clienteId;
            carrinhoAnonimo.Token = null;
            await _db.SaveChangesAsync();
            return;
        }

        foreach (var itemAnonimo in carrinhoAnonimo.Itens)
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
            }
        }

        _db.Carrinhos.Remove(carrinhoAnonimo);

        await _db.SaveChangesAsync();
    }
}
