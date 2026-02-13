using Ecommerce.MVC.Config;
using Ecommerce.MVC.Entities;
using Ecommerce.MVC.Enums;
using Ecommerce.MVC.Helpers;
using Ecommerce.MVC.Interfaces;
using Ecommerce.MVC.Models;
using Ecommerce.MVC.Models.Pedidos;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using System.Globalization;

namespace Ecommerce.MVC.Services;

public class PedidoService : IPedidoService
{
    #region Injection
    private readonly DatabaseContext _db;

    public PedidoService(DatabaseContext db)
    {
        _db = db;
    }
    #endregion

    #region Listar pedidos em andamento
    public async Task<IReadOnlyList<PedidosEmAndamentoViewModel>> ListarEmAndamentoAsync(Guid clienteId, CancellationToken ct)
    {
        return await _db.Pedidos
            .AsNoTracking()
            .Where(p => p.ClienteId == clienteId
                        && p.Status != EPedidoStatus.Rascunho
                        && p.Status != EPedidoStatus.Concluido
                        && p.Status != EPedidoStatus.Cancelado)
            .OrderByDescending(p => p.CriadoEmUtc)
            .Select(p => new PedidosEmAndamentoViewModel
            {
                Codigo = p.Codigo,
                CriadoEmUtc = p.CriadoEmUtc,
                Status = p.Status,
                StatusTexto = MapStatusTexto(p.Status),
                Step = MapStatusToStep(p.Status),
                MetodoEntrega = "Retirar na unidade",
                HorarioRetirada = p.HorarioRetirada,
                Observacao = p.Observacao,

                // --- INFORMAÇÕES FINANCEIRAS ---
                Subtotal = p.Subtotal,
                Total = p.Total,
                ValorSinal = Math.Round(p.Total * 0.50m, 2),
                ValorRestanteRetirada = p.Total - Math.Round(p.Total * 0.50m, 2),

                // --- INFORMAÇÕES DE PAGAMENTO FICTÍCIAS ---
                // Aqui simulamos que se o pedido for muito recente, está "Pendente"
                StatusPagamento = (int)p.Status == (int)EPedidoStatus.Confirmado ? "Sinal Pago" : "Aguardando Sinal",
                PixIdentificador = "a9b90c50-3128-4ad0-92f2-64167a511bb7",
                PixBeneficiario = "Barriga Cheia Ltda.",
                PixCopiaCola = "00020126330014BR.GOV.BCB.PIX0111+55999999999952040000530398654054.505802BR5920BROWNIE HOUSE DEMO6009OLINDA-PE62170513PEDIDO-...",
                PixQrCodeUrl = "https://api.qrserver.com/v1/create-qr-code/?size=150x150&data=ExemploPixFicticio",
                PixExpiraEm = p.CriadoEmUtc.AddHours(24).ToLocalTime().ToString("dd/MM/yyyy HH:mm"),

                Itens = p.Itens
                    .Select(i => new PedidosEmAndamentoItemViewModel
                    {
                        ProdutoNome = i.ProdutoNomeSnapshot,
                        Quantidade = i.Quantidade,
                        PrecoBase = i.PrecoBaseSnapshot,
                        TotalLinha = i.TotalLinha,
                        Acompanhamentos = i.Acompanhamentos
                            .Select(a => new PedidosEmAndamentoItemAcompanhamentoViewModel
                            {
                                Nome = a.NomeSnapshot,
                                Preco = a.PrecoSnapshot
                            }).ToList()
                    }).ToList()
            })
            .ToListAsync(ct);
    }

    private static int MapStatusToStep(EPedidoStatus status) => status switch
    {
        EPedidoStatus.AguardandoPagamento => 1,
        EPedidoStatus.Confirmado => 2,
        EPedidoStatus.EmPreparo => 3,
        EPedidoStatus.Pronto => 4,
        EPedidoStatus.Concluido => 5,
        _ => 1
    };

    private static string MapStatusTexto(EPedidoStatus status) => status switch
    {
        EPedidoStatus.AguardandoPagamento => "Aguardando pagamento",
        EPedidoStatus.Confirmado => "Confirmado",
        EPedidoStatus.EmPreparo => "Em preparo",
        EPedidoStatus.Pronto => "Pronto",
        EPedidoStatus.Concluido => "Concluído",
        EPedidoStatus.Cancelado => "Cancelado",
        _ => "Pedido criado"
    };
    #endregion

    #region Obter dados para finalização do pedido
    public async Task<PedidoModalFinalizarViewModel> ObterDadosFinalizacaoPedidoAsync(HttpContext http, Guid clienteId, CancellationToken ct)
    {
        var cliente = await _db.Set<Cliente>().AsNoTracking().FirstOrDefaultAsync(c => c.Id == clienteId, ct);

        if (cliente == null)
            throw new InvalidOperationException("Cliente não encontrado.");

        var token = CartTokenHelper.GetOrCreateToken(http);
        var carrinho = await _db.Carrinhos.AsNoTracking().Include(c => c.Itens).ThenInclude(i => i.Acompanhamentos).FirstOrDefaultAsync(c => c.UserId == cliente.Id, ct);

        if (carrinho == null)
            throw new InvalidOperationException("Carrinho não encontrado.");

        var itensVm = new List<PedidoCarrinhoItemResumoViewModel>();
        decimal subtotal = 0m;

        foreach (var item in carrinho.Itens)
        {
            var somaAcomp = item.Acompanhamentos?.Sum(a => a.PrecoSnapshot) ?? 0m;
            var totalLinha = (item.PrecoBaseSnapshot + somaAcomp) * item.Quantidade;

            subtotal += totalLinha;

            itensVm.Add(new PedidoCarrinhoItemResumoViewModel
            {
                ProdutoId = item.ProdutoId,
                ProdutoNome = item.ProdutoNomeSnapshot,
                Quantidade = item.Quantidade,
                PrecoBase = item.PrecoBaseSnapshot,
                PrecoAcompanhamentos = somaAcomp,
                TotalLinha = totalLinha,
                Acompanhamentos = item.Acompanhamentos?
                .Select(a => new PedidoAcompanhamentoDetalheViewModel
                {
                    Nome = a.NomeSnapshot,
                    Preco = a.PrecoSnapshot
                })
                .ToList() ?? new List<PedidoAcompanhamentoDetalheViewModel>()
            });
        }

        var tempoMaximoMinutos = carrinho.Itens.Any() ? carrinho.Itens.Max(i => i.TempoPreparoMinutosSnapshot) : 0;

        var horariosRetirada = GerarHorariosRetirada(tempoMaximoMinutos);

        var taxaEntrega = 0m;
        var total = subtotal + taxaEntrega;
        var valorSinal = Math.Round(total * 0.50m, 2, MidpointRounding.AwayFromZero);
        var valorRestante = total - valorSinal;

        return new PedidoModalFinalizarViewModel
        {
            ClienteId = cliente.Id,
            ClienteNome = cliente.Nome,
            ClienteTelefone = cliente.Telefone,

            Carrinho = new PedidoCarrinhoResumoViewModel
            {
                CarrinhoId = carrinho.Id,
                ItensCount = carrinho.Itens.Count,
                Subtotal = subtotal,
                TaxaEntrega = taxaEntrega,
                Total = total,
                ValorRestanteRetirada = valorRestante,
                ValorSinal = valorSinal,
                Itens = itensVm
            },

            HorariosRetirada = horariosRetirada.Select(h => new PedidoRetiradaHorarioViewModel
            {
                DataHora = h
            }).ToList()
        };
    }

    private static List<DateTime> GerarHorariosRetirada(int tempoPreparoMinutos, int diasDisponiveis = 30)
    {
        var resultado = new List<DateTime>();
        var agora = DateTime.Today.AddHours(22);

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

    private static DateTime CalcularHorarioDisponivel(DateTime dataPedido, int tempoPreparoMinutos)
    {
        var abertura = new TimeSpan(8, 0, 0);
        var fechamento = new TimeSpan(22, 0, 0);

        DateTime atual = dataPedido;

        if (atual.TimeOfDay >= fechamento)
        {
            atual = atual.Date.AddDays(1).Add(abertura);
        }
        else if (atual.TimeOfDay < abertura)
        {
            atual = atual.Date.Add(abertura);
        }

        int minutosRestantes = tempoPreparoMinutos;

        while (minutosRestantes > 0)
        {
            var fimDoDia = atual.Date.Add(fechamento);
            var minutosDisponiveisHoje = (int)(fimDoDia - atual).TotalMinutes;

            if (minutosRestantes <= minutosDisponiveisHoje)
            {
                atual = atual.AddMinutes(minutosRestantes);
                minutosRestantes = 0;
            }
            else
            {
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

    #endregion

    #region Confirmar criação do pedido
    public async Task<ConfirmarPedidoResponse> ConfirmarAsync(HttpContext http, Guid clienteId, ConfirmarPedidoRequest req, CancellationToken ct)
    {
        if (req == null)
            throw new ArgumentException("Requisição inválida.");

        var token = CartTokenHelper.GetOrCreateToken(http);

        var carrinho = await _db.Carrinhos
            .Include(c => c.Itens)
                .ThenInclude(i => i.Acompanhamentos)
            .FirstOrDefaultAsync(c => c.UserId == clienteId, ct);

        if (carrinho == null || carrinho.Itens.Count == 0)
            throw new InvalidOperationException("Carrinho vazio.");

        decimal subtotal = 0m;

        foreach (var item in carrinho.Itens)
        {
            var somaAcomp = item.Acompanhamentos?.Sum(a => a.PrecoSnapshot) ?? 0m;
            subtotal += (item.PrecoBaseSnapshot + somaAcomp) * item.Quantidade;
        }

        var total = subtotal;

        var horarioUtc = DateTime.SpecifyKind(req.HorarioRetirada, DateTimeKind.Utc);

        var pedido = new Pedido
        {
            ClienteId = clienteId,
            Codigo = await GerarCodigoPedidoAsync(ct),
            MetodoEntrega = "Retirada",
            Pagamento = "Pix",
            Observacao = string.IsNullOrWhiteSpace(req.Observacao) ? null : req.Observacao.Trim(),
            Subtotal = subtotal,
            TaxaEntrega = 0m,
            Total = total,
            CriadoEmUtc = DateTime.UtcNow,
            Status = Enums.EPedidoStatus.AguardandoPagamento,
            HorarioRetirada = horarioUtc
        };

        foreach (var ci in carrinho.Itens)
        {
            var somaAcomp = ci.Acompanhamentos?.Sum(a => a.PrecoSnapshot) ?? 0m;
            var totalLinha = (ci.PrecoBaseSnapshot + somaAcomp) * ci.Quantidade;

            var pedidoItem = new PedidoItem
            {
                ProdutoId = ci.ProdutoId,
                ProdutoNomeSnapshot = ci.ProdutoNomeSnapshot,
                PrecoBaseSnapshot = ci.PrecoBaseSnapshot,
                Quantidade = ci.Quantidade,
                PrecoAcompanhamentosSnapshot = somaAcomp,
                TotalLinha = totalLinha
            };

            if (ci.Acompanhamentos?.Any() == true)
            {
                foreach (var a in ci.Acompanhamentos)
                {
                    pedidoItem.Acompanhamentos.Add(new PedidoItemAcompanhamento
                    {
                        AcompanhamentoId = a.AcompanhamentoId,
                        CategoriaId = a.CategoriaId,
                        NomeSnapshot = a.NomeSnapshot,
                        PrecoSnapshot = a.PrecoSnapshot
                    });
                }
            }

            pedido.Itens.Add(pedidoItem);
        }

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        _db.Pedidos.Add(pedido);

        var itensIds = carrinho.Itens.Select(i => i.Id).ToList();

        if (itensIds.Count > 0)
        {
            var acomps = await _db.Set<CarrinhoItemAcompanhamento>()
                .Where(a => itensIds.Contains(a.CarrinhoItemId))
                .ToListAsync(ct);

            _db.Set<CarrinhoItemAcompanhamento>().RemoveRange(acomps);
        }

        _db.Set<CarrinhoItem>().RemoveRange(carrinho.Itens);
        _db.Carrinhos.Remove(carrinho);

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        CartTokenHelper.ClearToken(http);
        var valorSinal = Math.Round(pedido.Total * 0.50m, 2, MidpointRounding.AwayFromZero);

        // PIX fake
        var expiraEmUtc = DateTime.UtcNow.AddMinutes(1440);
        var pixChave = "pix-demo@browniehouse.com";
        var pixCopiaCola = BuildPixCopiaColaFake(pedido.Id, valorSinal);
        var qrBase64 = GenerateQrPngBase64(pixCopiaCola);

        return new ConfirmarPedidoResponse
        {
            Ok = true,
            PedidoId = pedido.Id,
            Valor = valorSinal,
            PixChave = pixChave,
            PixCopiaCola = pixCopiaCola,
            QrCodeBase64 = qrBase64,
            ExpiraEmUtc = expiraEmUtc,
            Codigo = pedido.Codigo
        };
    }

    private static string BuildPixCopiaColaFake(Guid pedidoId, decimal valorSinal)
    {
        // Isso NÃO é um payload PIX válido; é um placeholder para a UI.
        // Depois você troca pelo payload real do seu PSP (MercadoPago, Gerencianet, etc).
        var valor = valorSinal.ToString("0.00", CultureInfo.InvariantCulture);
        return $"00020126330014BR.GOV.BCB.PIX0111+5599999999995204000053039865405{valor}5802BR5920BROWNIE HOUSE DEMO6009OLINDA-PE62170513PEDIDO-{pedidoId:N}6304ABCD";
    }

    private static string GenerateQrPngBase64(string payload)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(qrData);
        byte[] pngBytes = qrCode.GetGraphic(20); // tamanho/qualidade
        return Convert.ToBase64String(pngBytes);
    }

    private async Task<string> GerarCodigoPedidoAsync(CancellationToken ct)
    {
        var ano = DateTime.UtcNow.Year;

        var seq = await _db.PedidoSequenciais
            .SingleOrDefaultAsync(x => x.Ano == ano, ct);

        if (seq == null)
        {
            seq = new PedidoSequencial
            {
                Ano = ano,
                UltimoNumero = 0
            };
            _db.Add(seq);
        }

        seq.UltimoNumero++;
        await _db.SaveChangesAsync(ct);

        // BH = Brownie House (ajuste o prefixo)
        return $"BC-{ano}-{seq.UltimoNumero:D6}";
    }

    #endregion

    #region Obter quantidade de Pedidos em andamento
    public async Task<int> ObterQuantidadeEmAndamentoAsync(Guid clienteId, CancellationToken ct)
    {
        // Consideramos "em aberto" os mesmos status que você usa na listagem
        return await _db.Pedidos
            .AsNoTracking()
            .CountAsync(p => p.ClienteId == clienteId
                             && p.Status != EPedidoStatus.Rascunho
                             && p.Status != EPedidoStatus.Concluido
                             && p.Status != EPedidoStatus.Cancelado, ct);
    }
    #endregion
}
