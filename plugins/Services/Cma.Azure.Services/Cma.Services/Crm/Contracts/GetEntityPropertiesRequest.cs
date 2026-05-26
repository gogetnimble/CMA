namespace Cma.Services.Crm.Contracts;

public record GetEntityPropertiesRequest(Guid EntityId, List<string> PropertyKeys)
{
    public string EntityName { get; init; } = null!;
};