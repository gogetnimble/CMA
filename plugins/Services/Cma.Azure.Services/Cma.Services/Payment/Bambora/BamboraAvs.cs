using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Cma.Services.Payment.Bambora;

[ExcludeFromCodeCoverage(Justification = "Unused. Needed for deserialization.")]
public record BamboraAvs
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("processed")]
    public bool Processed { get; init; }
}