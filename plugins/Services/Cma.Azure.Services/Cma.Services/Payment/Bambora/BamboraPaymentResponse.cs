using System.Text.Json.Serialization;

namespace Cma.Services.Payment.Bambora;

public record BamboraPaymentResponse
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;

    [JsonPropertyName("authorizing_merchant_id")]
    public int AuthorizingMerchantId { get; init; }

    [JsonPropertyName("approved")] public string Approved { get; init; } = string.Empty;

    [JsonPropertyName("message_id")] public string MessageId { get; init; } = string.Empty;

    [JsonPropertyName("message")] public string Message { get; init; } = string.Empty;

    [JsonPropertyName("auth_code")] public string AuthCode { get; init; } = string.Empty;

    [JsonPropertyName("created")] public DateTime Created { get; init; }

    [JsonPropertyName("order_number")] public string OrderNumber { get; init; } = string.Empty;

    [JsonPropertyName("type")] public string Type { get; init; } = string.Empty;

    [JsonPropertyName("payment_method")] public string PaymentMethod { get; init; } = string.Empty;

    [JsonPropertyName("risk_score")] public decimal RiskScore { get; init; }

    [JsonPropertyName("amount")] public decimal Amount { get; init; }

    [JsonPropertyName("custom")] public BamboraCustom Custom { get; init; } = new();

    [JsonPropertyName("card")]
    public BamboraCardPurchaseResponse Card { get; init; } = new();

    [JsonPropertyName("links")] public List<BamboraLink> Links { get; init; } = [];
}