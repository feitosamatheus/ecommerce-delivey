using Ecommerce.MVC.Config;
using Ecommerce.MVC.Entities;
using Ecommerce.MVC.Enums;
using Ecommerce.MVC.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ecommerce.MVC.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PagamentoController : Controller
{
    private readonly HttpClient _httpClient;
    private readonly DatabaseContext _context;
    private readonly IHubContext<PagamentoHub> _hubContext;

    public PagamentoController(IHttpClientFactory httpClientFactory, DatabaseContext context, IHubContext<PagamentoHub> hubContext)
    {
        _httpClient = httpClientFactory.CreateClient("Asaas");
        _context = context;
        _hubContext = hubContext;
    }

    [HttpPost("criar-cliente-cobranca-pix")]
    public async Task<IActionResult> CriarClienteECobrancaPix([FromBody] PagamentoViewModel model)
    {
        try
        {
            if (model == null || model.PedidoId == Guid.Empty)
            {
                return Json(new
                {
                    sucesso = false,
                    mensagem = "Dados inválidos."
                });
            } 

            var clienteIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(clienteIdClaim) || !Guid.TryParse(clienteIdClaim, out var clienteId))
            {
                Response.StatusCode = 401;
                return Json(new
                {
                    sucesso = false,
                    mensagem = "Usuário não autenticado."
                });
            }

            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.Id == clienteId);

            if (cliente == null)
            {
                Response.StatusCode = 404;
                return Json(new
                {
                    sucesso = false,
                    mensagem = "Cliente não encontrado."
                });
            }

            var pedido = await _context.Pedidos
                .Include(p => p.Pagamentos)
                .FirstOrDefaultAsync(p => p.Id == model.PedidoId && p.ClienteId == clienteId);

            if (pedido == null)
            {
                Response.StatusCode = 404;
                return Json(new
                {
                    sucesso = false,
                    mensagem = "Pedido não encontrado."
                });
            }

            var tipoCobranca = model.TipoCobranca; 

            if (tipoCobranca != ETipoCobrancaPedido.Sinal && tipoCobranca != ETipoCobrancaPedido.Saldo)
            {
                return Json(new
                {
                    sucesso = false,
                    mensagem = "Tipo de cobrança inválido para este fluxo."
                });
            }

            var valorCobranca = tipoCobranca switch
            {
                ETipoCobrancaPedido.Sinal => pedido.ValorEntrada,
                ETipoCobrancaPedido.Saldo => pedido.Total,
                _ => pedido.ValorEntrada
            };

            if (valorCobranca < 5m)
            {
                return Json(new
                {
                    sucesso = false,
                    mensagem = "O valor mínimo para gerar cobrança PIX é de R$ 5,00."
                });
            }

            var pagamentoExistente = pedido.Pagamentos
                .OrderBy(p => p.Sequencia)
                .FirstOrDefault(p =>
                    p.TipoCobranca == tipoCobranca &&
                    !string.IsNullOrWhiteSpace(p.GatewayPaymentId) &&
                    (p.Status == EStatusPagamento.Pending
                    || p.Status == EStatusPagamento.AwaitingRiskAnalysis
                    || p.Status == EStatusPagamento.DunningRequested
                    || p.Status == EStatusPagamento.Overdue
                    || p.Status == EStatusPagamento.Received
                    || p.Status == EStatusPagamento.Confirmed
                    || p.Status == EStatusPagamento.ReceivedInCash));

            if (pagamentoExistente != null)
            {
                return Json(new
                {
                    sucesso = true,
                    pedidoPagamentoExistente = true,
                    customerId = pagamentoExistente.GatewayCustomerId,
                    payment = new
                    {
                        id = pagamentoExistente.GatewayPaymentId,
                        status = pagamentoExistente.Status.ToString(),
                        value = pagamentoExistente.Valor,
                        dueDate = pagamentoExistente.PixExpirationDate,
                        invoiceUrl = pagamentoExistente.InvoiceUrl,
                        billingType = pagamentoExistente.TipoPagamento,
                        tipoCobranca = pagamentoExistente.TipoCobranca.ToString(),
                        sequencia = pagamentoExistente.Sequencia
                    },
                    resumoPedido = new
                    {
                        totalPedido = pedido.Total,
                        valorSinal = pedido.ValorEntrada,
                        valorRestanteRetirada = tipoCobranca == ETipoCobrancaPedido.Saldo
                            ? 0
                            : (pedido.Total - pedido.ValorEntrada),
                        tipoCobranca = tipoCobranca.ToString(),
                        tituloPagamento = tipoCobranca == ETipoCobrancaPedido.Sinal
                            ? "Pagamento do Sinal (50%)"
                            : "Pagamento"
                    },
                    pixQrCode = new
                    {
                        encodedImage = pagamentoExistente.PixEncodedImage,
                        payload = pagamentoExistente.PixPayload,
                        expirationDate = pagamentoExistente.PixExpirationDate,
                        description = "QR Code Pix já gerado para este pedido."
                    }
                });
            }

            string customerId = cliente.IdClientePagamento;

            if (string.IsNullOrWhiteSpace(customerId))
            {
                customerId = await CriarCliente(new CriarClienteAsaasRequest
                {
                    Name = cliente.Nome,
                    CpfCnpj = cliente.CPF
                });

                if (string.IsNullOrWhiteSpace(customerId))
                {
                    return Json(new
                    {
                        sucesso = false,
                        mensagem = "Não foi possível criar o cliente no Asaas."
                    });
                }

                cliente.IdClientePagamento = customerId;
                await _context.SaveChangesAsync();
            }

            var pagamentoAsaas = await CriarCobrancaPix(
                customerId,
                valorCobranca,
                DateTime.UtcNow.AddHours(24),
                ""
            );

            if (pagamentoAsaas == null || string.IsNullOrWhiteSpace(pagamentoAsaas.Id))
            {
                return Json(new
                {
                    sucesso = false,
                    mensagem = "Não foi possível gerar a cobrança Pix.",
                    customerId = customerId
                });
            }

            var qrCode = await ObterQrCodePix(pagamentoAsaas.Id);

            if (qrCode == null)
            {
                return Json(new
                {
                    sucesso = false,
                    mensagem = "Cobrança criada, mas não foi possível obter o QR Code Pix.",
                    customerId = customerId,
                    paymentId = pagamentoAsaas.Id,
                    payment = pagamentoAsaas
                });
            }

            DateTime? expiracaoPixUtc = null;

            if (!string.IsNullOrWhiteSpace(qrCode.ExpirationDate) &&
                DateTimeOffset.TryParse(qrCode.ExpirationDate, out var dto))
            {
                expiracaoPixUtc = dto.UtcDateTime;
            }

            var proximaSequencia = pedido.Pagamentos.Any()
                ? pedido.Pagamentos.Max(p => p.Sequencia) + 1
                : 1;

            var pedidoPagamento = new PedidoPagamento
            {
                PedidoId = pedido.Id,
                Gateway = "ASAAS",
                TipoPagamento = "PIX",
                GatewayCustomerId = customerId,
                GatewayPaymentId = pagamentoAsaas.Id,
                Valor = pagamentoAsaas.Value,
                Status = MapearStatusAsaas(pagamentoAsaas.Status),
                TipoCobranca = tipoCobranca,
                Sequencia = proximaSequencia,
                PixPayload = qrCode.Payload,
                PixEncodedImage = qrCode.EncodedImage,
                PixExpirationDate = expiracaoPixUtc,
                InvoiceUrl = pagamentoAsaas.InvoiceUrl,
                CriadoEmUtc = DateTime.UtcNow
            };

            _context.PedidoPagamentos.Add(pedidoPagamento);
            await _context.SaveChangesAsync();

            var expiracaoVisualUtc = DateTime.UtcNow.AddHours(24).ToString("o");

            return Json(new
            {
                sucesso = true,
                pedidoPagamentoExistente = false,
                customerId = customerId,
                payment = new
                {
                    id = pedidoPagamento.GatewayPaymentId,
                    status = pagamentoAsaas.Status,
                    value = pedidoPagamento.Valor,
                    dueDate = pedidoPagamento.PixExpirationDate,
                    invoiceUrl = pedidoPagamento.InvoiceUrl,
                    billingType = pedidoPagamento.TipoPagamento,
                    tipoCobranca = pedidoPagamento.TipoCobranca.ToString(),
                    sequencia = pedidoPagamento.Sequencia
                },
                resumoPedido = new
                {
                    totalPedido = pedido.Total,
                    valorSinal = pedido.ValorEntrada,
                    valorRestanteRetirada = tipoCobranca == ETipoCobrancaPedido.Saldo
                        ? 0
                        : (pedido.Total - pedido.ValorEntrada),
                    tipoCobranca = tipoCobranca.ToString(),
                    tituloPagamento = tipoCobranca == ETipoCobrancaPedido.Sinal
                        ? "Pagamento do Sinal (50%)"
                        : "Pagamento"
                },
                pixQrCode = new
                {
                    encodedImage = pedidoPagamento.PixEncodedImage,
                    payload = pedidoPagamento.PixPayload,
                    expirationDate = expiracaoVisualUtc,
                    description = qrCode.Description
                }
            });
        }
        catch (Exception ex)
        {
            Response.StatusCode = 500;

            return Json(new
            {
                sucesso = false,
                mensagem = "Erro interno ao processar a cobrança Pix.",
                detalhes = ex.Message
            });
        }
    }

    [HttpPost("criar-cobranca-cartao")]
    public async Task<IActionResult> CriarCobrancaCartaoPedido([FromBody] PagamentoViewModel model)
    {
        try
        {
            if (model == null || model.PedidoId == Guid.Empty)
            {
                return Json(new
                {
                    sucesso = false,
                    mensagem = "Dados inválidos."
                });
            }

            var clienteIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(clienteIdClaim) || !Guid.TryParse(clienteIdClaim, out var clienteId))
            {
                Response.StatusCode = 401;
                return Json(new
                {
                    sucesso = false,
                    mensagem = "Usuário não autenticado."
                });
            }

            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.Id == clienteId);

            if (cliente == null)
            {
                Response.StatusCode = 404;
                return Json(new
                {
                    sucesso = false,
                    mensagem = "Cliente não encontrado."
                });
            }

            var pedido = await _context.Pedidos
                .Include(p => p.Pagamentos)
                .FirstOrDefaultAsync(p => p.Id == model.PedidoId && p.ClienteId == clienteId);

            if (pedido == null)
            {
                Response.StatusCode = 404;
                return Json(new
                {
                    sucesso = false,
                    mensagem = "Pedido não encontrado."
                });
            }

            pedido.Pagamentos ??= new List<PedidoPagamento>();

            var tipoCobranca = model.TipoCobranca;

            if (tipoCobranca != ETipoCobrancaPedido.Sinal && tipoCobranca != ETipoCobrancaPedido.Saldo)
            {
                return Json(new
                {
                    sucesso = false,
                    mensagem = "Tipo de cobrança inválido para este fluxo."
                });
            }

            var valorCobranca = tipoCobranca switch
            {
                ETipoCobrancaPedido.Sinal => pedido.ValorEntrada,
                ETipoCobrancaPedido.Saldo => pedido.Total,
                _ => pedido.ValorEntrada
            };

            if (valorCobranca < 5m)
            {
                return Json(new
                {
                    sucesso = false,
                    mensagem = "O valor mínimo para gerar cobrança é de R$ 5,00."
                });
            }

            var pagamentoExistente = pedido.Pagamentos
                .OrderBy(p => p.Sequencia)
                .FirstOrDefault(p =>
                    p.TipoCobranca == tipoCobranca &&
                    !string.IsNullOrWhiteSpace(p.GatewayPaymentId) &&
                    (p.Status == EStatusPagamento.Pending
                     || p.Status == EStatusPagamento.AwaitingRiskAnalysis
                     || p.Status == EStatusPagamento.DunningRequested
                     || p.Status == EStatusPagamento.Overdue
                     || p.Status == EStatusPagamento.Received
                     || p.Status == EStatusPagamento.Confirmed
                     || p.Status == EStatusPagamento.ReceivedInCash));

            if (pagamentoExistente != null)
            {
                return Json(new
                {
                    sucesso = true,
                    pedidoPagamentoExistente = true,
                    customerId = pagamentoExistente.GatewayCustomerId,
                    payment = new
                    {
                        id = pagamentoExistente.GatewayPaymentId,
                        status = pagamentoExistente.Status.ToString(),
                        value = pagamentoExistente.Valor,
                        dueDate = (DateTime?)null,
                        invoiceUrl = pagamentoExistente.InvoiceUrl,
                        billingType = pagamentoExistente.TipoPagamento,
                        tipoCobranca = pagamentoExistente.TipoCobranca.ToString(),
                        sequencia = pagamentoExistente.Sequencia
                    },
                    resumoPedido = new
                    {
                        totalPedido = pedido.Total,
                        valorSinal = pedido.ValorEntrada,
                        valorRestanteRetirada = tipoCobranca == ETipoCobrancaPedido.Saldo
                            ? 0
                            : (pedido.Total - pedido.ValorEntrada),
                        tipoCobranca = tipoCobranca.ToString(),
                        tituloPagamento = tipoCobranca == ETipoCobrancaPedido.Sinal
                            ? "Pagamento do Sinal (50%)"
                            : "Pagamento"
                    }
                });
            }

            string customerId = cliente.IdClientePagamento;

            if (string.IsNullOrWhiteSpace(customerId))
            {
                customerId = await CriarCliente(new CriarClienteAsaasRequest
                {
                    Name = cliente.Nome,
                    CpfCnpj = cliente.CPF
                });

                if (string.IsNullOrWhiteSpace(customerId))
                {
                    return Json(new
                    {
                        sucesso = false,
                        mensagem = "Não foi possível criar o cliente no Asaas."
                    });
                }

                cliente.IdClientePagamento = customerId;
                await _context.SaveChangesAsync();
            }

            var pagamentoAsaas = await CriarCobrancaCartao(
                customerId,
                valorCobranca,
                DateTime.UtcNow.AddDays(1),
                ""
            );

            if (pagamentoAsaas == null || string.IsNullOrWhiteSpace(pagamentoAsaas.Id))
            {
                return Json(new
                {
                    sucesso = false,
                    mensagem = "Não foi possível gerar a cobrança por cartão."
                });
            }

            var proximaSequencia = pedido.Pagamentos.Any()
                ? pedido.Pagamentos.Max(p => p.Sequencia) + 1
                : 1;

            var pedidoPagamento = new PedidoPagamento
            {
                PedidoId = pedido.Id,
                Gateway = "ASAAS",
                TipoPagamento = "CREDIT_CARD",
                GatewayCustomerId = customerId,
                GatewayPaymentId = pagamentoAsaas.Id,
                Valor = pagamentoAsaas.Value,
                Status = MapearStatusAsaas(pagamentoAsaas.Status),
                TipoCobranca = tipoCobranca,
                Sequencia = proximaSequencia,
                InvoiceUrl = pagamentoAsaas.InvoiceUrl,
                CriadoEmUtc = DateTime.UtcNow
            };

            _context.PedidoPagamentos.Add(pedidoPagamento);
            await _context.SaveChangesAsync();

            return Json(new
            {
                sucesso = true,
                pedidoPagamentoExistente = false,
                customerId = customerId,
                payment = new
                {
                    id = pedidoPagamento.GatewayPaymentId,
                    status = pagamentoAsaas.Status,
                    value = pedidoPagamento.Valor,
                    dueDate = pagamentoAsaas.DueDate,
                    invoiceUrl = pedidoPagamento.InvoiceUrl,
                    billingType = pedidoPagamento.TipoPagamento,
                    tipoCobranca = pedidoPagamento.TipoCobranca.ToString(),
                    sequencia = pedidoPagamento.Sequencia
                },
                resumoPedido = new
                {
                    totalPedido = pedido.Total,
                    valorSinal = pedido.ValorEntrada,
                    valorRestanteRetirada = tipoCobranca == ETipoCobrancaPedido.Saldo
                        ? 0
                        : (pedido.Total - pedido.ValorEntrada),
                    tipoCobranca = tipoCobranca.ToString(),
                    tituloPagamento = tipoCobranca == ETipoCobrancaPedido.Sinal
                        ? "Pagamento do Sinal (50%)"
                        : "Pagamento"
                }
            });
        }
        catch (Exception ex)
        {
            Response.StatusCode = 500;

            return Json(new
            {
                sucesso = false,
                mensagem = "Erro interno ao processar a cobrança por cartão.",
                detalhes = ex.Message
            });
        }
    }

    [HttpPost("confirmar-pagamento")]
    public async Task<IActionResult> ConfirmarPagamentoSinal([FromBody] ConfirmarPagamentoRequest request)
    {
        try
        {
            if (request == null || request.PedidoId == Guid.Empty)
            {
                return Json(new
                {
                    success = false,
                    message = "Pedido inválido."
                });
            }

            var clienteIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(clienteIdClaim) || !Guid.TryParse(clienteIdClaim, out var clienteId))
            {
                Response.StatusCode = 401;
                return Json(new
                {
                    success = false,
                    message = "Usuário não autenticado."
                });
            }

            var pedido = await _context.Pedidos
                .Include(p => p.Pagamentos)
                .FirstOrDefaultAsync(p => p.Id == request.PedidoId && p.ClienteId == clienteId);

            if (pedido == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Pedido não encontrado."
                });
            }

            pedido.Pagamentos ??= new List<PedidoPagamento>();

            var pagamentoAtual = pedido.Pagamentos
                .OrderBy(p => p.Sequencia)
                .FirstOrDefault(p =>
                    !string.IsNullOrWhiteSpace(p.GatewayPaymentId) &&
                    (p.Status == EStatusPagamento.Pending
                     || p.Status == EStatusPagamento.AwaitingRiskAnalysis
                     || p.Status == EStatusPagamento.DunningRequested
                     || p.Status == EStatusPagamento.Overdue));

            if (pagamentoAtual == null)
            {
                pagamentoAtual = pedido.Pagamentos
                    .OrderByDescending(p => p.Sequencia)
                    .FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.GatewayPaymentId));
            }

            if (pagamentoAtual == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Nenhuma cobrança encontrada para este pedido."
                });
            }

            var pagamentoAsaas = await ConsultarPagamentoAsaas(pagamentoAtual.GatewayPaymentId!);

            if (pagamentoAsaas == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Não foi possível consultar o pagamento no Asaas."
                });
            }

            var novoStatus = MapearStatusAsaas(pagamentoAsaas.Status);
            pagamentoAtual.Status = novoStatus;

            if (novoStatus == EStatusPagamento.Received
                || novoStatus == EStatusPagamento.Confirmed
                || novoStatus == EStatusPagamento.ReceivedInCash)
            {
                pagamentoAtual.PagoEmUtc = DateTime.UtcNow;

                if (pedido.Status == EPedidoStatus.AguardandoPagamento)
                {
                    pedido.Status = EPedidoStatus.Confirmado;
                }
            }

            await _context.SaveChangesAsync();

            if (novoStatus == EStatusPagamento.Received
                || novoStatus == EStatusPagamento.Confirmed
                || novoStatus == EStatusPagamento.ReceivedInCash)
            {
                return Json(new
                {
                    success = true,
                    message = pagamentoAtual.TipoCobranca == ETipoCobrancaPedido.Sinal
                        ? "Pagamento do sinal confirmado com sucesso."
                        : "Pagamento confirmado com sucesso.",
                    tipoCobranca = pagamentoAtual.TipoCobranca.ToString(),
                    redirectUrl = Url.Action("EmAndamento", "Pedido", new { confirmado = true })
                });
            }

            return Json(new
            {
                success = false,
                message = $"Pagamento ainda não confirmado. Status atual: {pagamentoAsaas.Status}",
                tipoCobranca = pagamentoAtual.TipoCobranca.ToString()
            });
        }
        catch (Exception ex)
        {
            Response.StatusCode = 500;

            return Json(new
            {
                success = false,
                message = "Erro interno ao consultar o pagamento.",
                details = ex.Message
            });
        }
    }

    [HttpPost("pagar-cartao")]
    public async Task<IActionResult> PagarCartao([FromBody] PagarCartaoRequest request)
    {
        try
        {
            if (request == null || request.PedidoId == Guid.Empty || string.IsNullOrWhiteSpace(request.PaymentId))
            {
                return Json(new
                {
                    sucesso = false,
                    mensagem = "Dados inválidos para pagamento."
                });
            }

            var clienteIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(clienteIdClaim) || !Guid.TryParse(clienteIdClaim, out var clienteId))
            {
                Response.StatusCode = 401;
                return Json(new
                {
                    sucesso = false,
                    mensagem = "Usuário não autenticado."
                });
            }

            var pedido = await _context.Pedidos
                .Include(p => p.Pagamentos)
                .FirstOrDefaultAsync(p => p.Id == request.PedidoId && p.ClienteId == clienteId);

            if (pedido == null)
            {
                return Json(new
                {
                    sucesso = false,
                    mensagem = "Pedido não encontrado."
                });
            }

            var pedidoPagamento = pedido.Pagamentos
                .FirstOrDefault(p => p.GatewayPaymentId == request.PaymentId);

            if (pedidoPagamento == null)
            {
                return Json(new
                {
                    sucesso = false,
                    mensagem = "Cobrança não encontrada para este pedido."
                });
            }

            var asaasRequest = new PayWithCreditCardAsaasRequest
            {
                CreditCard = new CreditCardAsaasRequest
                {
                    HolderName = request.CreditCard.HolderName,
                    Number = request.CreditCard.Number,
                    ExpiryMonth = request.CreditCard.ExpiryMonth,
                    ExpiryYear = request.CreditCard.ExpiryYear,
                    Ccv = request.CreditCard.Ccv
                },
                CreditCardHolderInfo = new CreditCardHolderInfoAsaasRequest
                {
                    Name = request.CreditCardHolderInfo.Name,
                    Email = request.CreditCardHolderInfo.Email,
                    CpfCnpj = request.CreditCardHolderInfo.CpfCnpj,
                    PostalCode = request.CreditCardHolderInfo.PostalCode,
                    AddressNumber = request.CreditCardHolderInfo.AddressNumber,
                    AddressComplement = request.CreditCardHolderInfo.AddressComplement,
                    Phone = request.CreditCardHolderInfo.Phone,
                    MobilePhone = request.CreditCardHolderInfo.MobilePhone
                }
            };

            var pagamentoAsaas = await PagarCobrancaComCartao(request.PaymentId, asaasRequest);

            if (pagamentoAsaas == null)
            {
                return Json(new
                {
                    sucesso = false,
                    mensagem = "Não foi possível processar o pagamento com cartão."
                });
            }

            var novoStatus = MapearStatusAsaas(pagamentoAsaas.Status);
            pedidoPagamento.Status = novoStatus;

            if (novoStatus == EStatusPagamento.Received
                || novoStatus == EStatusPagamento.Confirmed
                || novoStatus == EStatusPagamento.ReceivedInCash)
            {
                pedidoPagamento.PagoEmUtc = DateTime.UtcNow;

                if (pedidoPagamento.TipoCobranca == ETipoCobrancaPedido.Sinal)
                {
                    pedido.Status = EPedidoStatus.Confirmado;
                }
            }

            await _context.SaveChangesAsync();

            return Json(new
            {
                sucesso = true,
                status = pagamentoAsaas.Status,
                redirectUrl = Url.Action("Detalhes", "Pedido", new { id = pedido.Id })
            });
        }
        catch (Exception ex)
        {
            Response.StatusCode = 500;
            return Json(new
            {
                sucesso = false,
                mensagem = "Erro interno ao processar pagamento com cartão.",
                detalhes = ex.Message
            });
        }
    }

    [AllowAnonymous]
    [HttpPost("webhook/asaas")]
    public async Task<IActionResult> WebhookAsaas([FromBody] WebhookAsaasRequest request)
    {
        if (request?.Payment == null || string.IsNullOrWhiteSpace(request.Payment.Id))
            return Ok();

        var pedidoPagamento = await _context.PedidoPagamentos
            .Include(pp => pp.Pedido)
            .FirstOrDefaultAsync(pp => pp.GatewayPaymentId == request.Payment.Id);

        if (pedidoPagamento == null)
            return Ok();

        var status = MapearStatusAsaas(request.Payment.Status);
        pedidoPagamento.Status = status;

        if (status == EStatusPagamento.Received
            || status == EStatusPagamento.Confirmed
            || status == EStatusPagamento.ReceivedInCash)
        {
            pedidoPagamento.PagoEmUtc = DateTime.UtcNow;

            if (pedidoPagamento.TipoCobranca == ETipoCobrancaPedido.Sinal)
            {
                pedidoPagamento.Pedido.Status = EPedidoStatus.Confirmado;
            }
        }

        await _context.SaveChangesAsync();

        var redirectUrl = Url.Action("Index", "Home", new { confirmado = true });

        await _hubContext.Clients.Group($"pedido-{pedidoPagamento.PedidoId}")
            .SendAsync("PagamentoConfirmado", new
            {
                pedidoId = pedidoPagamento.PedidoId,
                gatewayPaymentId = pedidoPagamento.GatewayPaymentId,
                tipoCobranca = pedidoPagamento.TipoCobranca.ToString(),
                status = request.Payment.Status,
                redirectUrl = redirectUrl
            });

        return Ok();
    }

    private async Task<string> CriarCliente(CriarClienteAsaasRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("customers", request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return string.Empty;

            var clienteCriado = JsonSerializer.Deserialize<CriarClienteAsaasResponse>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (clienteCriado == null || string.IsNullOrWhiteSpace(clienteCriado.Id))
                return string.Empty;

            return clienteCriado.Id;
        }
        catch
        {
            return string.Empty;
        }
    }

    private async Task<CriarCobrancaPixAsaasResponse> CriarCobrancaPix(string customerId, decimal value, DateTime dueDate, string? description = null)
    {
        try
        {
            var request = new CriarCobrancaPixAsaasRequest
            {
                Customer = customerId,
                BillingType = "PIX",
                Value = value,
                DueDate = dueDate.ToString("yyyy-MM-dd"),
                Description = description
            };

            var response = await _httpClient.PostAsJsonAsync("payments", request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return null;

            var pagamento = JsonSerializer.Deserialize<CriarCobrancaPixAsaasResponse>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return pagamento;
        }
        catch
        {
            return null;
        }
    }

    private async Task<ObterQrCodePixAsaasResponse> ObterQrCodePix(string paymentId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"payments/{paymentId}/pixQrCode");
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return null;

            var qrCode = JsonSerializer.Deserialize<ObterQrCodePixAsaasResponse>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return qrCode;
        }
        catch
        {
            return null;
        }
    }

    private async Task<ConsultarPagamentoAsaasResponse?> ConsultarPagamentoAsaas(string paymentId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"payments/{paymentId}");
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return null;

            return JsonSerializer.Deserialize<ConsultarPagamentoAsaasResponse>(
                content,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }
        catch
        {
            return null;
        }
    }
    
    private async Task<ConsultarPagamentoAsaasResponse?> PagarCobrancaComCartao(
    string paymentId,
    PayWithCreditCardAsaasRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"payments/{paymentId}/payWithCreditCard", request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return null;

            return JsonSerializer.Deserialize<ConsultarPagamentoAsaasResponse>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return null;
        }
    }

    #region Classes provisórias

    public class PagamentoViewModel
    {
        public Guid PedidoId { get; set; }
        public ETipoCobrancaPedido TipoCobranca { get; set; } = ETipoCobrancaPedido.Saldo;
    }

    #region webhook
    public class WebhookAsaasRequest
    {
        public string? Event { get; set; }
        public WebhookAsaasPayment? Payment { get; set; }
    }

    public class WebhookAsaasPayment
    {
        public string? Id { get; set; }
        public string? Status { get; set; }
    }
    #endregion

    #region Cliente

    public class CriarClienteAsaasRequest
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("cpfCnpj")]
        public string CpfCnpj { get; set; } = string.Empty;
    }

    public class CriarClienteAsaasResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }

    #endregion

    #region Cobranca Pix

    public class CriarCobrancaPixAsaasRequest
    {
        [JsonPropertyName("customer")]
        public string Customer { get; set; } = string.Empty;

        [JsonPropertyName("billingType")]
        public string BillingType { get; set; } = "PIX";

        [JsonPropertyName("value")]
        public decimal Value { get; set; }

        [JsonPropertyName("dueDate")]
        public string DueDate { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }

    public class CriarCobrancaPixAsaasResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("value")]
        public decimal Value { get; set; }

        [JsonPropertyName("dueDate")]
        public string? DueDate { get; set; }

        [JsonPropertyName("invoiceUrl")]
        public string? InvoiceUrl { get; set; }

        [JsonPropertyName("billingType")]
        public string? BillingType { get; set; }
    }

    #endregion

    #region Cobrança Cartão
    private async Task<CriarCobrancaCartaoAsaasResponse?> CriarCobrancaCartao(
    string customerId,
    decimal value,
    DateTime dueDate,
    string? description = null)
    {
        try
        {
            var request = new CriarCobrancaCartaoAsaasRequest
            {
                Customer = customerId,
                BillingType = "CREDIT_CARD",
                Value = value,
                DueDate = dueDate.ToString("yyyy-MM-dd"),
                Description = description
            };

            var response = await _httpClient.PostAsJsonAsync("payments", request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return null;

            return JsonSerializer.Deserialize<CriarCobrancaCartaoAsaasResponse>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return null;
        }
    }

    public class CriarCobrancaCartaoAsaasRequest
    {
        [JsonPropertyName("customer")]
        public string Customer { get; set; } = string.Empty;

        [JsonPropertyName("billingType")]
        public string BillingType { get; set; } = "CREDIT_CARD";

        [JsonPropertyName("value")]
        public decimal Value { get; set; }

        [JsonPropertyName("dueDate")]
        public string DueDate { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }

    public class CriarCobrancaCartaoAsaasResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("value")]
        public decimal Value { get; set; }

        [JsonPropertyName("dueDate")]
        public string? DueDate { get; set; }

        [JsonPropertyName("invoiceUrl")]
        public string? InvoiceUrl { get; set; }

        [JsonPropertyName("billingType")]
        public string? BillingType { get; set; }
    }
    
    public class PagarCartaoRequest
    {
        public Guid PedidoId { get; set; }
        public string PaymentId { get; set; } = string.Empty;
        public CreditCardDto CreditCard { get; set; } = new();
        public CreditCardHolderInfoDto CreditCardHolderInfo { get; set; } = new();
    }

    public class CreditCardDto
    {
        public string HolderName { get; set; } = string.Empty;
        public string Number { get; set; } = string.Empty;
        public string ExpiryMonth { get; set; } = string.Empty;
        public string ExpiryYear { get; set; } = string.Empty;
        public string Ccv { get; set; } = string.Empty;
    }

    public class CreditCardHolderInfoDto
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string CpfCnpj { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string AddressNumber { get; set; } = string.Empty;
        public string? AddressComplement { get; set; }
        public string Phone { get; set; } = string.Empty;
        public string? MobilePhone { get; set; }
    }

    public class PayWithCreditCardAsaasRequest
    {
        [JsonPropertyName("creditCard")]
        public CreditCardAsaasRequest CreditCard { get; set; } = new();

        [JsonPropertyName("creditCardHolderInfo")]
        public CreditCardHolderInfoAsaasRequest CreditCardHolderInfo { get; set; } = new();
    }

    public class CreditCardAsaasRequest
    {
        [JsonPropertyName("holderName")]
        public string HolderName { get; set; } = string.Empty;

        [JsonPropertyName("number")]
        public string Number { get; set; } = string.Empty;

        [JsonPropertyName("expiryMonth")]
        public string ExpiryMonth { get; set; } = string.Empty;

        [JsonPropertyName("expiryYear")]
        public string ExpiryYear { get; set; } = string.Empty;

        [JsonPropertyName("ccv")]
        public string Ccv { get; set; } = string.Empty;
    }

    public class CreditCardHolderInfoAsaasRequest
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("cpfCnpj")]
        public string CpfCnpj { get; set; } = string.Empty;

        [JsonPropertyName("postalCode")]
        public string PostalCode { get; set; } = string.Empty;

        [JsonPropertyName("addressNumber")]
        public string AddressNumber { get; set; } = string.Empty;

        [JsonPropertyName("addressComplement")]
        public string? AddressComplement { get; set; }

        [JsonPropertyName("phone")]
        public string Phone { get; set; } = string.Empty;

        [JsonPropertyName("mobilePhone")]
        public string? MobilePhone { get; set; }
    }
    #endregion

    #region QR Code Pix

    public class ObterQrCodePixAsaasResponse
    {
        [JsonPropertyName("encodedImage")]
        public string? EncodedImage { get; set; }

        [JsonPropertyName("payload")]
        public string? Payload { get; set; }

        [JsonPropertyName("expirationDate")]
        public string? ExpirationDate { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }

    #endregion

    #region Consulta Pagamento
    public class ConfirmarPagamentoRequest
    {
        public Guid PedidoId { get; set; }
    }

    public class ConsultarPagamentoAsaasResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("billingType")]
        public string? BillingType { get; set; }

        [JsonPropertyName("value")]
        public decimal Value { get; set; }

        [JsonPropertyName("dueDate")]
        public string? DueDate { get; set; }

        [JsonPropertyName("paymentDate")]
        public string? PaymentDate { get; set; }

        [JsonPropertyName("clientPaymentDate")]
        public string? ClientPaymentDate { get; set; }

        [JsonPropertyName("invoiceUrl")]
        public string? InvoiceUrl { get; set; }
    }
    #endregion

    #region Communs
    private EStatusPagamento MapearStatusAsaas(string? status)
    {
        return status?.ToUpper() switch
        {
            "PENDING" => EStatusPagamento.Pending,
            "RECEIVED" => EStatusPagamento.Received,
            "CONFIRMED" => EStatusPagamento.Confirmed,
            "OVERDUE" => EStatusPagamento.Overdue,
            "REFUNDED" => EStatusPagamento.Refunded,
            "RECEIVED_IN_CASH" => EStatusPagamento.ReceivedInCash,
            "REFUND_REQUESTED" => EStatusPagamento.RefundRequested,
            "REFUND_IN_PROGRESS" => EStatusPagamento.RefundInProgress,
            "CHARGEBACK_REQUESTED" => EStatusPagamento.ChargebackRequested,
            "CHARGEBACK_DISPUTE" => EStatusPagamento.ChargebackDispute,
            "AWAITING_CHARGEBACK_REVERSAL" => EStatusPagamento.AwaitingChargebackReversal,
            "DUNNING_REQUESTED" => EStatusPagamento.DunningRequested,
            "DUNNING_RECEIVED" => EStatusPagamento.DunningReceived,
            "AWAITING_RISK_ANALYSIS" => EStatusPagamento.AwaitingRiskAnalysis,
            _ => EStatusPagamento.Pending
        };
    }
    #endregion

    #endregion
}