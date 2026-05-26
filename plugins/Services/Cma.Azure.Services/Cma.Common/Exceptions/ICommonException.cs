namespace Cma.Common.Exceptions;

public interface ICommonException
{
    string Code { get; }
    string Message { get; }
    object? Metadata { get; }
    Exception? InnerException { get; }
}