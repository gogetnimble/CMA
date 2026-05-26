using FluentValidation;

namespace Cma.Common.Validators;

public class PasswordValidator : AbstractValidator<string?>
{
    public PasswordValidator()
    {
        RuleFor(x => x)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .NotEmpty()
            .MinimumLength(8).WithMessage("Password should be minimum 8 characters.")
            .MaximumLength(20).WithMessage("Password should be maximum 20 characters.")
            .Matches("[A-Z]").WithMessage("Password should contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password should contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password should contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password should contain at least one special character.");
    }
}