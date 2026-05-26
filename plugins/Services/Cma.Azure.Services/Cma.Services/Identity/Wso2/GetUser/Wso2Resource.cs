using System.Text.Json.Serialization;

namespace Cma.Services.Identity.Wso2.GetUser;

public record Wso2Resource
{
    [JsonPropertyName("id")] 
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("userName")] 
    public string Username { get; set; } = string.Empty;
    
    [JsonPropertyName("emails")] 
    public List<string> Emails { get; set; } = [];

    [JsonPropertyName("urn:ietf:params:scim:schemas:extension:enterprise:2.0:User")]
    public Wso2User Wso2User { get; set; } = new();
}