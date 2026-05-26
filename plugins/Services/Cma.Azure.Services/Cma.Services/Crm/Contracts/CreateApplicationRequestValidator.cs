using FluentValidation;

namespace Cma.Services.Crm.Contracts;

public class CreateApplicationRequestValidator : AbstractValidator<CreateApplicationRequest>
{
    public CreateApplicationRequestValidator()
    {
        RuleFor(x => x.CmahId)
            .NotNull()
            .NotEmpty();
        RuleFor(x => x.PtmaId)
            .NotNull()
            .NotEmpty();
    }
}