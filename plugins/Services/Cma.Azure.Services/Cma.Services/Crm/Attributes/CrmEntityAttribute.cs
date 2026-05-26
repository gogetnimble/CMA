namespace Cma.Services.Crm.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class CrmEntityAttribute : Attribute
{
    public string EntityName { get; }

    public CrmEntityAttribute(string entityName)
    {
        EntityName = entityName;
    }
}