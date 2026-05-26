namespace Cma.Services.Payment.Contracts;

public record GetPaymentTokenRequest(string CreditCardNumber, int ExpiryMonth, int ExpiryYear, int Cvd);