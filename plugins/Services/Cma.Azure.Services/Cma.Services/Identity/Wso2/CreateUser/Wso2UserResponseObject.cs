using System.Text.Json.Serialization;

namespace Cma.Services.Identity.Wso2.CreateUser;

public record Wso2UserResponseObject(
    [property: JsonPropertyName("userName")]
    string Username,
    [property: JsonPropertyName("id")]
    string Id,
    [property: JsonPropertyName(Wso2Constants.Schemas.User)]
    Wso2EnterpriseUser User
);