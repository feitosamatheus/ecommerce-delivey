using ClosedXML.Excel;
using Ecommerce.MVC.Areas.Admin.Services;
using Ecommerce.MVC.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Reflection.Metadata;

namespace Ecommerce.MVC.Services
{
    public class PedidoExportService : IPedidoExportService
    {
        private readonly CultureInfo _pt = new("pt-BR");

        public byte[] GerarPdf(Pedido pedido)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var documento = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(20);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(10));
                    page.Header().Element(c => MontarCabecalhoPdf(c, pedido));
                    page.Content().Element(c => MontarConteudoPdf(c, pedido));
                    page.Footer().AlignCenter().Text(txt =>
                    {
                        txt.Span("Gerado em ");
                        txt.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
                    });
                });
            });

            return documento.GeneratePdf();
        }

        public byte[] GerarExcel(Pedido pedido)
        {
            using var workbook = new XLWorkbook();

            var wsResumo = workbook.Worksheets.Add("Resumo");
            var wsPagamentos = workbook.Worksheets.Add("Pagamentos");
            var wsItens = workbook.Worksheets.Add("Itens");

            MontarAbaResumo(wsResumo, pedido);
            MontarAbaPagamentos(wsPagamentos, pedido);
            MontarAbaItens(wsItens, pedido);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private void MontarCabecalhoPdf(QuestPDF.Infrastructure.IContainer container, Pedido pedido)
        {
            container.Column(col =>
            {
                col.Item().Text($"Pedido #{pedido.Codigo}")
                    .FontSize(18)
                    .Bold()
                    .FontColor(Colors.Blue.Medium);

                col.Item().Text($"Criado em: {pedido.CriadoEmUtc.ToLocalTime():dd/MM/yyyy HH:mm}")
                    .FontSize(10)
                    .FontColor(Colors.Grey.Darken1);

                col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            });
        }

        private void MontarConteudoPdf(QuestPDF.Infrastructure.IContainer container, Pedido pedido)
        {
            container.Column(col =>
            {
                col.Spacing(14);

                col.Item().Element(c => MontarResumoPdf(c, pedido));
                col.Item().Element(c => MontarClienteEntregaPdf(c, pedido));
                col.Item().Element(c => MontarFinanceiroPdf(c, pedido));
                col.Item().Element(c => MontarPagamentosPdf(c, pedido));
                col.Item().Element(c => MontarItensPdf(c, pedido));
            });
        }

        private void MontarResumoPdf(QuestPDF.Infrastructure.IContainer container, Pedido pedido)
        {
            container.Column(col =>
            {
                col.Item().Text("Resumo").Bold().FontSize(12);

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        HeaderCell(header, "Status do pedido");
                        HeaderCell(header, "Situação financeira");
                        HeaderCell(header, "Total");
                        HeaderCell(header, "Pago");
                        HeaderCell(header, "Em aberto");
                    });

                    BodyCell(table, pedido.Status.ToString());
                    BodyCell(table, GetFinanceiroStatusTexto(pedido));
                    BodyCell(table, pedido.Total.ToString("C", _pt));
                    BodyCell(table, pedido.ValorPago.ToString("C", _pt));
                    BodyCell(table, pedido.ValorEmAberto.ToString("C", _pt));
                });
            });
        }

        private void MontarClienteEntregaPdf(QuestPDF.Infrastructure.IContainer container, Pedido pedido)
        {
            container.Column(col =>
            {
                col.Item().Text("Cliente e Entrega").Bold().FontSize(12);

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(150);
                        columns.RelativeColumn();
                    });

                    AdicionarLinhaChaveValor(table, "Cliente", pedido.Cliente?.Nome);
                    AdicionarLinhaChaveValor(table, "Data do pedido", pedido.CriadoEmUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm"));
                    AdicionarLinhaChaveValor(table, "Horário retirada", pedido.HorarioRetirada.ToLocalTime().ToString("dd/MM/yyyy HH:mm"));
                    AdicionarLinhaChaveValor(table, "Método entrega", pedido.MetodoEntrega?.ToString());
                    AdicionarLinhaChaveValor(table, "Forma informada", pedido.Pagamento?.ToString());
                    AdicionarLinhaChaveValor(table, "Observação", string.IsNullOrWhiteSpace(pedido.Observacao) ? "-" : pedido.Observacao);
                });
            });
        }

        private void MontarFinanceiroPdf(QuestPDF.Infrastructure.IContainer container, Pedido pedido)
        {
            container.Column(col =>
            {
                col.Item().Text("Resumo Financeiro").Bold().FontSize(12);

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(180);
                        columns.RelativeColumn();
                    });

                    AdicionarLinhaChaveValor(table, "Subtotal", pedido.Subtotal.ToString("C", _pt));
                    AdicionarLinhaChaveValor(table, "Taxa de entrega", pedido.TaxaEntrega.ToString("C", _pt));
                    AdicionarLinhaChaveValor(table, "Total do pedido", pedido.Total.ToString("C", _pt));
                    AdicionarLinhaChaveValor(table, "Valor de entrada (50%)", pedido.ValorEntrada.ToString("C", _pt));
                    AdicionarLinhaChaveValor(table, "Valor pago", pedido.ValorPago.ToString("C", _pt));
                    AdicionarLinhaChaveValor(table, "Valor em aberto", pedido.ValorEmAberto.ToString("C", _pt));
                    AdicionarLinhaChaveValor(table, "Sinal pago", pedido.SinalPago ? "Sim" : "Não");
                    AdicionarLinhaChaveValor(table, "Pedido quitado", pedido.PedidoQuitado ? "Sim" : "Não");
                });
            });
        }

        private void MontarPagamentosPdf(QuestPDF.Infrastructure.IContainer container, Pedido pedido)
        {
            container.Column(col =>
            {
                col.Item().Text("Pagamentos do Pedido").Bold().FontSize(12);

                if (pedido.Pagamentos == null || !pedido.Pagamentos.Any())
                {
                    col.Item().Text("Nenhum pagamento/cobrança registrado para este pedido.");
                    return;
                }

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(25);
                        columns.RelativeColumn(1.2f);
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.ConstantColumn(70);
                        columns.ConstantColumn(80);
                        columns.ConstantColumn(80);
                    });

                    table.Header(header =>
                    {
                        HeaderCell(header, "#");
                        HeaderCell(header, "Tipo cobrança");
                        HeaderCell(header, "Forma");
                        HeaderCell(header, "Gateway");
                        HeaderCell(header, "Status");
                        HeaderCell(header, "Valor");
                        HeaderCell(header, "Criado em");
                        HeaderCell(header, "Pago em");
                    });

                    foreach (var pagamento in pedido.Pagamentos.OrderBy(p => p.Sequencia).ThenBy(p => p.CriadoEmUtc))
                    {
                        BodyCell(table, pagamento.Sequencia.ToString());
                        BodyCell(table, pagamento.TipoCobranca.ToString());
                        BodyCell(table, pagamento.TipoPagamento?.ToString());
                        BodyCell(table, pagamento.Gateway?.ToString());
                        BodyCell(table, pagamento.Status.ToString());
                        BodyCell(table, pagamento.Valor.ToString("C", _pt));
                        BodyCell(table, pagamento.CriadoEmUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm"));
                        BodyCell(table, pagamento.PagoEmUtc?.ToLocalTime().ToString("dd/MM/yyyy HH:mm") ?? "-");
                    }
                });
            });
        }

        private void MontarItensPdf(QuestPDF.Infrastructure.IContainer container, Pedido pedido)
        {
            container.Column(col =>
            {
                col.Item().Text("Itens do Pedido").Bold().FontSize(12);

                if (pedido.Itens == null || !pedido.Itens.Any())
                {
                    col.Item().Text("Nenhum item encontrado.");
                    return;
                }

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1.6f);
                        columns.RelativeColumn(2f);
                        columns.ConstantColumn(40);
                        columns.ConstantColumn(75);
                        columns.ConstantColumn(75);
                        columns.ConstantColumn(75);
                    });

                    table.Header(header =>
                    {
                        HeaderCell(header, "Produto");
                        HeaderCell(header, "Acompanhamentos");
                        HeaderCell(header, "Qtd");
                        HeaderCell(header, "Preço base");
                        HeaderCell(header, "Adicionais");
                        HeaderCell(header, "Valor linha");
                    });

                    foreach (var item in pedido.Itens)
                    {
                        var acompanhamentos = item.Acompanhamentos != null && item.Acompanhamentos.Any()
                            ? string.Join(", ", item.Acompanhamentos.Select(a =>
                                a.PrecoSnapshot > 0
                                    ? $"{a.NomeSnapshot} (+{a.PrecoSnapshot.ToString("C", _pt)})"
                                    : a.NomeSnapshot))
                            : "Sem acompanhamentos";

                        BodyCell(table, item.ProdutoNomeSnapshot);
                        BodyCell(table, acompanhamentos);
                        BodyCell(table, item.Quantidade.ToString());
                        BodyCell(table, item.PrecoBaseSnapshot.ToString("C", _pt));
                        BodyCell(table, item.PrecoAcompanhamentosSnapshot.ToString("C", _pt));
                        BodyCell(table, item.TotalLinha.ToString("C", _pt));
                    }
                });

                col.Item().PaddingTop(10).AlignRight().Text($"Total do pedido: {pedido.Total.ToString("C", _pt)}")
                    .Bold()
                    .FontSize(12);
            });
        }

        private void HeaderCell(TableCellDescriptor header, string texto)
        {
            header.Cell().Element(CellStyleHeader).Text(texto).Bold();
        }

        private void BodyCell(TableDescriptor table, string? texto)
        {
            table.Cell().Element(CellStyleBody).Text(texto ?? "-");
        }

        private void AdicionarLinhaChaveValor(TableDescriptor table, string chave, string? valor)
        {
            table.Cell().Element(CellStyleBody).Text(chave).Bold();
            table.Cell().Element(CellStyleBody).Text(string.IsNullOrWhiteSpace(valor) ? "-" : valor);
        }

        private static QuestPDF.Infrastructure.IContainer CellStyleHeader(QuestPDF.Infrastructure.IContainer container)
        {
            return container
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Background(Colors.Grey.Lighten4)
                .Padding(5);
        }

        private static QuestPDF.Infrastructure.IContainer CellStyleBody(QuestPDF.Infrastructure.IContainer container)
        {
            return container
                .Border(1)
                .BorderColor(Colors.Grey.Lighten3)
                .Padding(5);
        }

        private void MontarAbaResumo(IXLWorksheet ws, Pedido pedido)
        {
            ws.Cell("A1").Value = "Pedido";
            ws.Cell("B1").Value = pedido.Codigo;

            ws.Cell("A2").Value = "Criado em";
            ws.Cell("B2").Value = pedido.CriadoEmUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm");

            ws.Cell("A4").Value = "Status do pedido";
            ws.Cell("B4").Value = pedido.Status.ToString();

            ws.Cell("A5").Value = "Situação financeira";
            ws.Cell("B5").Value = GetFinanceiroStatusTexto(pedido);

            ws.Cell("A6").Value = "Cliente";
            ws.Cell("B6").Value = pedido.Cliente?.Nome ?? "-";

            ws.Cell("A7").Value = "Horário retirada";
            ws.Cell("B7").Value = pedido.HorarioRetirada.ToLocalTime().ToString("dd/MM/yyyy HH:mm");

            ws.Cell("A8").Value = "Método entrega";
            ws.Cell("B8").Value = pedido.MetodoEntrega?.ToString() ?? "-";

            ws.Cell("A9").Value = "Forma informada";
            ws.Cell("B9").Value = pedido.Pagamento?.ToString() ?? "-";

            ws.Cell("A10").Value = "Observação";
            ws.Cell("B10").Value = string.IsNullOrWhiteSpace(pedido.Observacao) ? "-" : pedido.Observacao;

            ws.Cell("A12").Value = "Subtotal";
            ws.Cell("B12").Value = pedido.Subtotal;

            ws.Cell("A13").Value = "Taxa de entrega";
            ws.Cell("B13").Value = pedido.TaxaEntrega;

            ws.Cell("A14").Value = "Total";
            ws.Cell("B14").Value = pedido.Total;

            ws.Cell("A15").Value = "Valor de entrada";
            ws.Cell("B15").Value = pedido.ValorEntrada;

            ws.Cell("A16").Value = "Valor pago";
            ws.Cell("B16").Value = pedido.ValorPago;

            ws.Cell("A17").Value = "Valor em aberto";
            ws.Cell("B17").Value = pedido.ValorEmAberto;

            ws.Cell("A18").Value = "Sinal pago";
            ws.Cell("B18").Value = pedido.SinalPago ? "Sim" : "Não";

            ws.Cell("A19").Value = "Pedido quitado";
            ws.Cell("B19").Value = pedido.PedidoQuitado ? "Sim" : "Não";

            ws.Range("A1:B19").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Range("A1:B19").Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            ws.Column("A").Width = 24;
            ws.Column("B").Width = 40;

            ws.Range("B12:B17").Style.NumberFormat.Format = "R$ #,##0.00";
            ws.Range("A1:B1").Style.Font.Bold = true;
        }

        private void MontarAbaPagamentos(IXLWorksheet ws, Pedido pedido)
        {
            ws.Cell(1, 1).Value = "#";
            ws.Cell(1, 2).Value = "Tipo cobrança";
            ws.Cell(1, 3).Value = "Forma";
            ws.Cell(1, 4).Value = "Gateway";
            ws.Cell(1, 5).Value = "Status";
            ws.Cell(1, 6).Value = "Valor";
            ws.Cell(1, 7).Value = "Criado em";
            ws.Cell(1, 8).Value = "Pago em";
            ws.Cell(1, 9).Value = "Expiração PIX";
            ws.Cell(1, 10).Value = "Fatura";

            var linha = 2;

            if (pedido.Pagamentos != null)
            {
                foreach (var pagamento in pedido.Pagamentos.OrderBy(p => p.Sequencia).ThenBy(p => p.CriadoEmUtc))
                {
                    ws.Cell(linha, 1).Value = pagamento.Sequencia;
                    ws.Cell(linha, 2).Value = pagamento.TipoCobranca.ToString() ?? "-";
                    ws.Cell(linha, 3).Value = pagamento.TipoPagamento?.ToString() ?? "-";
                    ws.Cell(linha, 4).Value = pagamento.Gateway?.ToString() ?? "-";
                    ws.Cell(linha, 5).Value = pagamento.Status.ToString();
                    ws.Cell(linha, 6).Value = pagamento.Valor;
                    ws.Cell(linha, 7).Value = pagamento.CriadoEmUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
                    ws.Cell(linha, 8).Value = pagamento.PagoEmUtc?.ToLocalTime().ToString("dd/MM/yyyy HH:mm") ?? "-";
                    ws.Cell(linha, 9).Value = pagamento.PixExpirationDate?.ToLocalTime().ToString("dd/MM/yyyy HH:mm") ?? "-";
                    ws.Cell(linha, 10).Value = pagamento.InvoiceUrl ?? "-";
                    linha++;
                }
            }

            ws.RangeUsed()!.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.RangeUsed()!.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            ws.Row(1).Style.Font.Bold = true;
            ws.Column(6).Style.NumberFormat.Format = "R$ #,##0.00";
            ws.Columns().AdjustToContents();
        }

        private void MontarAbaItens(IXLWorksheet ws, Pedido pedido)
        {
            ws.Cell(1, 1).Value = "Produto";
            ws.Cell(1, 2).Value = "Acompanhamentos";
            ws.Cell(1, 3).Value = "Qtd";
            ws.Cell(1, 4).Value = "Preço base";
            ws.Cell(1, 5).Value = "Adicionais";
            ws.Cell(1, 6).Value = "Valor linha";

            var linha = 2;

            if (pedido.Itens != null)
            {
                foreach (var item in pedido.Itens)
                {
                    var acompanhamentos = item.Acompanhamentos != null && item.Acompanhamentos.Any()
                        ? string.Join(", ", item.Acompanhamentos.Select(a =>
                            a.PrecoSnapshot > 0
                                ? $"{a.NomeSnapshot} (+{a.PrecoSnapshot.ToString("C", _pt)})"
                                : a.NomeSnapshot))
                        : "Sem acompanhamentos";

                    ws.Cell(linha, 1).Value = item.ProdutoNomeSnapshot;
                    ws.Cell(linha, 2).Value = acompanhamentos;
                    ws.Cell(linha, 3).Value = item.Quantidade;
                    ws.Cell(linha, 4).Value = item.PrecoBaseSnapshot;
                    ws.Cell(linha, 5).Value = item.PrecoAcompanhamentosSnapshot;
                    ws.Cell(linha, 6).Value = item.TotalLinha;
                    linha++;
                }
            }

            ws.Cell(linha + 1, 5).Value = "TOTAL";
            ws.Cell(linha + 1, 6).Value = pedido.Total;

            ws.RangeUsed()!.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.RangeUsed()!.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            ws.Row(1).Style.Font.Bold = true;
            ws.Cell(linha + 1, 5).Style.Font.Bold = true;
            ws.Cell(linha + 1, 6).Style.Font.Bold = true;

            ws.Columns(4, 6).Style.NumberFormat.Format = "R$ #,##0.00";
            ws.Columns().AdjustToContents();
        }

        private string GetFinanceiroStatusTexto(Pedido pedido)
        {
            if (pedido.PedidoQuitado)
                return "Quitado";

            if (pedido.ValorPago > 0)
                return "Pagamento parcial";

            return "Em aberto";
        }
    }
}