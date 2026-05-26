namespace Cma.Services.Payment.Contracts;

public record ProcessPaymentRequest(
    string CardholderName,
    string PaymentTokenCode,
    decimal Amount,
    string OrderNumber);