namespace Cma.Common.Exceptions;

public class NotFoundException(string code, string message, object? metadata = null, Exception? innerException = null)
    : CommonException(code, message, metadata, innerException);