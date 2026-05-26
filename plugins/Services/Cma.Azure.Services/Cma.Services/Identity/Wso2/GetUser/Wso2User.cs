using System.Text.Json.Serialization;

namespace Cma.Services.Identity.Wso2.GetUser;

public record Wso2User
{
    [JsonPropertyName("cmahid")] public string CmahId { get; set; } = string.Empty;

    [JsonPropertyName("accountLock")] public bool AccountLock { get; set; }

    [JsonPropertyName("cmaMember")] public bool IsCmaMember { get; set; } = false;
}