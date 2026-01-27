using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ecommerce.MVC.Config;
using Ecommerce.MVC.Entities;
using Ecommerce.MVC.Helpers;
using Ecommerce.MVC.Interfaces;
using Ecommerce.MVC.Models;
using Ecommerce.MVC.Models.Produtos;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.MVC.Services;

public class ProdutoService : IProdutoService
{
    private readonly DatabaseContext _db;

    public ProdutoService(DatabaseContext context)
    {
        _db = context;
    }

    public async Task<IEnumerable<Produto>> BuscarProdutoAsync()
    {
        return await _db.Produtos.AsNoTracking().Include(p => p.Categoria).OrderBy(p => p.Nome).ToListAsync();
    }

    public async Task<FinalizarPedidoModalViewModel> MontarModalAsync(HttpContext http, Guid clienteId, CancellationToken ct)
        {
            // 1) Cliente + Endereços
            var cliente = await _db.Set<Cliente>()
                .AsNoTracking()
                .Include(c => c.Enderecos)
                .FirstOrDefaultAsync(c => c.Id == clienteId, ct);

            if (cliente == null)
                throw new InvalidOperationException("Cliente não encontrado.");

            // 2) Carrinho pelo token (cookie/session)
            var token = CartTokenHelper.GetOrCreateToken(http);

            var carrinho = await _db.Carrinhos
                .AsNoTracking()
                .Include(c => c.Itens)
                    .ThenInclude(i => i.Acompanhamentos)
                .FirstOrDefaultAsync(c => c.Token == token, ct);

            if (carrinho == null)
                throw new InvalidOperationException("Carrinho não encontrado.");

            // 3) Monta resumo de itens + totalização
            var itensVm = new List<CarrinhoItemResumoVm>();
            decimal subtotal = 0m;

            foreach (var item in carrinho.Itens)
            {
                var somaAcomp = item.Acompanhamentos?.Sum(a => a.PrecoSnapshot) ?? 0m;
                var totalLinha = (item.PrecoBaseSnapshot + somaAcomp) * item.Quantidade;

                subtotal += totalLinha;

                itensVm.Add(new CarrinhoItemResumoVm
                {
                    ProdutoId = item.ProdutoId,
                    ProdutoNome = item.ProdutoNomeSnapshot,
                    Quantidade = item.Quantidade,
                    PrecoBase = item.PrecoBaseSnapshot,
                    PrecoAcompanhamentos = somaAcomp,
                    TotalLinha = totalLinha,
                    Acompanhamentos = item.Acompanhamentos?.Select(a => a.NomeSnapshot).ToList() ?? new List<string>()
                });
            }

            // 4) Taxa de entrega (placeholder)
            // Aqui você pode calcular por endereço/cidade/bairro etc.
            var taxaEntrega = 0m;
            var total = subtotal + taxaEntrega;

            var enderecos = cliente.Enderecos
                .OrderByDescending(e => e.EhPrincipal)
                .ThenByDescending(e => e.CriadoEm)
                .Select(e => new EnderecoResumoVm
                {
                    Id = e.Id,
                    Cep = e.Cep,
                    Logradouro = e.Logradouro,
                    Numero = e.Numero,
                    Complemento = e.Complemento,
                    Bairro = e.Bairro,
                    Cidade = e.Cidade,
                    Estado = e.Estado,
                    EhPrincipal = e.EhPrincipal
                })
                .ToList();

            var enderecoPrincipal = enderecos.FirstOrDefault(e => e.EhPrincipal)?.Id;

            return new FinalizarPedidoModalViewModel
            {
                ClienteId = cliente.Id,
                ClienteNome = cliente.Nome,
                ClienteTelefone = cliente.Telefone,

                Enderecos = enderecos,
                EnderecoSelecionadoId = enderecoPrincipal,

                Carrinho = new CarrinhoResumoVm
                {
                    CarrinhoId = carrinho.Id,
                    ItensCount = carrinho.Itens.Count,
                    Subtotal = subtotal,
                    TaxaEntrega = taxaEntrega,
                    Total = total,
                    Itens = itensVm
                }
            };
        }

    public async Task<Produto> ObterPorIdAsync(Guid produtoId)
    {
        return await _db.Produtos.AsNoTracking()
        .Include(p => p.Categoria)
        .Include(p => p.AcompanhamentoCategorias)
            .ThenInclude(pc => pc.Categoria)
                .ThenInclude(c => c.Acompanhamentos)
        .FirstOrDefaultAsync(p => p.Id == produtoId);
    }
}