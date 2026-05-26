using System.Text.Json.Serialization;

namespace Cma.Services.Payment.Bambora;

public record BamboraTokenPurchase(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("code")] string Code
)
{
    [JsonPropertyName("complete")] public bool Complete => true;
}