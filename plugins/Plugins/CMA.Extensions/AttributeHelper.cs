using System;
using System.Linq.Expressions;

namespace CMA.Extensions
{
    public static class AttributeHelper
    {
        /// <summary>
        /// Gets the lowercase attribute name from a property expression
        /// </summary>
        /// <example>
        /// string nameAttr = EntityHelper.AttributeName&lt;Account&gt;(a => a.Name);
        /// </example>
        public static string AttributeName<T>(Expression<Func<T, object>> propertyExpression)
        {
            var memberExpression = propertyExpression.Body as MemberExpression;
            if (memberExpression == null)
            {
                var unaryExpression = propertyExpression.Body as UnaryExpression;
                memberExpression = unaryExpression?.Operand as MemberExpression;
            }

            return memberExpression?.Member.Name.ToLower();
        }
    }
}