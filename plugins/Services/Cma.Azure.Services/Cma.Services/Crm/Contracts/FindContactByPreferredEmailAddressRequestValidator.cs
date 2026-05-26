using FluentValidation;

namespace Cma.Services.Crm.Contracts;

public class FindContactByPreferredEmailAddressRequestValidator : AbstractValidator<FindContactByPreferredEmailAddressRequest>
{
    public FindContactByPreferredEmailAddressRequestValidator()
    {
        RuleFor(x => x.PreferredEmailAddress).NotNull().NotEmpty();
    }
}