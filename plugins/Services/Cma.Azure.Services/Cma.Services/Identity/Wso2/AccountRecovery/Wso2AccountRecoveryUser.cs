using System.Text.Json.Serialization;

namespace Cma.Services.Identity.Wso2.AccountRecovery;

public record Wso2AccountRecoveryUser([property: JsonPropertyName("username")]string Username);