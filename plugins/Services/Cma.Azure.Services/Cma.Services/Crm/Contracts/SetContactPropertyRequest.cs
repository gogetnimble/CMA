namespace Cma.Services.Crm.Contracts;

public record SetContactPropertyRequest<T>(Guid ContactId, string PropertyKey, T PropertyValue);