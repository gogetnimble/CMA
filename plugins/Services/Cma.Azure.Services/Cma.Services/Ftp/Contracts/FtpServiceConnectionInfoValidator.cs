using FluentValidation;

namespace Cma.Services.Ftp.Contracts;

public class FtpServiceConnectionInfoValidator : AbstractValidator<IFtpServiceConnectionInfo>
{
    public FtpServiceConnectionInfoValidator()
    {
        RuleFor(x => x.HostName)
            .NotNull()
            .NotEmpty();

        RuleFor(x => x.Username)
            .NotNull()
            .NotEmpty();
        
        RuleFor(x => x.Port).GreaterThan(0);
        
        RuleFor(x => x.PrivateKey)
            .NotEmpty()
            .When(x => !string.IsNullOrWhiteSpace(x.Passphrase))
            .WithMessage("Private key must be set when pass phrase has a value.");

        RuleFor(x => x)
            .Must(x =>
                !string.IsNullOrWhiteSpace(x.PrivateKey) ||
                !string.IsNullOrWhiteSpace(x.Password))
            .WithMessage("Either PrivateKey or Password must be supplied.");
    }
}