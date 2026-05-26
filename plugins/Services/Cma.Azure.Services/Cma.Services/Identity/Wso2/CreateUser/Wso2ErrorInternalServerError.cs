using System.Text.Json.Serialization;

namespace Cma.Services.Identity.Wso2.CreateUser;

public record Wso2ErrorInternalServerError(
    [property:JsonPropertyName("status")] string Status,
    [property:JsonPropertyName("schemas")] string Schemas,
    [property:JsonPropertyName("detail")] string Detail
);