using FluentValidation;

namespace Cma.Services.Ftp.Contracts;

public class DeleteFolderRequestValidator : AbstractValidator<DeleteFolderRequest>
{
    public DeleteFolderRequestValidator()
    {
        RuleFor(x => x.Path)
            .NotNull()
            .NotEmpty();
    }
}