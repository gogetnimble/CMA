namespace Cma.Services.Identity.Contracts;

public record FindUserRequest(Dictionary<string, string?> QueryParameters);