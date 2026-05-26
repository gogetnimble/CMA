using FluentValidation;

namespace Cma.Services.Crm.Contracts;

public class FindContactByRequestValidator : AbstractValidator<FindContactByRequest>
{
    public FindContactByRequestValidator()
    {
        RuleFor(x => x.KeyValues)
            .NotNull()
            .NotEmpty();
    }
}