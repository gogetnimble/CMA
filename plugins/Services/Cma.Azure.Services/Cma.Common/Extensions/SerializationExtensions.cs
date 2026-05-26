using System.Text.Json;
using Cma.Common.Exceptions;

namespace Cma.Common.Extensions;

public static class SerializationExtensions
{
    public static string Serialize(this object obj, bool pretty = false)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = pretty
        };

        return JsonSerializer.Serialize(obj, options);
    }

    public static T? Deserialize<T>(this string value)
    {
        var options = new JsonSerializerOptions
        {
            IncludeFields = true,
            PropertyNameCaseInsensitive = true
        };
        
        return JsonSerializer.Deserialize<T>(value, options);
    }

    public static T? Clone<T>(this T obj)
    {
        if (obj == null)
        {
            throw new BadRequestException(ErrorCode.ArgumentNull.ToSnakeCase(), "Cannot clone null object");
        }

        try
        {
            return obj.Serialize().Deserialize<T>();
        }
        catch (Exception exception)
        {
            throw new BadRequestException(ErrorCode.UnknownError.ToSnakeCase(), "Serialization error", new
            {
                Object = obj,
                Type = typeof(T).Name
            }, exception);
        }
    }

    public static T Clone<T>(this T obj, Action<T> action)
    {
        var clone = obj.Clone();

        if (clone == null)
        {
            throw new InternalServerErrorException(ErrorCode.SerializationFailure.ToSnakeCase(), "Error cloning",
                obj);
        }

        action.Invoke(clone);
        

        return clone;
    }
}