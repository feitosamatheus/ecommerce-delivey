namespace Ecommerce.Infrastructure.Enums;

public enum EStatusPagamento
{
    Pending = 1,
    Received = 2,
    Confirmed = 3,
    Overdue = 4,
    Refunded = 5,
    ReceivedInCash = 6,
    RefundRequested = 7,
    RefundInProgress = 8,
    ChargebackRequested = 9,
    ChargebackDispute = 10,
    AwaitingChargebackReversal = 11,
    DunningRequested = 12,
    DunningReceived = 13,
    AwaitingRiskAnalysis = 14
}
