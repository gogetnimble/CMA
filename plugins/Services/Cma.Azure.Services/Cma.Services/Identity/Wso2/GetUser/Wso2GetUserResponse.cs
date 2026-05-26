using System.Text.Json.Serialization;

namespace Cma.Services.Identity.Wso2.GetUser;

public record Wso2GetUserResponse
{
    [JsonPropertyName("totalResults")] public int TotalResults { get; set; }

    [JsonPropertyName("startIndex")] public int StartIndex { get; set; }

    [JsonPropertyName("itemsPerPage")] public int ItemsPerPage { get; set; }

    [JsonPropertyName("Resources")] public List<Wso2Resource> Resources { get; set; } = [];
}