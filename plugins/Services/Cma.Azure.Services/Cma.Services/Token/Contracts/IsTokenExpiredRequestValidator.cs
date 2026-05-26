using FluentValidation;

namespace Cma.Services.Token.Contracts;

public class IsTokenExpiredRequestValidator : AbstractValidator<IsTokenExpiredRequest>
{
    public IsTokenExpiredRequestValidator()
    {
        RuleFor(x => x.Timestamp).GreaterThan(0);
        RuleFor(x => x.ExpiryInHours).GreaterThan(0);
    }
}