using FluentValidation;

namespace Cma.Services.Crm.Contracts;

public class SetEntityPropertiesRequestValidator : AbstractValidator<SetEntityPropertiesRequest>
{
    public SetEntityPropertiesRequestValidator()
    {
        RuleFor(x => x.EntityId)
            .NotEmpty();
        RuleFor(x => x.EntityName)
            .NotNull()
            .NotEmpty();
        RuleFor(x => x.Properties)
            .NotNull()
            .NotEmpty()
            .Must(y => y != null && !y.Contains(null!));
        RuleForEach(x => x.Properties).SetValidator(new EntityPropertyValidator());
    }

    private class EntityPropertyValidator : AbstractValidator<EntityProperty>
    {
        public EntityPropertyValidator()
        {
            RuleFor(x => x.Key)
                .NotNull()
                .NotEmpty();
        }
    }
}