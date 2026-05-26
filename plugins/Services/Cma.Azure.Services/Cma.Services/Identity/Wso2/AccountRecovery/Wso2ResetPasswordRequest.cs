using System.Text.Json.Serialization;

namespace Cma.Services.Identity.Wso2.AccountRecovery;

public record Wso2ResetPasswordRequest([property: JsonPropertyName("key")] string Key, [property: JsonPropertyName("password")] string Password);