using System.Net.Mime;
using System.Text;
using Cma.Common.Exceptions;
using Cma.Common.Extensions;
using Cma.Services.Payment.Bambora;
using Cma.Services.Payment.Contracts;
using Humanizer;
using Microsoft.Extensions.Logging;

namespace Cma.Services.Payment;

public class PaymentService : IPaymentService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(IHttpClientFactory httpClientFactory, ILogger<PaymentService> logger)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient(nameof(PaymentService));
    }

    public async Task<ProcessPaymentResponse> ProcessPayment(ProcessPaymentRequest request)
    {
        _logger.LogInformationWithMetadata(request);

        new ProcessPaymentRequestValidator().ValidateAndThrowBadRequest(request);

        // https://dev.na.bambora.com/docs/references/payment_APIs/v1-0-5/
        const string endpoint = "v1/payments";

        var bamboraTokenPaymentRequest = new BamboraTokenPaymentRequest(
            request.OrderNumber,
            request.Amount,
            new(request.CardholderName, request.PaymentTokenCode)
        );

        _logger.LogInformationWithMetadata(bamboraTokenPaymentRequest);
        using StringContent body = new(bamboraTokenPaymentRequest.Serialize(), Encoding.UTF8,
            MediaTypeNames.Application.Json);

        var response = await _httpClient.PostAsync(endpoint, body);

        var bamboraResponse = await response.Content.ReadAsStringAsync();
        _logger.LogInformationWithMetadata(nameof(bamboraResponse), bamboraResponse);

        if (!response.IsSuccessStatusCode)
        {
            var bamboraResponseFailure = bamboraResponse.Deserialize<BamboraErrorResponse>();
            
            throw new ConflictException(
                ErrorCode.PaymentFailure.ToSnakeCase(),
                bamboraResponseFailure?.Message ?? ErrorCode.PaymentFailure.Humanize(),
                bamboraResponseFailure);
        }

        var bamboraResponseSuccess = bamboraResponse.Deserialize<BamboraPaymentResponse>();

        if (bamboraResponseSuccess == null)
        {
            throw new InternalServerErrorException(
                ErrorCode.UnknownError.ToSnakeCase(),
                "Bambora response could not be deserialized"
            );
        }

        var processPaymentResponse = new ProcessPaymentResponse(
            bamboraResponseSuccess.Id,
            bamboraResponseSuccess.Approved == BamboraConstants.Approval.Approved,
            bamboraResponseSuccess.Card.CardType,
            bamboraResponseSuccess.Card.LastFour,
            bamboraResponseSuccess.OrderNumber,
            bamboraResponseSuccess.Amount,
            bamboraResponseSuccess.Created
        );
        _logger.LogInformationWithMetadata(processPaymentResponse);

        return processPaymentResponse;
    }

    public async Task<GetPaymentTokenResponse> GetPaymentToken(GetPaymentTokenRequest request)
    {
        _logger.LogInformationWithMetadata(request);

        // https://dev.na.bambora.com/docs/references/payment_APIs/v1-0-5/
        const string endpoint = "/scripts/tokenization/tokens";

        var bamboraRequest = new BamboraTokenRequest(
            request.CreditCardNumber,
            request.ExpiryMonth.ToString("D2"),
            request.ExpiryYear.ToString().Substring(2, 2),
            request.Cvd.ToString()
        );

        _logger.LogInformationWithMetadata(bamboraRequest);
        using StringContent body = new(bamboraRequest.Serialize(), Encoding.UTF8, MediaTypeNames.Application.Json);

        var response = await _httpClient.PostAsync(endpoint, body);

        var bamboraResponse = await response.Content.ReadAsStringAsync();
        _logger.LogInformationWithMetadata(nameof(bamboraResponse), bamboraResponse);

        var bamboraTokenResponse = bamboraResponse.Deserialize<BamboraTokenResponse>();

        if (bamboraTokenResponse == null)
        {
            throw new InternalServerErrorException(ErrorCode.UnknownError.ToSnakeCase(),
                "Bambora response could not be deserialized");
        }

        if (!string.IsNullOrWhiteSpace(bamboraTokenResponse.Message))
        {
            _logger.LogWarning(bamboraTokenResponse.Message);
        }

        return new(bamboraTokenResponse.Token);
    }

    public Task<ProcessRefundResponse> ProcessRefund(ProcessRefundRequest request)
    {
        throw new NotImplementedException();
    }
}