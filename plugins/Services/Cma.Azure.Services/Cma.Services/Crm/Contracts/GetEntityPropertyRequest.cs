namespace Cma.Services.Crm.Contracts;

public record GetEntityPropertyRequest(Guid EntityId, string PropertyKey);