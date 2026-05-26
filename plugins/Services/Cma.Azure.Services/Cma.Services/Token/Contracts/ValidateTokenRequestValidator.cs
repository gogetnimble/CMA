using FluentValidation;

namespace Cma.Services.Token.Contracts;

public class ValidateTokenRequestValidator : AbstractValidator<ValidateTokenRequest>
{
    public ValidateTokenRequestValidator()
    {
        RuleFor(x => x.ContactId).NotEmpty();
        RuleFor(x => x.PreferredEmailAddress).NotEmpty().EmailAddress();
        RuleFor(x => x.Timestamp).NotEmpty();
        RuleFor(x => x.Token).NotEmpty();
    }
}