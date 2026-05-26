using System.Text.Json.Serialization;

namespace Cma.Services.Identity.Wso2.CreateUser;

public record Wso2EnterpriseUser([property: JsonPropertyName("cmahid")] string CmahId)
{
    [JsonPropertyName("accountLock")] public bool AccountLock { get; set; } = true;

    [JsonPropertyName("cmaMember")] public bool IsCmaMember { get; set; } = false;
};