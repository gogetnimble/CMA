using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Cma.Services.Payment.Bambora;

[ExcludeFromCodeCoverage(Justification = "Unused. Needed for deserialization.")]
public record BamboraCustom
{
    [JsonPropertyName("ref1")] public string Ref1 { get; init; } = string.Empty;
    [JsonPropertyName("ref2")] public string Ref2 { get; init; } = string.Empty;
    [JsonPropertyName("ref3")] public string Ref3 { get; init; } = string.Empty;
    [JsonPropertyName("ref4")] public string Ref4 { get; init; } = string.Empty;
    [JsonPropertyName("ref5")] public string Ref5 { get; init; } = string.Empty;
}