namespace Cma.Common.Exceptions;

public abstract class CommonException(
    string code,
    string message,
    object? metadata = null,
    Exception? innerException = null)
    : Exception(message, innerException), ICommonException
{
    public string Code { get; } = code;
    public object? Metadata { get; } = metadata;
}