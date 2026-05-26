using FluentValidation;

namespace Cma.Services.Identity.Contracts;

public class FindUserByUsernameRequestValidator : AbstractValidator<FindUserByUsernameRequest>
{
    public FindUserByUsernameRequestValidator()
    {
        RuleFor(x => x.Username).NotNull().NotEmpty();
    }
}