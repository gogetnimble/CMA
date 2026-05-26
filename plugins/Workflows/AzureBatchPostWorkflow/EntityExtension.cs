

using Microsoft.Xrm.Sdk;
using System;

namespace AzureBatchPostWorkflow
{
    public static class EntityExtension
    {
        public static void SetAttributeValue(this Entity e, string attributeName, object value)
        {
            if (e.Contains(attributeName))
                e[attributeName] = value;
            else
                e.Attributes.Add(attributeName, value);

        }
        public static string AttributeToString(this Entity entity, string attributeName, string prefix = "", bool hideAttributeName = false)
        {
            object entityValue = entity.Contains(attributeName) ? entity[attributeName] : null;
            entityValue = entityValue is AliasedValue ? ((AliasedValue)entityValue).Value : entityValue;
            string result = $"{prefix}{ (hideAttributeName ? "" : attributeName) }: ";
            if (entityValue is Guid)
                result += $"{entityValue}";
            else if (entityValue is EntityReference)
            {
                result += $"'{ (entityValue != null ? ((EntityReference)entityValue).Name : "")}' - { (entityValue != null ? ((EntityReference)entityValue).Id : Guid.Empty) }";
            }
            else if (entityValue is OptionSetValue)
                result += $"'{ (entity.FormattedValues.Contains(attributeName) ? entity.FormattedValues[attributeName] : "") }' - { (entityValue != null ? ((OptionSetValue)entityValue).Value.ToString() : "null") }";
            else if (entityValue is Money)
                result += $"{ (entityValue != null ? ((Money)entityValue).Value.ToString() : "null")}";
            else if (entityValue is DateTime)
                result += $"{(((DateTime)entityValue) != DateTime.MinValue && entityValue != null ? ((DateTime)entityValue).ToString() : "Default to MinDate")}";
            else if (entityValue is int ||
                        entityValue is decimal ||
                        entityValue is double
            )
                result += $"{ (entityValue != null ? entityValue.ToString() : "Default to 0")}";
            else if (entityValue is bool)
                result += $"{(entityValue != null ? entityValue.ToString() : "Default to false")}";
            else
                result += $"{(entityValue != null ? entityValue.ToString() : "null")}";
            return result;
        }
        public static T GetAttributeValueEx<T>(this Entity entity, string attributeName, ITracingService ts = null, string prefix = null)
        {

            object entityValue = entity.Contains(attributeName) ? entity[attributeName] : null;
            if (entityValue == null & ts != null)
                ts.Trace($"Entity does not contain {attributeName}");
            T result;// = (T)Convert.ChangeType(null, typeof(T));

            object theValue = entityValue is AliasedValue ? ((AliasedValue)entityValue).Value : entityValue;


            if (typeof(T) == typeof(Guid))
            {
                Guid temp = theValue != null ? (Guid)theValue : Guid.Empty;
                result = (T)Convert.ChangeType(temp, typeof(T));

            }
            else if (typeof(T) == typeof(EntityReference))
            {
                EntityReference temp = theValue != null ? (EntityReference)theValue : null;
                result = (T)Convert.ChangeType(temp, typeof(T));
            }
            else if (typeof(T) == typeof(OptionSetValue))
            {
                OptionSetValue temp = theValue != null ? (OptionSetValue)theValue : null;
                result = (T)Convert.ChangeType(temp, typeof(T));
            }
            else if (typeof(T) == typeof(Money))
            {
                Money temp = theValue != null ? (Money)theValue : null;
                result = (T)Convert.ChangeType(temp, typeof(T));
            }
            else if (typeof(T) == typeof(DateTime))
            {
                DateTime temp = theValue != null ? (DateTime)theValue : DateTime.MinValue;
                result = (T)Convert.ChangeType(temp, typeof(T));
            }
            else if (typeof(T) == typeof(int) ||
                        typeof(T) == typeof(decimal) ||
                        typeof(T) == typeof(double)
            )
                result = (T)(theValue != null ? Convert.ChangeType(theValue, typeof(T)) : Convert.ChangeType(0, typeof(T)));
            else if (typeof(T) == typeof(bool))
                result = (T)(theValue != null ? Convert.ChangeType(theValue, typeof(T)) : Convert.ChangeType(false, typeof(T)));
            else if (typeof(T) == typeof(int?) ||
                        typeof(T) == typeof(decimal?) ||
                        typeof(T) == typeof(double?) ||
                        typeof(T) == typeof(DateTime?) ||
                        typeof(T) == typeof(Guid?)
                        )
            {
                if (theValue != null)
                    result = (T)Convert.ChangeType(theValue, Nullable.GetUnderlyingType(typeof(T)));
                else
                    result = default(T);
            }
            else
                result = (T)Convert.ChangeType(theValue, typeof(T));

            if (ts != null)
                ts.Trace(entity.AttributeToString(attributeName, prefix));
            return result;
        }
    }
}