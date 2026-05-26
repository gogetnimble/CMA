namespace Cma.Services.Token.Contracts;

public record IsTokenExpiredRequest(long Timestamp)
{
    public DateTime Now { get; set; } = DateTime.UtcNow;
    public int ExpiryInHours { get; init; }
}