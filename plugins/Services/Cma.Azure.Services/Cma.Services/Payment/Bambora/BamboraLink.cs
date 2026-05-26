using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Cma.Services.Payment.Bambora;

[ExcludeFromCodeCoverage(Justification = "Unused. Needed for deserialization.")]
public record BamboraLink
{
    [JsonPropertyName("rel")] public string Rel { get; init; } = string.Empty;
    [JsonPropertyName("href")] public string Href { get; init; } = string.Empty;
    [JsonPropertyName("method")] public string Method { get; init; } = string.Empty;
}