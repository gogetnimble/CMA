using FluentValidation;

namespace Cma.Services.Crm.Contracts;

public class GetContactPointConsentsRequestValidator : AbstractValidator<GetContactPointConsentsRequest>
{
    public GetContactPointConsentsRequestValidator()
    {
        RuleFor(x => x.EmailAddress)
            .NotNull()
            .NotEmpty();
    }
}