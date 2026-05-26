using FluentValidation;

namespace Cma.Services.Token.Contracts;

public class GenerateNewTokenRequestValidator : AbstractValidator<GenerateNewContactTokenRequest>
{
    public GenerateNewTokenRequestValidator()
    {
        RuleFor(x => x.ContactId)
            .NotEmpty();
        RuleFor(x => x.PreferredEmailAddress)
            .NotNull()
            .NotEmpty()
            .EmailAddress();
    }
}