using FluentValidation;

namespace Cma.Services.Ftp.Contracts;

public class CreateFolderRequestValidator : AbstractValidator<CreateFolderRequest>
{
    public CreateFolderRequestValidator()
    {
        RuleFor(x => x.Path)
            .NotNull()
            .NotEmpty();
    }
}