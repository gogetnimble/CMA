using System.Text.Json.Serialization;

namespace Cma.Services.Payment.Bambora;

public record BamboraTokenRequest(
    [property: JsonPropertyName("number")] string Number,
    [property: JsonPropertyName("expiry_month")]
    string ExpiryMonth,
    [property: JsonPropertyName("expiry_year")]
    string ExpiryYear,
    [property: JsonPropertyName("cvd")] string Cvd);