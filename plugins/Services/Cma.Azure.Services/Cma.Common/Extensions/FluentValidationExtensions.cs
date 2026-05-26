using Cma.Common.Exceptions;
using FluentValidation;

namespace Cma.Common.Extensions;

public static class FluentValidationExtensions
{
    public static void ValidateAndThrowBadRequest<T>(this IValidator<T> validator, T instance)
    {
        var validationResult = validator.Validate(instance);

        if (validationResult.IsValid)
        {
            return;
        }

        var exception = new ValidationException(validationResult.Errors);
        var metadata = exception.Errors.ToLookup(e => e.PropertyName, e => e.ErrorMessage);

        throw new BadRequestException(ErrorCode.ArgumentInvalid.ToSnakeCase(), "Argument validation failed", metadata,
            exception);
    }
}