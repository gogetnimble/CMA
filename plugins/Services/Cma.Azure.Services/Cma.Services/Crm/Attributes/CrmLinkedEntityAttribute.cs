namespace Cma.Services.Crm.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class CrmLinkedEntityAttribute : Attribute
{
    public string LinkedEntityName { get; }
    public string ParentPropertyName { get; }
    public string LinkedPropertyName { get; }
    public string LinkAlias { get; }
    public string LinkType { get; init; }

    public CrmLinkedEntityAttribute(string linkedEntityName,
        string parentPropertyName,
        string linkedPropertyName,
        string linkAlias,
        string linkType = CrmConstants.LinkType.Outer)
    {
        LinkedEntityName = linkedEntityName;
        ParentPropertyName = parentPropertyName;
        LinkedPropertyName = linkedPropertyName;
        LinkAlias = linkAlias;
        LinkType = linkType;
    }
}