using System.Text.Json.Serialization;

namespace Cma.Services.Identity.Wso2.CreateUser;

record Wso2UserObject(
    [property: JsonPropertyName("userName")]
    string Username,
    [property: JsonPropertyName("password")]
    string Password,
    [property: JsonPropertyName(Wso2Constants.Schemas.User)]
    Wso2EnterpriseUser User
);