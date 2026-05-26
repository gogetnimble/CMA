using Cma.Common.Exceptions;
using Microsoft.Extensions.Configuration;

namespace Cma.Common.Extensions;

public static class ConfigurationExtensions
{
    public static T GetValueIfExists<T>(this IConfiguration configuration, string key)
    {
        T? configurationValue;

        try
        {
            configurationValue = configuration.GetValue<T>(key);
        }
        catch (Exception exception)
        {
            throw new InternalServerErrorException(ErrorCode.ArgumentInvalid.ToSnakeCase(),
                $"{exception.Message}. {key}",
                new
                {
                    Key = key
                },
                exception);
        }

        if (typeof(T) == typeof(string))
        {
            var configurationValueAsString = configurationValue as string;

            if (string.IsNullOrWhiteSpace(configurationValueAsString))
            {
                throw new InternalServerErrorException(ErrorCode.ArgumentInvalid.ToSnakeCase(),
                    $"Key is null or empty. {key}", new
                    {
                        Key = key
                    });
            }
        }
        else if (configurationValue is null)
        {
            throw new InternalServerErrorException(ErrorCode.ArgumentInvalid.ToSnakeCase(),
                $"Key is null or empty. {key}", new
                {
                    Key = key
                });
        }

        return configurationValue!;
    }
}