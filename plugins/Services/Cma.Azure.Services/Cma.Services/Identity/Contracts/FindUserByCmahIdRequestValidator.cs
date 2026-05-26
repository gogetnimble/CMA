using FluentValidation;

namespace Cma.Services.Identity.Contracts;

public class FindUserByCmahIdRequestValidator : AbstractValidator<FindUserByCmahIdRequest>
{
    public FindUserByCmahIdRequestValidator()
    {
        RuleFor(x => x.CmahId).NotNull().NotEmpty();
    }
}