using System.Text.Json.Serialization;

namespace Cma.Services.Payment.Bambora;

public record BamboraTokenResponse(
    [property: JsonPropertyName("token")] string Token,
    [property: JsonPropertyName("message")]
    string Message);