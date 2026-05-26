using FluentValidation;

namespace Cma.Services.Crm.Contracts;

public class FindContactByCmahIdRequestValidator : AbstractValidator<FindContactByCmahIdRequest>
{
    public FindContactByCmahIdRequestValidator()
    {
        RuleFor(x => x.CmahId)
            .NotNull()
            .NotEmpty();
    }
}