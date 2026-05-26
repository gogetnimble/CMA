using Cma.Services.Payment.Contracts;

namespace Cma.Services.Payment;

public interface IPaymentService
{
    Task<ProcessPaymentResponse> ProcessPayment(ProcessPaymentRequest request);
    Task<GetPaymentTokenResponse> GetPaymentToken(GetPaymentTokenRequest request);
}