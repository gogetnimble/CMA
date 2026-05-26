using System.Text.Json.Serialization;

namespace Cma.Services.Payment.Bambora;

public record BamboraCardValidation
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;

    [JsonPropertyName("approved")] public int Approved { get; init; }

    [JsonPropertyName("message_id")] public int MessageId { get; init; }

    [JsonPropertyName("message")] public string Message { get; init; } = string.Empty;

    [JsonPropertyName("auth_code")] public string AuthCode { get; init; } = string.Empty;

    [JsonPropertyName("trans_date")] public string TransDate { get; init; } = string.Empty;

    [JsonPropertyName("order_number")] public string OrderNumber { get; init; } = string.Empty;

    [JsonPropertyName("type")] public string Type { get; init; } = string.Empty;

    [JsonPropertyName("amount")] public decimal Amount { get; init; }

    [JsonPropertyName("cvd_id")] public int CvdId { get; init; }
}