using System.Text.Json.Serialization;

namespace Cma.Services.Payment.Bambora;

public record BamboraErrorResponse
{
    [JsonPropertyName("code")] public int Code { get; init; }

    [JsonPropertyName("category")] public int Category { get; init; }

    [JsonPropertyName("message")] public string Message { get; init; } = string.Empty;

    [JsonPropertyName("reference")] public string Reference { get; init; } = string.Empty;

    [JsonPropertyName("card")] public BamboraCardPurchaseResponse Card { get; init; } = new();

    [JsonPropertyName("details")] public List<BamboraDetail> Details { get; init; } = [];

    [JsonPropertyName("validation")] public BamboraCardValidation Validation { get; init; } = new();
}