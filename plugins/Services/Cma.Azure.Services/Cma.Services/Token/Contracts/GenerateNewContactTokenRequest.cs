namespace Cma.Services.Token.Contracts;

public record GenerateNewContactTokenRequest(Guid ContactId, string PreferredEmailAddress);