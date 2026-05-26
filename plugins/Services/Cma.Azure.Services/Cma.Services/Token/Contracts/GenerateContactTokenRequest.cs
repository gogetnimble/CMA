namespace Cma.Services.Token.Contracts;

public record GenerateContactTokenRequest(Guid ContactId, string? PreferredEmailAddress, long Timestamp);