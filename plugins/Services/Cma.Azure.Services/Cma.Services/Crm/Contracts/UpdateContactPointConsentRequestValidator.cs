using FluentValidation;

namespace Cma.Services.Crm.Contracts;

public class UpdateContactPointConsentRequestValidator : AbstractValidator<UpdateContactPointConsentRequest>
{
    public UpdateContactPointConsentRequestValidator()
    {
        RuleFor(x => x.ContactPointConsentId)
            .NotNull()
            .NotEmpty();

        RuleFor(x => x.ConsentStatusKey)
            .NotNull()
            .NotEmpty();
    }
}