namespace Cma.Services.Crm.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class CrmPropertyAttribute : Attribute
{
    public string PropertyName { get; }
    public string? EntityName { get; init; }

    public CrmPropertyAttribute(string propertyName, string? entityName = null)
    {
        PropertyName = propertyName;
    }
}