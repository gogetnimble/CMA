using FluentValidation;

namespace Cma.Services.Identity.Contracts;

public class UnlockUserRequestValidator : AbstractValidator<UnlockUserRequest>
{
    public UnlockUserRequestValidator()
    {
        RuleFor(x => x.UserId).NotNull().NotEmpty();
    }
}