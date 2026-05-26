using Humanizer;

namespace Cma.Common.Extensions;

public static class EnumExtensions
{
    public static string ToSnakeCase(this Enum value)
    {
        var name = value.ToString();
        return name.Humanize().Underscore();
    }
}