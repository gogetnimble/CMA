using System.Text.Json.Serialization;

namespace Cma.Services.Identity.Wso2;

public record Wso2AuthenticationSuccessResponse([property: JsonPropertyName("token")] string Token);