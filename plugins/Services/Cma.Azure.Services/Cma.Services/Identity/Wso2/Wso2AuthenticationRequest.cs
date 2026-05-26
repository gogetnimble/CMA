using System.Text.Json.Serialization;

namespace Cma.Services.Identity.Wso2;

public record Wso2AuthenticationRequest(
    [property: JsonPropertyName("username")]
    string Username,
    [property: JsonPropertyName("password")]
    string Password);