using FluentValidation;
using Cma.Common.Validators;

namespace Cma.Services.Identity.Contracts;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotNull()
            .NotEmpty()
            .MaximumLength(100);
        RuleFor(x => x.Password)
            .NotNull()
            .NotEmpty();
        RuleFor(x => x.CmahId)
            .NotNull()
            .NotEmpty();
    }
}