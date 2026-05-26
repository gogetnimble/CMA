using System.Text.Json.Serialization;

namespace Cma.Services.Identity.Wso2.AccountRecovery;

public record Wso2RecoveryInitiatingRequest([property: JsonPropertyName("user")] Wso2AccountRecoveryUser User);