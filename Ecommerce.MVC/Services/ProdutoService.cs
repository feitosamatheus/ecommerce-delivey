using Ecommerce.MVC.Config;
using Ecommerce.MVC.Entities;
using Ecommerce.MVC.Helpers;
using Ecommerce.MVC.Interfaces;
using Ecommerce.MVC.Migrations;
using Ecommerce.MVC.Models;
using Ecommerce.MVC.Models.Pedidos;
using Ecommerce.MVC.Models.Produtos;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

    //public async Task<FinalizarPedidoModalViewModel> MontarModalAsync(HttpContext http, Guid clienteId, CancellationToken ct)
    //    {
    //        // 1) Cliente + Endereços
    //        var cliente = await _db.Set<Cliente>()
    //            .AsNoTracking()
    //            .Include(c => c.Enderecos)
    //            .FirstOrDefaultAsync(c => c.Id == clienteId, ct);

    //        if (cliente == null)
    //            throw new InvalidOperationException("Cliente não encontrado.");

    //        // 2) Carrinho pelo token (cookie/session)
    //        var token = CartTokenHelper.GetOrCreateToken(http);

    //        var carrinho = await _db.Carrinhos
    //            .AsNoTracking()
    //            .Include(c => c.Itens)
    //                .ThenInclude(i => i.Acompanhamentos)
    //            .FirstOrDefaultAsync(c => c.Token == token, ct);

    //        if (carrinho == null)
    //            throw new InvalidOperationException("Carrinho não encontrado.");

    //        // 3) Monta resumo de itens + totalização
    //        var itensVm = new List<CarrinhoItemResumoVm>();
    //        decimal subtotal = 0m;

    //        foreach (var item in carrinho.Itens)
    //        {
    //            var somaAcomp = item.Acompanhamentos?.Sum(a => a.PrecoSnapshot) ?? 0m;
    //            var totalLinha = (item.PrecoBaseSnapshot + somaAcomp) * item.Quantidade;

    //            subtotal += totalLinha;

    //            itensVm.Add(new CarrinhoItemResumoVm
    //            {
    //                ProdutoId = item.ProdutoId,
    //                ProdutoNome = item.ProdutoNomeSnapshot,
    //                Quantidade = item.Quantidade,
    //                PrecoBase = item.PrecoBaseSnapshot,
    //                PrecoAcompanhamentos = somaAcomp,
    //                TotalLinha = totalLinha,
    //                Acompanhamentos = item.Acompanhamentos?.Select(a => a.NomeSnapshot).ToList() ?? new List<string>()
    //            });
    //        }

    //        // 4) Taxa de entrega (placeholder)
    //        // Aqui você pode calcular por endereço/cidade/bairro etc.
    //        var taxaEntrega = 0m;
    //        var total = subtotal + taxaEntrega;

    //        var enderecos = cliente.Enderecos
    //            .OrderByDescending(e => e.EhPrincipal)
    //            .ThenByDescending(e => e.CriadoEm)
    //            .Select(e => new EnderecoResumoVm
    //            {
    //                Id = e.Id,
    //                Cep = e.Cep,
    //                Logradouro = e.Logradouro,
    //                Numero = e.Numero,
    //                Complemento = e.Complemento,
    //                Bairro = e.Bairro,
    //                Cidade = e.Cidade,
    //                Estado = e.Estado,
    //                EhPrincipal = e.EhPrincipal
    //            })
    //            .ToList();

    //        var enderecoPrincipal = enderecos.FirstOrDefault(e => e.EhPrincipal)?.Id;

    //        return new FinalizarPedidoModalViewModel
    //        {
    //            ClienteId = cliente.Id,
    //            ClienteNome = cliente.Nome,
    //            ClienteTelefone = cliente.Telefone,

    //            Enderecos = enderecos,
    //            EnderecoSelecionadoId = enderecoPrincipal,

    //            Carrinho = new CarrinhoResumoVm
    //            {
    //                CarrinhoId = carrinho.Id,
    //                ItensCount = carrinho.Itens.Count,
    //                Subtotal = subtotal,
    //                TaxaEntrega = taxaEntrega,
    //                Total = total,
    //                Itens = itensVm
    //            }
    //        };
    //    }


    public async Task<FinalizarPedidoModalViewModel> MontarModalAsync(
    HttpContext http,
    Guid clienteId,
    CancellationToken ct)
{
    // 1) Cliente + Endereços
    var cliente = await _db.Set<Cliente>()
        .AsNoTracking()
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
            Acompanhamentos = item.Acompanhamentos?
            .Select(a => new AcompanhamentoDetalheVm // Certifique-se de ter essa classe ou use uma similar
            {
                Nome = a.NomeSnapshot,
                Preco = a.PrecoSnapshot
            })
            .ToList() ?? new List<AcompanhamentoDetalheVm>()
            });
    }

    // 4) 🔥 MAIOR TEMPO DE PREPARO DO CARRINHO
    var tempoMaximoMinutos = carrinho.Itens.Any()
        ? carrinho.Itens.Max(i => i.TempoPreparoMinutosSnapshot)
        : 0;

    // 5) 🔥 HORÁRIOS DISPONÍVEIS PARA RETIRADA
    var horariosRetirada = GerarHorariosRetirada(tempoMaximoMinutos);

    // 6) Taxa de entrega (placeholder)
    var taxaEntrega = 0m;
    var total = subtotal + taxaEntrega;

    // 8) ViewModel final
    return new FinalizarPedidoModalViewModel
    {
        ClienteId = cliente.Id,
        ClienteNome = cliente.Nome,
        ClienteTelefone = cliente.Telefone,

        Carrinho = new CarrinhoResumoVm
        {
            CarrinhoId = carrinho.Id,
            ItensCount = carrinho.Itens.Count,
            Subtotal = subtotal,
            TaxaEntrega = taxaEntrega,
            Total = total,
            Itens = itensVm
        },

        // 👉 DISPONÍVEL APENAS PARA RETIRADA
        HorariosRetirada = horariosRetirada
            .Select(h => new RetiradaHorarioVm
            {
                DataHora = h
            })
            .ToList()
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

    private static List<DateTime> GerarHorariosRetirada(
    int tempoPreparoMinutos,
    int diasDisponiveis = 30)
    {
        var resultado = new List<DateTime>();

        // 🔧 horário fixo para teste
        var agora = DateTime.Today.AddHours(22);
        // depois volta para: DateTime.Now

        var horarioMinimo = CalcularHorarioDisponivel(agora, tempoPreparoMinutos);
        horarioMinimo = ArredondarParaProximoMultiploDe5(horarioMinimo);

        var horarioInicial = AjustarParaHorarioFuncionamento(horarioMinimo);

        for (int dia = 0; dia < diasDisponiveis; dia++)
        {
            var data = horarioInicial.Date.AddDays(dia);

            var abertura = data.AddHours(8);
            var fechamento = data.AddHours(22);

            var inicio = dia == 0 ? horarioInicial : abertura;

            for (var h = inicio; h <= fechamento; h = h.AddMinutes(60))
            {
                resultado.Add(h);
            }
        }

        return resultado;
    }

    private static DateTime CalcularHorarioDisponivel(
    DateTime dataPedido,
    int tempoPreparoMinutos)
    {
        var abertura = new TimeSpan(8, 0, 0);
        var fechamento = new TimeSpan(22, 0, 0);

        DateTime atual = dataPedido;

        // 1️⃣ Ajusta o horário inicial para horário válido da loja
        if (atual.TimeOfDay >= fechamento)
        {
            // Após fechamento → começa no dia seguinte às 08:00
            atual = atual.Date.AddDays(1).Add(abertura);
        }
        else if (atual.TimeOfDay < abertura)
        {
            // Antes da abertura → começa às 08:00 do mesmo dia
            atual = atual.Date.Add(abertura);
        }

        int minutosRestantes = tempoPreparoMinutos;

        // 2️⃣ Soma o tempo respeitando o horário da loja
        while (minutosRestantes > 0)
        {
            var fimDoDia = atual.Date.Add(fechamento);
            var minutosDisponiveisHoje = (int)(fimDoDia - atual).TotalMinutes;

            if (minutosRestantes <= minutosDisponiveisHoje)
            {
                // Cabe tudo hoje
                atual = atual.AddMinutes(minutosRestantes);
                minutosRestantes = 0;
            }
            else
            {
                // Usa o que dá hoje e pula para o próximo dia
                minutosRestantes -= minutosDisponiveisHoje;
                atual = atual.Date.AddDays(1).Add(abertura);
            }
        }

        return atual;
    }


    private static DateTime AjustarParaHorarioFuncionamento(DateTime dataHora)
    {
        var abertura = dataHora.Date.AddHours(8);
        var fechamento = dataHora.Date.AddHours(22);

        if (dataHora < abertura)
            return abertura;

        if (dataHora > fechamento)
            return dataHora.Date.AddDays(1).AddHours(8);

        return dataHora;
    }

    private static DateTime ArredondarParaProximoMultiploDe5(DateTime dataHora)
    {
        var minutos = dataHora.Minute;
        var resto = minutos % 5;

        if (resto == 0)
            return dataHora;

        return dataHora.AddMinutes(5 - resto);
    }

}