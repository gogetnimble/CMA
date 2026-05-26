using Cma.Common.Validators;
using FluentValidation;

namespace Cma.Services.Identity.Contracts;

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotNull()
            .NotEmpty();

        RuleFor(x => x.Password)
            .NotNull()
            .SetValidator(new PasswordValidator());
    }
}