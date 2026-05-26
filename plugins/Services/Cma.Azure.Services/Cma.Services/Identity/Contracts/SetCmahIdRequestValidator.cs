using FluentValidation;

namespace Cma.Services.Identity.Contracts;

public class SetCmahIdRequestValidator : AbstractValidator<SetCmahIdRequest>
{
    public SetCmahIdRequestValidator()
    {
        RuleFor(x => x.UserId).NotNull().NotEmpty();
        RuleFor(x => x.CmahId).NotNull().NotEmpty();
    }
}