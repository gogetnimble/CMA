using System.Text.Json.Serialization;

namespace Cma.Services.Identity.Wso2;

public record Wso2Error(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("message")]
    string Message,
    [property: JsonPropertyName("description")]
    string Description,
    [property: JsonPropertyName("properties")]
    Dictionary<string, string> Properties
);