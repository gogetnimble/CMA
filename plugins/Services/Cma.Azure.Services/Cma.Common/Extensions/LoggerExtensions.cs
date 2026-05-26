using Microsoft.Extensions.Logging;

namespace Cma.Common.Extensions;

public static class LoggerExtensions
{
    public static void LogInformationWithMetadata<T>(this ILogger logger, object objectToSerializeToLog)
    {
        logger.LogInformation(typeof(T).Name + Environment.NewLine + objectToSerializeToLog.Serialize(true));
    }
    
    public static void LogInformationWithMetadata(this ILogger logger, object objectToSerializeToLog)
    {
        logger.LogInformation(objectToSerializeToLog.GetType().Name + Environment.NewLine + objectToSerializeToLog.Serialize(true));
    }

    public static void LogInformationWithMetadata(this ILogger logger, string message, object objectToSerializeToLog)
    {
        logger.LogInformation(message + Environment.NewLine + objectToSerializeToLog.Serialize(true));
    }

    public static void LogWarningWithMetadata<T>(this ILogger logger, object objectToSerializeToLog)
    {
        logger.LogWarning(typeof(T).Name + Environment.NewLine + objectToSerializeToLog.Serialize(true));
    }
    
    public static void LogWarningWithMetadata(this ILogger logger, object objectToSerializeToLog)
    {
        logger.LogWarning(objectToSerializeToLog.GetType().Name + Environment.NewLine + objectToSerializeToLog.Serialize(true));
    }

    public static void LogWarningWithMetadata(this ILogger logger, string message, object objectToSerializeToLog)
    {
        logger.LogWarning(message + Environment.NewLine + objectToSerializeToLog.Serialize(true));
    }

    public static void LogErrorWithMetadata<T>(this ILogger logger, object objectToSerializeToLog)
    {
        logger.LogError(typeof(T).Name + Environment.NewLine + objectToSerializeToLog.Serialize(true));
    }
    
    public static void LogErrorWithMetadata(this ILogger logger, object objectToSerializeToLog)
    {
        logger.LogError(objectToSerializeToLog.GetType().Name + Environment.NewLine + objectToSerializeToLog.Serialize(true));
    }

    public static void LogErrorWithMetadata(this ILogger logger, string message, object objectToSerializeToLog)
    {
        logger.LogError(message + Environment.NewLine + objectToSerializeToLog.Serialize(true));
    }
}