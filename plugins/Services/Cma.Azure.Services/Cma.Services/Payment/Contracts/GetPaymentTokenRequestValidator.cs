using FluentValidation;

namespace Cma.Services.Payment.Contracts;

public class GetPaymentTokenRequestValidator : AbstractValidator<GetPaymentTokenRequest>
{
    public GetPaymentTokenRequestValidator()
    {
        RuleFor(x => x.CreditCardNumber)
            .NotNull()
            .NotEmpty();
        RuleFor(x => x.ExpiryMonth)
            .GreaterThanOrEqualTo(1)
            .LessThanOrEqualTo(12);
        RuleFor(x => x.ExpiryYear)
            .GreaterThanOrEqualTo(DateTime.UtcNow.Year)
            .LessThanOrEqualTo(DateTime.UtcNow.AddYears(100).Year);
        RuleFor(x => x.Cvd)
            .GreaterThan(0)
            .LessThan(10000);
    }
}