namespace Cma.Services.Token.Contracts;

public record ValidateTokenRequest(Guid ContactId, string PreferredEmailAddress, long Timestamp, string Token);