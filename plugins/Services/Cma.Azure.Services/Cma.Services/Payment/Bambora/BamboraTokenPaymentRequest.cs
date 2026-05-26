using System.Text.Json.Serialization;

namespace Cma.Services.Payment.Bambora;

public record BamboraTokenPaymentRequest(
    [property: JsonPropertyName("order_number")]
    string OrderNumber,
    [property: JsonPropertyName("amount")] decimal Amount,
    [property: JsonPropertyName("token")] BamboraTokenPurchase Token)
{
    [JsonPropertyName("payment_method")] public string PaymentMethod => "token";
}