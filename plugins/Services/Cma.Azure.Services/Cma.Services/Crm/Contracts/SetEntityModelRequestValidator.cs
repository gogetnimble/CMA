using FluentValidation;

namespace Cma.Services.Crm.Contracts;

public class SetEntityModelRequestValidator<T> : AbstractValidator<SetEntityModelRequest<T>> where T: new()
{
    public SetEntityModelRequestValidator()
    {
        RuleFor(x => x.EntityModel)
            .NotNull();
    }
}