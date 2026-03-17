using Ecommerce.MVC.Config;
using Ecommerce.MVC.Entities;
using Ecommerce.MVC.Enums;
using Ecommerce.MVC.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
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

            var clienteIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(clienteIdClaim) || !Guid.TryParse(clienteIdClaim, out var clienteId))
            {
                Response.StatusCode = 401;
                return Json(new
                {
                    sucesso = false,
                    mensagem = "Usuário não autenticado."
                });
            }

            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.Id == clienteId);

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
                .Include(p => p.PedidoPagamento)
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

            if (pedido.ValorEntrada < 5m)
            {
                return Json(new
                {
                    sucesso = false,
                    mensagem = "O valor mínimo para gerar cobrança PIX é de R$ 5,00."
                });
            }

            if (pedido.PedidoPagamento != null)
            {
                return Json(new
                {
                    sucesso = true,
                    pedidoPagamentoExistente = true,
                    customerId = pedido.PedidoPagamento.GatewayCustomerId,
                    payment = new
                    {
                        id = pedido.PedidoPagamento.GatewayPaymentId,
                        status = pedido.PedidoPagamento.Status.ToString(),
                        value = pedido.PedidoPagamento.Valor,
                        dueDate = pedido.PedidoPagamento.PixExpirationDate,
                        invoiceUrl = pedido.PedidoPagamento.InvoiceUrl,
                        billingType = pedido.PedidoPagamento.TipoPagamento
                    },
                    pixQrCode = new
                    {
                        encodedImage = pedido.PedidoPagamento.PixEncodedImage,
                        payload = pedido.PedidoPagamento.PixPayload,
                        expirationDate = pedido.PedidoPagamento.PixExpirationDate,
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

            var pagamento = await CriarCobrancaPix(
                customerId,
                pedido.ValorEntrada,
                DateTime.UtcNow.AddHours(24),
                ""
            );

            if (pagamento == null || string.IsNullOrWhiteSpace(pagamento.Id))
            {
                return Json(new
                {
                    sucesso = false,
                    mensagem = "Não foi possível gerar a cobrança Pix.",
                    customerId = customerId
                });
            }

            var qrCode = await ObterQrCodePix(pagamento.Id);

            if (qrCode == null)
            {
                return Json(new
                {
                    sucesso = false,
                    mensagem = "Cobrança criada, mas não foi possível obter o QR Code Pix.",
                    customerId = customerId,
                    paymentId = pagamento.Id,
                    payment = pagamento
                });
            }

            DateTime? expiracaoPixUtc = null;

            if (!string.IsNullOrWhiteSpace(qrCode.ExpirationDate) &&
                DateTimeOffset.TryParse(qrCode.ExpirationDate, out var dto))
            {
                expiracaoPixUtc = dto.UtcDateTime;
            }

            var pedidoPagamento = new PedidoPagamento
            {
                PedidoId = pedido.Id,
                Gateway = "ASAAS",
                TipoPagamento = "PIX",
                GatewayCustomerId = customerId,
                GatewayPaymentId = pagamento.Id,
                Valor = pagamento.Value,
                Status = MapearStatusAsaas(pagamento.Status),
                PixPayload = qrCode.Payload,
                PixEncodedImage = qrCode.EncodedImage,
                PixExpirationDate = expiracaoPixUtc,
                InvoiceUrl = pagamento.InvoiceUrl,
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
                    id = pagamento.Id,
                    status = pagamento.Status,
                    value = pagamento.Value,
                    dueDate = pagamento.DueDate,
                    invoiceUrl = pagamento.InvoiceUrl,
                    billingType = pagamento.BillingType
                },
                pixQrCode = new
                {
                    encodedImage = qrCode.EncodedImage,
                    payload = qrCode.Payload,
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

            var clienteIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(clienteIdClaim) || !Guid.TryParse(clienteIdClaim, out var clienteId))
            {
                Response.StatusCode = 401;
                return Json(new
                {
                    sucesso = false,
                    mensagem = "Usuário não autenticado."
                });
            }

            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.Id == clienteId);

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
                .Include(p => p.PedidoPagamento)
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

            if (pedido.ValorEntrada < 5m)
            {
                return Json(new
                {
                    sucesso = false,
                    mensagem = "O valor mínimo para gerar cobrança é de R$ 5,00."
                });
            }

            if (pedido.PedidoPagamento != null)
            {
                return Json(new
                {
                    sucesso = true,
                    pedidoPagamentoExistente = true,
                    payment = new
                    {
                        id = pedido.PedidoPagamento.GatewayPaymentId,
                        status = pedido.PedidoPagamento.Status.ToString(),
                        value = pedido.PedidoPagamento.Valor,
                        invoiceUrl = pedido.PedidoPagamento.InvoiceUrl,
                        billingType = pedido.PedidoPagamento.TipoPagamento
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

            var pagamento = await CriarCobrancaCartao(
                customerId,
                pedido.ValorEntrada,
                DateTime.UtcNow.AddDays(1),
                ""
            );

            if (pagamento == null || string.IsNullOrWhiteSpace(pagamento.Id))
            {
                return Json(new
                {
                    sucesso = false,
                    mensagem = "Não foi possível gerar a cobrança por cartão."
                });
            }

            var pedidoPagamento = new PedidoPagamento
            {
                PedidoId = pedido.Id,
                Gateway = "ASAAS",
                TipoPagamento = "CREDIT_CARD",
                GatewayCustomerId = customerId,
                GatewayPaymentId = pagamento.Id,
                Valor = pagamento.Value,
                Status = MapearStatusAsaas(pagamento.Status),
                InvoiceUrl = pagamento.InvoiceUrl,
                CriadoEmUtc = DateTime.UtcNow
            };

            _context.PedidoPagamentos.Add(pedidoPagamento);
            await _context.SaveChangesAsync();

            return Json(new
            {
                sucesso = true,
                pedidoPagamentoExistente = false,
                payment = new
                {
                    id = pagamento.Id,
                    status = pagamento.Status,
                    value = pagamento.Value,
                    dueDate = pagamento.DueDate,
                    invoiceUrl = pagamento.InvoiceUrl,
                    billingType = pagamento.BillingType
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

            var pedidoId = request.PedidoId;

            var clienteIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

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
                .Include(p => p.PedidoPagamento)
                .FirstOrDefaultAsync(p => p.Id == pedidoId && p.ClienteId == clienteId);

            if (pedido == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Pedido não encontrado."
                });
            }

            if (pedido.PedidoPagamento == null || string.IsNullOrWhiteSpace(pedido.PedidoPagamento.GatewayPaymentId))
            {
                return Json(new
                {
                    success = false,
                    message = "Nenhuma cobrança PIX encontrada para este pedido."
                });
            }

            var paymentId = pedido.PedidoPagamento.GatewayPaymentId;

            var pagamentoAsaas = await ConsultarPagamentoAsaas(paymentId);

            if (pagamentoAsaas == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Não foi possível consultar o pagamento no Asaas."
                });
            }

            var novoStatus = MapearStatusAsaas(pagamentoAsaas.Status);
            pedido.PedidoPagamento.Status = novoStatus;

            if (novoStatus == EStatusPagamento.Received || novoStatus == EStatusPagamento.Confirmed)
            {
                pedido.Status = EPedidoStatus.Confirmado;
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Pagamento confirmado com sucesso.",
                    redirectUrl = Url.Action("Index", "Home", new { confirmado = true })
                });
            }

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = false,
                message = $"Pagamento ainda não confirmado. Status atual: Pendente"
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

        if (status == EStatusPagamento.Received || status == EStatusPagamento.Confirmed)
        {
            pedidoPagamento.Pedido.Status = EPedidoStatus.Confirmado;
        }

        await _context.SaveChangesAsync();

        var redirectUrl = Url.Action("Index", "Home", new { confirmado = true });

        await _hubContext.Clients.Group($"pedido-{pedidoPagamento.PedidoId}")
            .SendAsync("PagamentoConfirmado", new
            {
                pedidoId = pedidoPagamento.PedidoId,
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
    
    #region Classes provisórias

    public class PagamentoViewModel
    {
        public Guid PedidoId { get; set; }
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