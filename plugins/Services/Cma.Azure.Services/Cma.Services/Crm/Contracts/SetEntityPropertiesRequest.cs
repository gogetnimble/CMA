namespace Cma.Services.Crm.Contracts;

public record SetEntityPropertiesRequest(Guid EntityId, List<EntityProperty> Properties)
{
    public SetEntityPropertiesRequest(Guid EntityId, EntityProperty property) : this(EntityId, [property])
    {
    }
    
    public string EntityName { get; init; } = null!;
}