using FluentValidation;

namespace Cma.Services.Identity.Contracts;

public class DeleteUserRequestValidator : AbstractValidator<DeleteUserRequest>
{
    public DeleteUserRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}