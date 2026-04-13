using Ecommerce.Infrastructure.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Ecommerce.Infrastructure.Entities;

public class Pedido
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Codigo { get; set; }

    public Guid ClienteId { get; set; }
    public Cliente Cliente { get; set; }

    public string MetodoEntrega { get; set; }
    public string Pagamento { get; set; }
    public string Observacao { get; set; }
    public DateTime HorarioRetirada { get; set; }

    public decimal Subtotal { get; set; }
    public decimal TaxaEntrega { get; set; }
    public decimal Total { get; set; }



    public EPedidoStatus Status { get; set; }
    public DateTime CriadoEmUtc { get; set; } = DateTime.UtcNow;
    public DateTime? EntregueEmUtc { get; set; }
    public DateTime? ConcluidoEmUtc { get; set; }
    public DateTime? ProntoEmUtc { get; set; }
    public DateTime? EmpreparoEmUtc { get; set; }

    public List<PedidoItem> Itens { get; set; } = new();
    public List<PedidoPagamento> Pagamentos { get; set; } = new();

    [NotMapped]
    public decimal ValorEntrada => Math.Round(Total * 0.5m, 2);

   [NotMapped]
    public bool SinalPago =>
        Pagamentos.Any(p =>
        (
            p.TipoCobranca == ETipoCobrancaPedido.Sinal ||
            p.TipoCobranca == ETipoCobrancaPedido.Saldo
        ) &&
        (
            p.Status == EStatusPagamento.Received ||
            p.Status == EStatusPagamento.Confirmed ||
            p.Status == EStatusPagamento.ReceivedInCash
        ));

    [NotMapped]
    public bool SaldoPago =>
        Pagamentos.Any(p =>
        p.TipoCobranca == ETipoCobrancaPedido.Saldo &&
        (
            p.Status == EStatusPagamento.Received ||
            p.Status == EStatusPagamento.Confirmed ||
            p.Status == EStatusPagamento.ReceivedInCash
        ));

    [NotMapped]
    public decimal ValorPago =>
        Pagamentos
            .Where(p =>
                p.Status == EStatusPagamento.Received ||
                p.Status == EStatusPagamento.Confirmed ||
                p.Status == EStatusPagamento.ReceivedInCash)
            .Sum(p => p.Valor);

    [NotMapped]
    public decimal ValorEmAberto => Total - ValorPago;

    [NotMapped]
    public bool PedidoQuitado => ValorEmAberto <= 0;
}