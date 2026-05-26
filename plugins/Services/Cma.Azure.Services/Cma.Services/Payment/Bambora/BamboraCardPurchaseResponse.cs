using System.Text.Json.Serialization;

namespace Cma.Services.Payment.Bambora;

public record BamboraCardPurchaseResponse
{
    [JsonPropertyName("card_type")] public string CardType { get; init; } = string.Empty;

    [JsonPropertyName("last_four")] public string LastFour { get; init; } = string.Empty;

    [JsonPropertyName("card_bin")] public string CardBin { get; init; } = string.Empty;

    [JsonPropertyName("address_match")] public int AddressMatch { get; init; }

    [JsonPropertyName("postal_result")] public int PostalResult { get; init; }

    [JsonPropertyName("avs_result")] public string AvsResult { get; init; } = string.Empty;

    [JsonPropertyName("cvd_result")] public string CvdResult { get; init; } = string.Empty;

    [JsonPropertyName("avs")] public BamboraAvs Avs { get; init; } = new();
}