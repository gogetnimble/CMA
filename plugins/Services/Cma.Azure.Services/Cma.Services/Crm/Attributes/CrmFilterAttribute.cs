namespace Cma.Services.Crm.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true)]
public class CrmFilterAttribute : Attribute, ICrmFilter
{
    public string FilterProperty { get; }
    public string FilterOperator { get; }
    public object FilterValue { get; }

    public CrmFilterAttribute(string filterProperty, string filterOperator, object filterValue)
    {
        FilterProperty = filterProperty;
        FilterOperator = filterOperator;
        FilterValue = filterValue;
    }
}

// This is mostly to get around the Attribute not being serializable so logging can be achieved
public interface ICrmFilter
{
    string FilterProperty { get; }
    string FilterOperator { get; }
    object FilterValue { get; }
}