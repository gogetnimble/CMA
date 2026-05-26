using FluentValidation;

namespace Cma.Services.Crm.Contracts;

public class
    SendContactEmailDirectlyFromTemplateRequestValidator : AbstractValidator<
    SendContactEmailDirectlyFromTemplateRequest>
{
    public SendContactEmailDirectlyFromTemplateRequestValidator()
    {
        RuleFor(x => x.TemplateName)
            .NotNull()
            .NotEmpty();
        RuleFor(x => x.ContactId)
            .NotEmpty();
        RuleFor(x => x.ContactLanguage)
            .Must(x => x == CrmConstants.Contact.LanguageKey.English || x == CrmConstants.Contact.LanguageKey.French);
        RuleFor(x => x.DirectEmailAddress)
            .NotNull()
            .NotEmpty()
            .EmailAddress();
    }
}