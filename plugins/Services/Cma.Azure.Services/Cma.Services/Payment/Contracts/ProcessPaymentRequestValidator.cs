using FluentValidation;

namespace Cma.Services.Payment.Contracts;

public class ProcessPaymentRequestValidator : AbstractValidator<ProcessPaymentRequest>
{
    public ProcessPaymentRequestValidator()
    {
        RuleFor(x => x.CardholderName)
            .NotNull()
            .NotEmpty()
            .MinimumLength(1)
            .MaximumLength(64);
        RuleFor(x => x.PaymentTokenCode)
            .NotNull()
            .NotEmpty()
            .MaximumLength(40);
        RuleFor(x => x.Amount)
            .NotNull()
            .GreaterThan(0);
        RuleFor(x => x.OrderNumber)
            .NotNull()
            .NotEmpty()
            .MaximumLength(30);
    }
}