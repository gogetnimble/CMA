namespace Cma.Services.Payment.Contracts;

public record ProcessPaymentResponse(
    string PaymentId,
    bool Approved,
    string CardType,
    string CardLastFourDigits,
    string OrderNumber,
    decimal Amount,
    DateTime CreatedDate);