using FluentValidation;

namespace Cma.Services.Ftp.Contracts;

public class DeleteFileRequestValidator : AbstractValidator<DeleteFileRequest>
{
    public DeleteFileRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotNull()
            .NotEmpty();
    }
}