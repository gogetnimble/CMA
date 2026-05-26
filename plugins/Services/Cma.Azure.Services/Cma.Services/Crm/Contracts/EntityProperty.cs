namespace Cma.Services.Crm.Contracts;

public record EntityProperty(string Key, object? Value)
{
    public bool IsOptionSet { get; init; }
    public bool IsEntityReference { get; init; }
    public string? ReferencedEntityName { get; init; }
}